using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public MaintenanceReportController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/MaintenanceReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceReport>>> GetMaintenanceReports(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null)
        {
            var query = _context.MaintenanceReports
                .Include(m => m.VehicleAssessment)
                .AsQueryable();

            // Filter by vehicle ID
            if (vehicleId.HasValue)
            {
                query = query.Where(m => m.VehicleId == vehicleId.Value);
            }

            // Filter by user ID
            if (userId.HasValue)
            {
                query = query.Where(m => m.UserId == userId.Value);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(m => m.Created >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(m => m.Created <= endDate);
            }

            // Search in title and body
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    m.Title.Contains(searchTerm) ||
                    m.Body.Contains(searchTerm));
            }

            var maintenanceReports = await query
                .OrderByDescending(m => m.Created)
                .ToListAsync();

            return Ok(maintenanceReports);
        }

       

        // GET: api/MaintenanceReport/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetMaintenanceReportsByVehicle(Guid vehicleId)
        {
            var maintenanceReports = await _context.MaintenanceReports
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.Created)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Body,
                    m.VehicleId,
                    m.UserId,
                    m.Created,
                    m.LastMaintenanceMileage,
                    
                })
                .ToListAsync();

            if (!maintenanceReports.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(maintenanceReports);
        }

       


     

        // POST: api/MaintenanceReport
        [HttpPost]
        public async Task<ActionResult> CreateMaintenanceReport([FromBody] CreateMaintenanceReportDto createDto)
        {
            try
            {
              

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

                var maintenanceReport = new MaintenanceReport
                {
                    Id = Guid.NewGuid(),
                    Title = createDto.Title?.ToUpper(),
                    LastMaintenanceMileage = createDto.LastMaintenanceMileage,
                    Body = createDto.Body,
                    VehicleId = createDto.VehicleId,
                    UserId = createDto.UserId,
                    Created = DateTime.UtcNow
                };

                vehicle.LastMaintenanceDate = DateTime.UtcNow;
                vehicle.CurrentMileage = createDto.LastMaintenanceMileage;
                vehicle.MaintenanceStatus = "Ok";
                vehicle.LastMaintenanceMileage= createDto.LastMaintenanceMileage;

                Console.WriteLine($"Saving maintenance report to database");
                _context.MaintenanceReports.Add(maintenanceReport);
                _context.VehicleAssessments.Update(vehicle);

                await _context.SaveChangesAsync();
                _userService.ActivityLog($"Created Maintenance Report {createDto.Title}", createDto.VehicleId, createDto.UserId);


                return Ok(new
                {
                    id = maintenanceReport.Id,
                    message = "Maintenance report created successfully",
                    created = maintenanceReport.Created
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the maintenance report", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the maintenance report", error = ex.Message });
            }
        }

        // PUT: api/MaintenanceReport/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenanceReport(Guid id, [FromBody] CreateMaintenanceReportDto updateDto)
        {
            var maintenanceReport = await _context.MaintenanceReports.FindAsync(id);

            if (maintenanceReport == null)
            {
                return NotFound(new { message = $"Maintenance report with ID {id} not found" });
            }

            try
            {
                // Update only the fields that are provided
                if (!string.IsNullOrWhiteSpace(updateDto.Title))
                    maintenanceReport.Title = updateDto.Title.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.Body))
                    maintenanceReport.Body = updateDto.Body;

                maintenanceReport.LastMaintenanceMileage = updateDto.LastMaintenanceMileage;

                await _context.SaveChangesAsync();
                _userService.ActivityLog($"Updated Maintenance Report {updateDto.Title}", updateDto.VehicleId, updateDto.UserId);


                return Ok(new { message = "Maintenance report updated successfully", maintenanceReport });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the maintenance report", error = ex.Message });
            }
        }

        // DELETE: api/MaintenanceReport/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceReport(Guid id)
        {
            try
            {
                var maintenanceReport = await _context.MaintenanceReports.FindAsync(id);

                if (maintenanceReport == null)
                {
                    return NotFound(new { message = $"Maintenance report with ID {id} not found" });
                }

                _userService.ActivityLog($"deleted Maintenance Report {maintenanceReport.Title}", maintenanceReport.VehicleId, maintenanceReport.UserId);


                _context.MaintenanceReports.Remove(maintenanceReport);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Maintenance report deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the maintenance report", error = ex.Message });
            }
        }

        
      
    }
}