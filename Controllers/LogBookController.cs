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
    public class LogBookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public LogBookController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/LogBook
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LogBook>>> GetLogBooks(
            [FromQuery] Guid? vehicleId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            var query = _context.LogBooks
                .Include(l => l.VehicleAssessment)
                .AsQueryable();

            // Filter by vehicle ID
            if (vehicleId.HasValue)
            {
                query = query.Where(l => l.VehicleId == vehicleId.Value);
            }

            // Filter by user ID
            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId.Value);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.Created >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(l => l.Created <= endDate);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status.Contains(status));
            }

            var logBooks = await query
                .OrderByDescending(l => l.Created)
                .ThenByDescending(l => l.TimeOut)
                .ToListAsync();

            return Ok(logBooks);
        }

        // GET: api/LogBook/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LogBook>> GetLogBook(Guid id)
        {
            var logBook = await _context.LogBooks
                .Include(l => l.VehicleAssessment)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (logBook == null)
            {
                return NotFound(new { message = $"LogBook entry with ID {id} not found" });
            }

            return Ok(logBook);
        }

        // GET: api/LogBook/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetLogBooksByVehicle(Guid vehicleId)
        {
            var logBooks = await _context.LogBooks
                .Where(l => l.VehicleId == vehicleId)
                .OrderByDescending(l => l.Created)
                .ThenByDescending(l => l.TimeOut)
                .Select(l => new
                {
                    l.Id,
                    l.From,
                    l.To,
                    l.MileageBefore,
                    l.MileageAfter,
                    l.MileageTotal,
                    l.OfficerCarried,
                    l.Purpose,
                    l.TimeOut,
                    l.TimeIn,
                    l.Status,
                    l.ReasonForRjection,
                    
                    l.Remarks,
                    l.VehicleId,
                    l.UserId,
                    l.Created
                })
                .ToListAsync();

            if (!logBooks.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(logBooks);
        }

       

        // POST: api/LogBook
        [HttpPost]
        public async Task<ActionResult> CreateLogBook([FromBody] CreateLogBookDto createDto)
        {
            try
            {
                Console.WriteLine($"=== START CREATE LOGBOOK ===");
                Console.WriteLine($"VehicleId: {createDto.VehicleId}");
                Console.WriteLine($"From: {createDto.From}");
                Console.WriteLine($"To: {createDto.To}");
                Console.WriteLine($"Purpose: {createDto.Purpose}");

                // Validate required fields
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    Console.WriteLine($"ModelState errors: {string.Join(", ", errors)}");
                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Validate required fields
                if (createDto.VehicleId == Guid.Empty)
                {
                    return BadRequest(new { message = "Vehicle ID is required" });
                }

                if (string.IsNullOrWhiteSpace(createDto.From))
                {
                    return BadRequest(new { message = "Starting location is required" });
                }

                if (string.IsNullOrWhiteSpace(createDto.To))
                {
                    return BadRequest(new { message = "Destination is required" });
                }

                if (string.IsNullOrWhiteSpace(createDto.Purpose))
                {
                    return BadRequest(new { message = "Purpose of trip is required" });
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

                // Calculate mileage total if both before and after are provided
                decimal mileageTotal = 0;
                if (createDto.MileageBefore!=null &&
                    createDto.MileageAfter !=null)
                {
                   
                        var total = createDto.MileageAfter - createDto.MileageBefore;
                        mileageTotal = total;
                    
                }

                // Set default status if not provided
                string status = "Pending";

                var logBook = new LogBook
                {
                    
                    From = createDto.From?.ToUpper(),
                    To = createDto.To?.ToUpper(),
                    MileageBefore = createDto.MileageBefore,
                    MileageAfter = createDto.MileageAfter,
                    MileageTotal = mileageTotal,
                    OfficerCarried = createDto.OfficerCarried?.ToUpper(),
                    Purpose = createDto.Purpose?.ToUpper(),
                    TimeOut = createDto.TimeOut ?? DateTime.Now.ToString("HH:mm"),
                    TimeIn = createDto.TimeIn,
                    Status = status,
                    
                    Remarks = createDto.Remarks,
                    VehicleId = createDto.VehicleId,
                    UserId = createDto.UserId,
                };

                _context.LogBooks.Add(logBook);
                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Created Log Book Report {createDto.Remarks}", createDto.VehicleId, createDto.UserId);


                return Ok(new
                {
                    id = logBook.Id,
                    message = "LogBook entry created successfully",
                    mileageTotal = mileageTotal
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the logbook entry", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the logbook entry", error = ex.Message });
            }
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveLogBookAsync([FromRoute] Guid id)
        {
            var logBook = await _context.LogBooks.FindAsync(id);
            if (logBook == null)
                throw new KeyNotFoundException($"LogBook with ID {id} not found");

            // Only allow approval of pending logs
            if (logBook.Status != "Pending")
                throw new InvalidOperationException($"Cannot approve log with status: {logBook.Status}");

            logBook.Status = "Approved";
            


            _context.LogBooks.Update(logBook);
            await _context.SaveChangesAsync();

            _userService.ActivityLog($"Approve Log Book Report {logBook.Remarks}", logBook.VehicleId, logBook.UserId);

            return Ok(new {message="Approve Successfully"});
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectLogBookAsync([FromRoute] Guid id, [FromBody] RejectLogBookDto Reason )
        {
            var logBook = await _context.LogBooks.FindAsync(id);
            if (logBook == null)
                throw new KeyNotFoundException($"LogBook with ID {id} not found");

            // Only allow approval of pending logs
            if (logBook.Status != "Pending")
                throw new InvalidOperationException($"Cannot reject log with status: {logBook.Status}");

            logBook.Status = "Rejected";
            logBook.ReasonForRjection = Reason.Reason;



            _context.LogBooks.Update(logBook);
            await _context.SaveChangesAsync();

            _userService.ActivityLog($"Rejected Log Book Report {logBook.Remarks}", logBook.VehicleId, logBook.UserId);

            return Ok(new { message = "Rejected Successfully" });
        }

        public class RejectLogBookDto
        {
           
            public string Reason { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLogBook(Guid id, [FromBody] CreateLogBookDto updateDto)
        {
            var logBook = await _context.LogBooks.FindAsync(id);



            if (logBook == null)
            {
                return NotFound(new { message = $"LogBook entry with ID {id} not found" });
            }
            if (logBook.Status == "Completed")
            {
                return BadRequest(new { message = "LogBook cannot be Updated" });
            }

            try
            {
                // Update only the fields that are provided
                if (!string.IsNullOrWhiteSpace(updateDto.From))
                    logBook.From = updateDto.From.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.To))
                    logBook.To = updateDto.To.ToUpper();

                if (updateDto.MileageBefore!=null)
                {
                    logBook.MileageBefore = updateDto.MileageBefore;

                    // Recalculate total if after is also available
                    if (logBook.MileageAfter !=null)
                    {
                            var total = logBook.MileageAfter - logBook.MileageBefore;
                            logBook.MileageTotal = total;
                        
                    }
                }

                if (updateDto.MileageAfter!=null)
                {
                    logBook.MileageAfter = updateDto.MileageAfter;

                    // Recalculate total if before is also available
                    if (logBook.MileageBefore!=null)
                    {
                      
                            var total = logBook.MileageAfter - logBook.MileageBefore;
                            logBook.MileageTotal = total;
                        
                    }
                }

                if (!string.IsNullOrWhiteSpace(updateDto.OfficerCarried))
                    logBook.OfficerCarried = updateDto.OfficerCarried.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.Purpose))
                    logBook.Purpose = updateDto.Purpose.ToUpper();

                if (!string.IsNullOrWhiteSpace(updateDto.TimeOut))
                    logBook.TimeOut = updateDto.TimeOut;

                if (!string.IsNullOrWhiteSpace(updateDto.TimeIn))
                    logBook.TimeIn = updateDto.TimeIn;

              

                if (!string.IsNullOrWhiteSpace(updateDto.Remarks))
                    logBook.Remarks = updateDto.Remarks;
                logBook.Status = "Pending";

                _context.Update(logBook);

                await _context.SaveChangesAsync();

                return Ok(new { message = "LogBook entry updated successfully", logBook });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the logbook entry", error = ex.Message });
            }
        }

        // DELETE: api/LogBook/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogBook(Guid id)
        {
            try
            {
                var logBook = await _context.LogBooks.FindAsync(id);



                if (logBook == null)
                {
                    return NotFound(new { message = $"LogBook entry with ID {id} not found" });
                }

                if(logBook.Status == "Completed")
                {
                    return BadRequest(new {message ="LogBook cannot be Deleted"});
                }

                _context.LogBooks.Remove(logBook);
                await _context.SaveChangesAsync();

                return Ok(new { message = "LogBook entry deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the logbook entry", error = ex.Message });
            }
        }

        

       
        
    }
}