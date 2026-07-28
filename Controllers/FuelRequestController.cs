// Controllers/FuelRequestController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FuelRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FuelRequestController> _logger;

        public FuelRequestController(ApplicationDbContext context, ILogger<FuelRequestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create a new fuel request
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFuelRequest([FromBody] FuelRequestCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate vehicle exists
                var vehicle = await _context.VehicleAssessments.FindAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return NotFound(new { error = $"Vehicle with ID {request.VehicleId} not found" });
                }

                // Check for duplicate pending request
                var existingRequest = await _context.FuelRequests
                    .FirstOrDefaultAsync(f => f.VehicleId == request.VehicleId
                        && f.Status == "Pending");

                if (existingRequest != null)
                {
                    return Conflict(new
                    {
                        error = "This vehicle already has a pending fuel request",
                        existingRequestId = existingRequest.Id,
                        requestNumber = existingRequest.RequestNumber
                    });
                }

                var user = _context.Users.FirstOrDefault(u => u.UserId == request.UserId);

                var fuelRequest = new FuelRequest
                {
                    VehicleId = request.VehicleId,
                    RequestNumber = GenerateRequestNumber(),
                    RequesterName = $"{user.Svn} {user.Fullname}",
                    RequiredDate = request.RequiredDate,
                    RequestedQuantity = request.RequestedQuantity,
                    FuelType = request.FuelType,
                    CurrentMileage = request.CurrentMileage,
                    Purpose = request.Purpose,
                    UserId = user.UserId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.FuelRequests.Add(fuelRequest);
                await _context.SaveChangesAsync();


                _logger.LogInformation("Fuel request created: {RequestNumber} for vehicle {VehicleId}",
                    fuelRequest.RequestNumber, request.VehicleId);

                return Ok(new { message = "Request created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fuel request for vehicle {VehicleId}", request.VehicleId);
                return StatusCode(500, new { error = $"An error occurred while creating the fuel request {ex.StackTrace}" });
            }
        }

        /// <summary>
        /// Get a specific fuel request by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFuelRequest(int id)
        {
            try
            {
                var result = await _context.FuelRequests.FindAsync(id);
                if (result == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the fuel request" });
            }
        }

        /// <summary>
        /// Get all fuel requests with filtering and pagination
        /// </summary>
        [HttpGet("get-all/{UserId}")]
        public async Task<ActionResult<IEnumerable<FuelRequest>>> GetFuelRequests(
      [FromRoute] Guid UserId,
      [FromQuery] ServerTableRequest request)
        {
            try
            {
                // Get the user
                var user = await _context.Users.FindAsync(UserId);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {UserId} not found." });
                }

                // Start with base query including navigation properties
                var query = _context.FuelRequests
                    .Include(f => f.VehicleAssessment)
                    .Include(f => f.User)
                    .AsQueryable();

                // Apply access level filters
                if (user.AccessLevel == "view")
                {
                    // View only users see requests for vehicles in their command
                    query = query.Where(f => f.Command.Contains(user.Command));
                }
                else if (user.AccessLevel == "driver")
                {


                    query = query.Where(f => f.UserId == user.UserId);

                }
                else if (user.AccessLevel == "chief_driver_com")
                {


                    query = query.Where(f =>
                                             f.Command.Contains(user.Command));
                }
                else if (user.AccessLevel == "zone")
                {
                    // Zone users see requests for vehicles in their zone
                    query = query.Where(f => f.Zone == user.Zone);
                }

                else if (user.AccessLevel == "admin" || user.AccessLevel == "manager")
                {
                    // Admins and managers see all requests (no additional filtering)
                    // Keep query as is
                }

                // Apply search functionality
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(f =>
                        f.RequestNumber.ToLower().Contains(request.Search.ToLower()) ||
                        f.RequesterName.ToLower().Contains(request.Search.ToLower()) ||
                        f.FuelType.ToLower().Contains(request.Search.ToLower()) ||
                        f.Purpose.ToLower().Contains(request.Search.ToLower()) ||

                        f.Status.ToLower().Contains(request.Search.ToLower())
                    );
                }



                // Apply date range filter
                if (request.StartDate.HasValue)
                {
                    query = query.Where(f => f.RequestDate >= request.StartDate.Value);
                }
                if (request.EndDate.HasValue)
                {
                    var endDate = request.EndDate.Value.Date.AddDays(1);
                    query = query.Where(f => f.RequestDate < endDate);
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                {
                    // Handle sorting for navigation properties
                    var sortProperty = request.SortBy.ToLower();

                    // Check if sorting by vehicle property
                    if (sortProperty.Contains("vehicle."))
                    {
                        var vehicleProperty = sortProperty.Replace("vehicle.", "");
                        query = request.SortOrder?.ToLower() == "desc"
                            ? query.OrderByDescending(f => EF.Property<object>(f.FuelType, vehicleProperty))
                            : query.OrderBy(f => EF.Property<object>(f.FuelType, vehicleProperty));
                    }
                    else
                    {
                        // Direct property sorting
                        query = request.SortOrder?.ToLower() == "desc"
                            ? query.OrderByDescending(f => EF.Property<object>(f, request.SortBy))
                            : query.OrderBy(f => EF.Property<object>(f, request.SortBy));
                    }
                }
                else
                {
                    // Default sorting by creation date
                    query = query.OrderByDescending(f => f.CreatedAt);
                }

                // Get total count before pagination
                var totalRecords = await query.CountAsync();

                // Apply pagination
                var fuelRequests = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Map to DTOs
                var result = fuelRequests.ToList();

                return Ok(new
                {
                    data = result,
                    totalCount = totalRecords,
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fuel requests for user {UserId}", UserId);
                return StatusCode(500, new { error = "An error occurred while retrieving fuel requests" });
            }
        }

        /// <summary>
        /// Update a fuel request (only when pending)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFuelRequest(Guid id, [FromBody] FuelRequestUpdateDto request)
        {
            try
            {
                var fuelRequest = await _context.FuelRequests.FindAsync(id);
                if (fuelRequest == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }

                // Only allow updates if status is Pending
                if (fuelRequest.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        error = $"Cannot update request with status: {fuelRequest.Status}",
                        currentStatus = fuelRequest.Status
                    });
                }

                // Update allowed fields
                    fuelRequest.Purpose = request.Purpose;

                fuelRequest.RequiredDate = request.RequiredDate;
                fuelRequest.RequestedQuantity = request.RequestedQuantity;
                fuelRequest.FuelType = request.FuelType;
                fuelRequest.CurrentMileage = request.CurrentMileage;





                fuelRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Fuel request {RequestNumber} updated", fuelRequest.RequestNumber);

                return Ok(new { message = "Fuel request updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while updating the fuel request" });
            }
        }

        /// <summary>
        /// Approve or reject a fuel request
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveFuelRequest(Guid id, [FromBody] FuelRequestApproveDto request)
        {
            try
            {
                var fuelRequest = await _context.FuelRequests.FindAsync(id);

                if (fuelRequest == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }

                if (fuelRequest.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        error = $"Cannot approve request with status: {fuelRequest.Status}",
                        currentStatus = fuelRequest.Status
                    });
                }

                var approverId = _context.Users.Find(fuelRequest.UserId);

                fuelRequest.Status = "Approved";

               
                    fuelRequest.ApprovedQuantity = request.ApprovedQuantity > 0
                        ? request.ApprovedQuantity
                        : fuelRequest.RequestedQuantity;

                    fuelRequest.ApprovedDate = DateTime.UtcNow;
               

                fuelRequest.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Fuel request {RequestNumber} {Status} by user {UserId}",
                    fuelRequest.RequestNumber, fuelRequest.Status, approverId);

                return Ok(new { message = "Request approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while approving the fuel request" });
            }
        }

        /// <summary>
        /// Dispense fuel for an approved request
        /// </summary>
        [HttpPost("{id}/dispense/{userId}")]
        public async Task<IActionResult> DispenseFuelRequest(Guid id, Guid userId)
        {
            try
            {
                var fuelRequest = await _context.FuelRequests.FindAsync(id);

                if (fuelRequest == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }

                if (fuelRequest.Status != "Approved")
                {
                    return BadRequest(new
                    {
                        error = $"Cannot dispense request with status: {fuelRequest.Status}",
                        currentStatus = fuelRequest.Status
                    });
                }

               

                var dispenserId = _context.Users.Find(userId);

                fuelRequest.Status = "Dispensed";
                fuelRequest.DispensedDate = DateTime.UtcNow;
                fuelRequest.DispensedByUserId = dispenserId.UserId;
                fuelRequest.UpdatedAt = DateTime.UtcNow;



                await _context.SaveChangesAsync();

                _logger.LogInformation("Fuel request {RequestNumber} dispensed by user {UserId}",
                    fuelRequest.RequestNumber, dispenserId);

                return Ok(new { message = "Fuel dispensed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispensing fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while dispensing fuel" });
            }
        }

        /// <summary>
        /// Cancel a fuel request
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelFuelRequest(Guid id, [FromBody] FuelRequestCancelDto request)
        {
            try
            {
                var fuelRequest = await _context.FuelRequests.FindAsync(id);
                if (fuelRequest == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }

                // Allow cancellation only for Pending or Approved requests
                if (fuelRequest.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        error = $"Cannot cancel request with status: {fuelRequest.Status}",
                        currentStatus = fuelRequest.Status
                    });
                }

                fuelRequest.Status = "Cancelled";
                fuelRequest.Reason = string.IsNullOrEmpty(request.Reason)
                    ? "Cancelled by user"
                    : $"Cancelled: {request.Reason}";
                fuelRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Fuel request {RequestNumber} cancelled by user {UserId}",
                    fuelRequest.RequestNumber);

                return Ok(new
                {
                    success = true,
                    message = "Fuel request cancelled successfully",
                    requestNumber = fuelRequest.RequestNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while cancelling the fuel request" });
            }
        }



        /// <summary>
        /// Get fuel requests by vehicle
        /// </summary>
        [HttpGet("fuel/vehicle/{vehicleId}")]
        public async Task<IActionResult> GetRequestsByVehicle(Guid vehicleId,
            [FromQuery] string status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.FuelRequests
                    .Include(f => f.VehicleAssessment)
                   
                    .Include(f => f.User)
                    .Where(f => f.VehicleId == vehicleId);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(f => f.Status == status);

                if (fromDate.HasValue)
                    query = query.Where(f => f.RequestDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(f => f.RequestDate <= toDate.Value);

                var requests = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                var result = requests.ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fuel requests for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { error = "An error occurred while retrieving fuel requests" });
            }
        }

        //delete fuel request
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuelRequest(Guid id)
        {
            try
            {
                var fuelRequest = await _context.FuelRequests.FindAsync(id);
                if (fuelRequest == null)
                {
                    return NotFound(new { error = $"Fuel request with ID {id} not found" });
                }
                // Only allow deletion if status is Pending
                if (fuelRequest.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        error = $"Cannot delete request with status: {fuelRequest.Status}",
                        currentStatus = fuelRequest.Status
                    });
                }
                _context.FuelRequests.Remove(fuelRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Fuel request {RequestNumber} deleted", fuelRequest.RequestNumber);
                return Ok(new { message = "Fuel request deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fuel request {Id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the fuel request" });
            }
        }


        private string GenerateRequestNumber()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            return $"FR-{date}-{randomPart}";
        }





     

        


        public class FuelRequestCreateDto
        {
            [Required]
            public Guid VehicleId { get; set; }

            [Required]
            public Guid UserId { get; set; }

           

            [Required]
            public DateTime RequiredDate { get; set; }

            [Required]
            [Range(0.01, double.MaxValue)]
            public decimal RequestedQuantity { get; set; }

            [Required]
            [StringLength(50)]
            public string FuelType { get; set; }

            [Required]
            public int CurrentMileage { get; set; }

            [Required]
            [StringLength(500)]
            public string Purpose { get; set; }

          


        }

        public class FuelRequestUpdateDto
        {
            [Required]
            public DateTime RequiredDate { get; set; }

            [Required]
            [Range(0.01, double.MaxValue)]
            public decimal RequestedQuantity { get; set; }

            [Required]
            [StringLength(50)]
            public string FuelType { get; set; }

            [Required]
            public int CurrentMileage { get; set; }

            [Required]
            [StringLength(500)]
            public string Purpose { get; set; }

        }

        public class FuelRequestApproveDto
        {
            
            public Guid UserId { get; set; }

            [Range(0, double.MaxValue)]
            public decimal ApprovedQuantity { get; set; }

            [StringLength(500)]
            public string? Remarks { get; set; }
        }

        

        public class FuelRequestCancelDto
        {
            public Guid UserId { get; set; }
            [StringLength(500)]
            public string Reason { get; set; }
        }

       



    }
}