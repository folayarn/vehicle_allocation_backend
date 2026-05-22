using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public MaintenanceRequestController(ApplicationDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/MaintenanceRequest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceRequest>>> GetMaintenanceRequests(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null)
        {
            var query = _context.MaintenanceRequests
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

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(m => m.status == status);
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

            var maintenanceRequests = await query
                .OrderByDescending(m => m.Created)
                .ToListAsync();

            return Ok(maintenanceRequests);
        }

        // GET: api/MaintenanceRequest/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetMaintenanceRequestsByVehicle(Guid vehicleId)
        {
            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.Created)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Body,
                    m.status,
                    m.VehicleId,
                    m.UserId,
                    m.Created
                })
                .ToListAsync();

            if (!maintenanceRequests.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(maintenanceRequests);
        }

        // GET: api/MaintenanceRequest/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMaintenanceRequestById(Guid id)
        {
            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(m => m.VehicleAssessment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenanceRequest == null)
            {
                return NotFound(new { message = $"Maintenance request with ID {id} not found" });
            }

            return Ok(maintenanceRequest);
        }

        // POST: api/MaintenanceRequest
        [HttpPost]
        public async Task<ActionResult> CreateMaintenanceRequest([FromBody] CreateMaintenanceRequestDto createDto)
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

                var maintenanceRequest = new MaintenanceRequest
                {
                    Id = Guid.NewGuid(),
                    Title = createDto.Title?.ToUpper(),
                    Body = createDto.Body,
                    status = createDto.Status ?? "Pending",
                    VehicleId = createDto.VehicleId,
                    UserId = createDto.UserId,
                    Created = DateTime.UtcNow
                };

                Console.WriteLine($"Saving maintenance request to database");
                _context.MaintenanceRequests.Add(maintenanceRequest);
                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Created Maintenance Request {createDto.Title}", createDto.VehicleId, createDto.UserId);

                return Ok(new
                {
                    id = maintenanceRequest.Id,
                    message = "Maintenance request created successfully",
                    created = maintenanceRequest.Created,
                    status = maintenanceRequest.status
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the maintenance request", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the maintenance request", error = ex.Message });
            }
        }

        // PUT: api/MaintenanceRequest/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenanceRequest(Guid id, [FromBody] CreateMaintenanceRequestDto updateDto)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);

            if (maintenanceRequest == null)
            {
                return NotFound(new { message = $"Maintenance request with ID {id} not found" });
            }

            try
            {
                // Update only the fields that are provided
                if (!string.IsNullOrWhiteSpace(updateDto.Title))
                    maintenanceRequest.Title = updateDto.Title.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.Body))
                    maintenanceRequest.Body = updateDto.Body;

                if (!string.IsNullOrWhiteSpace(updateDto.Status))
                    maintenanceRequest.status = updateDto.Status;

                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Updated Maintenance Request {maintenanceRequest.Title}", maintenanceRequest.VehicleId, maintenanceRequest.UserId);

                return Ok(new { message = "Maintenance request updated successfully", maintenanceRequest });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the maintenance request", error = ex.Message });
            }
        }


        // In your MaintenanceRequestController.cs, add:

        // PATCH: api/MaintenanceRequest/{id}/acknowledge

       public class AckDto
        {
            
            public string remark { get; set; }
        }

        [HttpPut("{id}/acknowledge")]
        public async Task<IActionResult> AcknowledgeMaintenanceRequest(Guid id, [FromBody] AckDto dto)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);

            if (maintenanceRequest == null)
            {
                return NotFound(new { message = $"Maintenance request with ID {id} not found" });
            }

            try
            {
                maintenanceRequest.status = "Acknowledged";
                maintenanceRequest.remark = dto.remark;

                _context.Update(maintenanceRequest);

                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Acknowledged Maintenance Request {maintenanceRequest.Title}", maintenanceRequest.VehicleId, maintenanceRequest.UserId);

                return Ok(new { message = "Maintenance request acknowledged successfully", status = maintenanceRequest.status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while acknowledging the maintenance request", error = ex.Message });
            }
        }


        // PATCH: api/MaintenanceRequest/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateMaintenanceRequestStatus(Guid id, [FromBody] CreateMaintenanceRequestDto updateStatusDto)
        {
            var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);

            if (maintenanceRequest == null)
            {
                return NotFound(new { message = $"Maintenance request with ID {id} not found" });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(updateStatusDto.Status))
                {
                    maintenanceRequest.status = updateStatusDto.Status;
                    await _context.SaveChangesAsync();

                    _userService.ActivityLog($"Updated Maintenance Request status to {updateStatusDto.Status} for request {maintenanceRequest.Title}", maintenanceRequest.VehicleId, maintenanceRequest.UserId);

                    return Ok(new { message = "Maintenance request status updated successfully", status = maintenanceRequest.status });
                }

                return BadRequest(new { message = "Status is required" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the maintenance request status", error = ex.Message });
            }
        }

        // DELETE: api/MaintenanceRequest/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceRequest(Guid id)
        {
            try
            {
                var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);

                if (maintenanceRequest == null)
                {
                    return NotFound(new { message = $"Maintenance request with ID {id} not found" });
                }

                _userService.ActivityLog($"Deleted Maintenance Request {maintenanceRequest.Title}", maintenanceRequest.VehicleId, maintenanceRequest.UserId);

                _context.MaintenanceRequests.Remove(maintenanceRequest);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Maintenance request deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the maintenance request", error = ex.Message });
            }
        }
    }
}