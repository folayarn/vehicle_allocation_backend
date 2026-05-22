using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public IncidentReportController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/IncidentReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IncidentReport>>> GetIncidentReports(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null)
        {
            var query = _context.IncidentReports
                .Include(i => i.VehicleAssessment)
                .AsQueryable();

            // Filter by vehicle ID
            if (vehicleId.HasValue)
            {
                query = query.Where(i => i.VehicleId == vehicleId.Value);
            }

            // Filter by user ID
            if (userId.HasValue)
            {
                query = query.Where(i => i.UserId == userId.Value);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(i => i.Created >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(i => i.Created <= endDate);
            }

            // Search in title and body
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i =>
                    i.Title.Contains(searchTerm) ||
                    i.Body.Contains(searchTerm));
            }

            var incidentReports = await query
                .OrderByDescending(i => i.Created)
                .ToListAsync();

            return Ok(incidentReports);
        }

        // GET: api/IncidentReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IncidentReport>> GetIncidentReport(Guid id)
        {
            var incidentReport = await _context.IncidentReports
                .Include(i => i.VehicleAssessment)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incidentReport == null)
            {
                return NotFound(new { message = $"Incident report with ID {id} not found" });
            }

            return Ok(incidentReport);
        }

        // GET: api/IncidentReport/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetIncidentReportsByVehicle(Guid vehicleId)
        {
            var incidentReports = await _context.IncidentReports
                .Where(i => i.VehicleId == vehicleId)
                .OrderByDescending(i => i.Created)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Body,
                    i.FilePath,
                    i.VehicleId,
                    i.UserId,
                    i.Created,
                    VehicleInfo = i.VehicleAssessment != null ? new
                    {
                        i.VehicleAssessment.ChassisNumber,
                       
                    } : null
                })
                .ToListAsync();

            if (!incidentReports.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(incidentReports);
        }

       

        // POST: api/IncidentReport
        [HttpPost]
        public async Task<ActionResult> CreateIncidentReport([FromForm] CreateIncidentReportDto createDto)
        {
            try
            {
                Console.WriteLine($"=== START CREATE INCIDENT REPORT ===");
                Console.WriteLine($"Title: {createDto.Title}");
                Console.WriteLine($"VehicleId: {createDto.VehicleId}");
                Console.WriteLine($"UserId: {createDto.UserId}");
                Console.WriteLine($"File received: {createDto.FilePath?.FileName}");
                Console.WriteLine($"File length: {createDto.FilePath?.Length}");
                Console.WriteLine($"File content type: {createDto.FilePath?.ContentType}");

                // Validate required fields
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    Console.WriteLine($"ModelState errors: {string.Join(", ", errors)}");
                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(createDto.Title))
                {
                    return BadRequest(new { message = "Title is required" });
                }

                if (string.IsNullOrWhiteSpace(createDto.Body))
                {
                    return BadRequest(new { message = "Body/description is required" });
                }

                if (createDto.VehicleId == Guid.Empty)
                {
                    return BadRequest(new { message = "Vehicle ID is required" });
                }

                if (createDto.UserId == Guid.Empty)
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                // Check if vehicle exists
                var vehicle = await _context.VehicleAssessments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == createDto.VehicleId);

                if (vehicle == null)
                {
                    Console.WriteLine($"Vehicle not found: {createDto.VehicleId}");
                    return BadRequest(new { message = $"Vehicle with ID {createDto.VehicleId} not found" });
                }

                string relativePath = null;

                // Handle file upload if provided
                if (createDto.FilePath != null && createDto.FilePath.Length > 0)
                {
                    // Validate file extension
                    var allowedExtensions = new[] { ".pdf" };
                    var fileExtension = Path.GetExtension(createDto.FilePath.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        Console.WriteLine($"Invalid file extension: {fileExtension}");
                        return BadRequest(new { message = "Invalid file type. Allowed types: PDF" });
                    }

                    // Validate file size (max 4MB for incident reports)
                    const long maxFileSize = 4 * 1024 * 1024;
                    if (createDto.FilePath.Length > maxFileSize)
                    {
                        Console.WriteLine($"File too large: {createDto.FilePath.Length} bytes");
                        return BadRequest(new { message = $"File size exceeds 10MB limit. Current size: {createDto.FilePath.Length} bytes" });
                    }

                    // Save the file
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Clean the filename - remove spaces and special characters
                    string originalFileName = createDto.FilePath.FileName;
                    string cleanFileName = Path.GetFileNameWithoutExtension(originalFileName);
                    string extension = Path.GetExtension(originalFileName);

                    // Remove special characters and replace spaces with underscores
                    cleanFileName = Regex.Replace(cleanFileName, @"[^\w\-]", "_");
                    cleanFileName = cleanFileName.Replace(" ", "_");

                    // Generate unique filename with cleaned name
                    string uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}_{cleanFileName}{extension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    Console.WriteLine($"Saving file to: {filePath}");

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await createDto.FilePath.CopyToAsync(fileStream);
                    }

                    // Store relative path - use forward slashes
                    relativePath = $"/Uploads/{uniqueFileName}";
                }

                var incidentReport = new IncidentReport
                {
                   
                    Title = createDto.Title?.ToUpper(),
                    Body = createDto.Body,
                    FilePath = relativePath,
                    VehicleId = createDto.VehicleId,
                    UserId = createDto.UserId,
                  
                };

                Console.WriteLine($"Saving incident report to database");
                _context.IncidentReports.Add(incidentReport);
                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Created Incident Report {createDto.Title}",createDto.VehicleId, createDto.UserId);


                return Ok(new
                {
                  
                    message = "Incident report created successfully",
                 
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the incident report", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the incident report", error = ex.Message });
            }
        }

        // PUT: api/IncidentReport/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncidentReport(Guid id, [FromBody] CreateIncidentReportDto updateDto)
        {
            var incidentReport = await _context.IncidentReports.FindAsync(id);

            if (incidentReport == null)
            {
                return NotFound(new { message = $"Incident report with ID {id} not found" });
            }

            try
            {
                // Update only the fields that are provided
                if (!string.IsNullOrWhiteSpace(updateDto.Title))
                    incidentReport.Title = updateDto.Title.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.Body))
                    incidentReport.Body = updateDto.Body;


                if (updateDto.FilePath != null || updateDto.FilePath.Length != 0)
                {
                    
                }

                // Validate file extension
                var allowedExtensions = new[] { ".pdf" };
                var fileExtension = Path.GetExtension(updateDto.FilePath.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Invalid file type. Allowed types: PDF" });
                }

                // Validate file size (max 10MB)
                const long maxFileSize = 4* 1024 * 1024;
                if (updateDto.FilePath.Length > maxFileSize)
                {
                    return BadRequest(new { message = $"File size exceeds 4MB limit. Current size: {updateDto.FilePath.Length} bytes" });
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(incidentReport.FilePath))
                {
                    try
                    {
                        string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                            incidentReport.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            Console.WriteLine($"Deleted old file: {oldFilePath}");
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"Error deleting old file: {fileEx.Message}");
                    }
                }

                // Save new file
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Clean the filename
                string originalFileName = updateDto.FilePath.FileName;
                string cleanFileName = Path.GetFileNameWithoutExtension(originalFileName);
                string extension = Path.GetExtension(originalFileName);
                cleanFileName = Regex.Replace(cleanFileName, @"[^\w\-]", "_");
                cleanFileName = cleanFileName.Replace(" ", "_");

                string uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}_{cleanFileName}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updateDto.FilePath.CopyToAsync(fileStream);
                }

                string relativePath = $"/Uploads/{uniqueFileName}";
                incidentReport.FilePath = relativePath;

                _context.Update(incidentReport);
                await _context.SaveChangesAsync();
                _userService.ActivityLog($"Updated Incident Report {updateDto.Title}", updateDto.VehicleId, updateDto.UserId);

                return Ok(new { message = "Incident report updated successfully", incidentReport });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the incident report", error = ex.Message });
            }
        }

       
        // DELETE: api/IncidentReport/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncidentReport(Guid id)
        {
            try
            {
                var incidentReport = await _context.IncidentReports.FindAsync(id);

                if (incidentReport == null)
                {
                    return NotFound(new { message = $"Incident report with ID {id} not found" });
                }

                // Delete the associated file if it exists
                if (!string.IsNullOrEmpty(incidentReport.FilePath))
                {
                    try
                    {
                        string filePath = Path.Combine(Directory.GetCurrentDirectory(),
                            incidentReport.FilePath.TrimStart('/'));

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            Console.WriteLine($"Deleted file: {filePath}");
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"Error deleting file: {fileEx.Message}");
                    }
                }

                _userService.ActivityLog($"Delete Incident Report {incidentReport.Title}", incidentReport.VehicleId, incidentReport.UserId);

                _context.IncidentReports.Remove(incidentReport);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Incident report deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the incident report", error = ex.Message });
            }
        }

      

    }
}