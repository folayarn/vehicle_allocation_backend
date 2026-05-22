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
    public class AllocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public AllocationController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Allocation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Allocation>>> GetAllocations(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] string? chassisNumber = null,
            [FromQuery] int? year = null)
        {
            var query = _context.Allocations
                .Include(a => a.VehicleAssessment)
                .AsQueryable();

            // Filter by vehicle ID
            if (vehicleId.HasValue)
            {
                query = query.Where(a => a.VehicleId == vehicleId.Value);
            }

            // Filter by chassis number
            if (!string.IsNullOrWhiteSpace(chassisNumber))
            {
                query = query.Where(a => a.ChassisNumber.Contains(chassisNumber));
            }

            // Filter by year of allocation
            if (year.HasValue)
            {
                query = query.Where(a => a.YearOfAllocation == year.Value);
            }

            var allocations = await query
                .OrderByDescending(a => a.YearOfAllocation)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(allocations);
        }

        // GET: api/Allocation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Allocation>> GetAllocation(Guid id)
        {
            var allocation = await _context.Allocations
                .Include(a => a.VehicleAssessment)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (allocation == null)
            {
                return Ok(new List<Allocation>());
            }

            return Ok(allocation);
        }

        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetAllocationsByVehicle(Guid vehicleId)
        {
            var allocations = await _context.Allocations
                .Where(a => a.VehicleId == vehicleId)
                .OrderByDescending(a => a.YearOfAllocation)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new 
                {
                    Id = a.Id,
                    VehicleId = a.VehicleId,
                    ChassisNumber = a.ChassisNumber,
                    OfficerName = a.OfficerName,
                    OfficerSerNo = a.OfficerSerNo,
                    Type = a.Type,
                    Rank = a.Rank,
                    Command = a.Command,
                    Zone = a.Zone,
                    Department = a.Department,
                    a.FilePath,
                    Office = a.Office,
                    Unit = a.Unit,
                    YearOfAllocation = a.YearOfAllocation,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(allocations);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAllocation([FromForm] CreateAllocationDto createDto)
        {
            try
            {
              

                // Validate required fields
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Validate file
                if (createDto.FilePath == null || createDto.FilePath.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".pdf" };
                var fileExtension = Path.GetExtension(createDto.FilePath.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Invalid file type. Allowed types: PDF" });
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024;
                if (createDto.FilePath.Length > maxFileSize)
                {
                    Console.WriteLine($"File too large: {createDto.FilePath.Length} bytes");
                    return BadRequest(new { message = $"File size exceeds 5MB limit. Current size: {createDto.FilePath.Length} bytes" });
                }

                // Check if vehicle exists
                var vehicle = await _context.VehicleAssessments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == createDto.VehicleId);

                if (vehicle == null)
                {
                    return BadRequest(new { message = $"Vehicle with ID {createDto.VehicleId} not found" });
                }


                // Check for duplicate allocation
                bool existingAllocation = false;

                if (!string.IsNullOrWhiteSpace(createDto.OfficerSerNo))
                {
                    existingAllocation = await _context.Allocations
                        .AnyAsync(a => a.VehicleId == createDto.VehicleId
                            && a.OfficerSerNo == createDto.OfficerSerNo);
                }
                else if (!string.IsNullOrWhiteSpace(createDto.Department))
                {
                    existingAllocation = await _context.Allocations
                        .AnyAsync(a => a.VehicleId == createDto.VehicleId
                            && (a.Department == createDto.Department));
                }
                else if (!string.IsNullOrWhiteSpace(createDto.Office))
                {
                    existingAllocation = await _context.Allocations
                        .AnyAsync(a => a.VehicleId == createDto.VehicleId
                            && a.Office == createDto.Office);
                }

                if (existingAllocation)
                {
                    return Conflict(new { message = "An allocation already exists for this vehicle with the same officer/department/office" });
                }

                // Save the file - Use Path.Combine properly
                // Save the file - Clean the filename
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
                string relativePath = $"/Uploads/{uniqueFileName}";

                if(createDto.Type == "Auction")
                {
                    vehicle.Condition = "UNSERVICEABLE";

                }

                var allocation = new Allocation
                {
                    Id = Guid.NewGuid(),
                    VehicleId = createDto.VehicleId,
                    ChassisNumber = vehicle.ChassisNumber ?? createDto.ChassisNumber ?? string.Empty,
                    OfficerName = createDto.OfficerName?.ToUpper(),
                    OfficerSerNo = createDto.OfficerSerNo?.ToUpper(),
                    Type = createDto.Type,
                    Rank = createDto.Rank,
                    UserId = createDto.UserId,
                    Command = createDto.Command?.ToUpper(),
                    Zone = createDto.Zone?.ToUpper(),
                    Department = createDto.Department?.ToUpper(),
                    Office = createDto.Office?.ToUpper(),
                    Unit = createDto.Unit?.ToUpper(),
                    YearOfAllocation = createDto.YearOfAllocation,
                    FilePath = relativePath,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleAssessments.Update(vehicle);
                _context.Allocations.Add(allocation);
                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Created Allocation for {createDto.Department} {createDto.Unit} {createDto.Office} {createDto.OfficerSerNo}{createDto.OfficerName}", createDto.VehicleId, createDto.UserId);

                return Ok(new
                {
                 
                    message = "Allocation created successfully",
                    
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the allocation", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the allocation", error = ex.Message });
            }
        }


        // PUT: api/Allocation/5-
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAllocation(Guid id, [FromBody] CreateAllocationDto updateDto)
        {
            var allocation = await _context.Allocations.FindAsync(id);

            if (allocation == null)
            {
                return Ok(new List<Allocation>());
            }

            try
            {
                // Update only the fields that are provided
                if (!string.IsNullOrWhiteSpace(updateDto.OfficerName))
                    allocation.OfficerName = updateDto.OfficerName;

                if (!string.IsNullOrWhiteSpace(updateDto.OfficerSerNo))
                    allocation.OfficerSerNo = updateDto.OfficerSerNo;

                if (!string.IsNullOrWhiteSpace(updateDto.Type))
                    allocation.Type = updateDto.Type;

                if (!string.IsNullOrWhiteSpace(updateDto.Rank))
                    allocation.Rank = updateDto.Rank;

                if (!string.IsNullOrWhiteSpace(updateDto.Command))
                    allocation.Command = updateDto.Command;

                if (!string.IsNullOrWhiteSpace(updateDto.Zone))
                    allocation.Zone = updateDto.Zone;

                if (!string.IsNullOrWhiteSpace(updateDto.Department))
                    allocation.Department = updateDto.Department;

                if (!string.IsNullOrWhiteSpace(updateDto.Office))
                    allocation.Office = updateDto.Office;

                if (!string.IsNullOrWhiteSpace(updateDto.Unit))
                    allocation.Unit = updateDto.Unit;

                if (updateDto.YearOfAllocation != 0)
                    allocation.YearOfAllocation = updateDto.YearOfAllocation;

                await _context.SaveChangesAsync();
                _userService.ActivityLog($"Updated Allocation for {updateDto.Department} {updateDto.Unit} {updateDto.Office} {updateDto.OfficerSerNo}{updateDto.OfficerName}", updateDto.VehicleId, updateDto.UserId);

                return Ok(new { message = "Allocation updated successfully", allocation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the allocation", error = ex.Message });
            }
        }

        // DELETE: api/Allocation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAllocation(Guid id)
        {
            try
            {
                var allocation = await _context.Allocations.FindAsync(id);

                if (allocation == null)
                {
                    return NotFound(new { message = $"Allocation with ID {id} not found" });
                }

                // Delete the associated file if it exists
                if (!string.IsNullOrEmpty(allocation.FilePath))
                {
                    try
                    {
                        // Get the physical file path
                        // Option 1: If FilePath stores relative path like "/Uploads/filename.pdf"
                        string fileName = Path.GetFileName(allocation.FilePath);
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        // Option 2: If FilePath stores the full relative path
                        // string filePath = Path.Combine(Directory.GetCurrentDirectory(), allocation.FilePath.TrimStart('/'));

                        // Check if file exists and delete it
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            Console.WriteLine($"Deleted file: {filePath}");
                        }
                        else
                        {
                            Console.WriteLine($"File not found: {filePath}");
                        }
                    }
                    catch (Exception fileEx)
                    {
                        // Log file deletion error but continue with database deletion
                        Console.WriteLine($"Error deleting file: {fileEx.Message}");
                        // You can choose to return an error or just log it
                        // return StatusCode(500, new { message = "Error deleting associated file", error = fileEx.Message });
                    }
                }

                // Delete the allocation record from database
                _userService.ActivityLog($"Deleted Allocation for {allocation.Office} {allocation.OfficerSerNo}{allocation.OfficerName}", allocation.VehicleId, allocation.UserId);

                _context.Allocations.Remove(allocation);
                await _context.SaveChangesAsync();



                return Ok(new { message = "Allocation deleted successfully", fileDeleted = !string.IsNullOrEmpty(allocation.FilePath) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the allocation", error = ex.Message });
            }
        }

        // DELETE: api/Allocation/vehicle/{vehicleId}
        [HttpDelete("vehicle/{vehicleId}")]
        public async Task<IActionResult> DeleteAllocationsByVehicle(Guid vehicleId)
        {
            try
            {
                Console.WriteLine($"Attempting to delete all allocations for vehicle ID: {vehicleId}");

                var allocations = await _context.Allocations
                    .Where(a => a.VehicleId == vehicleId)
                    .ToListAsync();

                if (!allocations.Any())
                {
                    Console.WriteLine($"No allocations found for vehicle ID {vehicleId}");
                    return NotFound(new { message = $"No allocations found for vehicle ID {vehicleId}" });
                }

                var fileDeletionResults = new List<object>();
                int filesDeleted = 0;
                int filesNotFound = 0;
                int fileErrors = 0;

                // Delete associated files for each allocation
                foreach (var allocation in allocations)
                {
                    if (!string.IsNullOrEmpty(allocation.FilePath))
                    {
                        try
                        {
                            // Construct the full file path
                            string relativePath = allocation.FilePath.TrimStart('/');
                            string filePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

                            Console.WriteLine($"Looking for file: {filePath}");

                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                                filesDeleted++;
                                fileDeletionResults.Add(new
                                {
                                    allocationId = allocation.Id,
                                    filePath = allocation.FilePath,
                                    deleted = true
                                });
                                Console.WriteLine($"Deleted file: {filePath}");
                            }
                            else
                            {
                                // Try alternative path (just in case the file is directly in Uploads folder)
                                string alternativePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", Path.GetFileName(allocation.FilePath));
                                if (System.IO.File.Exists(alternativePath))
                                {
                                    System.IO.File.Delete(alternativePath);
                                    filesDeleted++;
                                    fileDeletionResults.Add(new
                                    {
                                        allocationId = allocation.Id,
                                        filePath = allocation.FilePath,
                                        deleted = true,
                                        alternativePath = true
                                    });
                                    Console.WriteLine($"Deleted file from alternative path: {alternativePath}");
                                }
                                else
                                {
                                    filesNotFound++;
                                    fileDeletionResults.Add(new
                                    {
                                        allocationId = allocation.Id,
                                        filePath = allocation.FilePath,
                                        deleted = false,
                                        reason = "File not found"
                                    });
                                    Console.WriteLine($"File not found: {filePath}");
                                }
                            }
                        }
                        catch (UnauthorizedAccessException unauthEx)
                        {
                            fileErrors++;
                            fileDeletionResults.Add(new
                            {
                                allocationId = allocation.Id,
                                filePath = allocation.FilePath,
                                deleted = false,
                                reason = $"Unauthorized access: {unauthEx.Message}"
                            });
                            Console.WriteLine($"Unauthorized access to file: {unauthEx.Message}");
                        }
                        catch (IOException ioEx)
                        {
                            fileErrors++;
                            fileDeletionResults.Add(new
                            {
                                allocationId = allocation.Id,
                                filePath = allocation.FilePath,
                                deleted = false,
                                reason = $"IO error: {ioEx.Message}"
                            });
                            Console.WriteLine($"IO error deleting file: {ioEx.Message}");
                        }
                        catch (Exception fileEx)
                        {
                            fileErrors++;
                            fileDeletionResults.Add(new
                            {
                                allocationId = allocation.Id,
                                filePath = allocation.FilePath,
                                deleted = false,
                                reason = $"Error: {fileEx.Message}"
                            });
                            Console.WriteLine($"Error deleting file: {fileEx.Message}");
                        }
                    }
                    else
                    {
                        fileDeletionResults.Add(new
                        {
                            allocationId = allocation.Id,
                           
                            deleted = false,
                            reason = "No file associated"
                        });
                    }
                }

                // Delete all allocation records from database
                _context.Allocations.RemoveRange(allocations);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully deleted {allocations.Count} allocation(s) for vehicle ID: {vehicleId}");
                Console.WriteLine($"Files deleted: {filesDeleted}, Not found: {filesNotFound}, Errors: {fileErrors}");

                return Ok(new
                {
                    message = $"{allocations.Count} allocation(s) deleted successfully",
                    vehicleId = vehicleId,
                    allocationsDeleted = allocations.Count,
                    filesDeleted = filesDeleted,
                    filesNotFound = filesNotFound,
                    fileErrors = fileErrors,
                    fileDetails = fileDeletionResults
                });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error while deleting allocations: {dbEx.Message}");
                Console.WriteLine($"Inner exception: {dbEx.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while deleting allocations", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while deleting allocations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while deleting allocations", error = ex.Message });
            }
        }
        // GET: api/Allocation/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetAllocationStatistics()
        {
            try
            {
                var stats = new
                {
                    TotalAllocations = await _context.Allocations.CountAsync(),
                    AllocationsByType = await _context.Allocations
                        .GroupBy(a => a.Type)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    AllocationsByYear = await _context.Allocations
                        .GroupBy(a => a.YearOfAllocation)
                        .OrderByDescending(g => g.Key)
                        .Select(g => new { Year = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    AllocationsByCommand = await _context.Allocations
                        .GroupBy(a => a.Command)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => new { Command = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    CurrentAllocations = await _context.Allocations
                        .Where(a => a.YearOfAllocation == DateTime.UtcNow.Year)
                        .CountAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching statistics", error = ex.Message });
            }
        }
    }
}