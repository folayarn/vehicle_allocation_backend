using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SparePartRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public SparePartRequestController(ApplicationDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/SparePartRequest
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SparePartRequest>>> GetSparePartRequests(


           [FromQuery] ServerTableRequest request
            )
        {
            var query = _context.SparePartRequests
                .Include(s => s.VehicleAssessment)
                .Include(s => s.Items)
                .Include(s=>s.ApprovedByUser)
               
                .AsQueryable();


            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v =>
                    v.RequestNumber.Contains(request.Search) ||
                    v.Priority.Contains(request.Search) ||
                    v.RequestType.Contains(request.Search) ||
                    v.RequiredByDate.Contains(request.Search) ||
                    v.Status.Contains(request.Search) ||
                    
                    v.ApprovalRemarks.Contains(request.Search)
                );
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(v => v.Created >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.Created < endDate);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(r => EF.Property<object>(r, request.SortBy))
                    : query.OrderBy(r => EF.Property<object>(r, request.SortBy));
            }
            else
            {
                query = query.OrderByDescending(v => v.Created);
            }

            // Apply pagination
            var totalRecords = await query.CountAsync();
            var vehicles = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return Ok(new
            {
                data = vehicles,
                totalCount = totalRecords,
                page = request.Page,
                pageSize = request.PageSize,
                totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize)
            });
        }

        // GET: api/SparePartRequest/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<IActionResult> GetSparePartRequestsByVehicle(Guid vehicleId)
        {
            var sparePartRequests = await _context.SparePartRequests
                .Where(s => s.VehicleId == vehicleId)
                .Include(s => s.Items)
                .OrderByDescending(s => s.Created)
                .Select(s => new
                {
                    s.Id,
                    s.RequestNumber,
                    s.Priority,
                    s.Status,
                    s.RequestType,
                    s.RequiredByDate,
                    s.IsUrgent,
                    s.Created,
                    s.VehicleId,
                    s.UserId,
                    ItemCount = s.Items.Count,
                    
                    s.Items
                })
                .ToListAsync();

            if (!sparePartRequests.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(sparePartRequests);
        }

        // GET: api/SparePartRequest/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSparePartRequestById(Guid id)
        {
            var sparePartRequest = await _context.SparePartRequests
                .Include(s => s.VehicleAssessment)
                .Include(s => s.Items)
                .Include(s => s.ApprovedByUser)
                
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sparePartRequest == null)
            {
                return NotFound(new { message = $"Spare part request with ID {id} not found" });
            }

            return Ok(sparePartRequest);
        }

        // GET: api/SparePartRequest/request-number/{requestNumber}
        [HttpGet("request-number/{requestNumber}")]
        public async Task<IActionResult> GetSparePartRequestByNumber(string requestNumber)
        {
            var sparePartRequest = await _context.SparePartRequests
                .Include(s => s.VehicleAssessment)
                .Include(s => s.Items)
                .Include(s => s.ApprovedByUser)
                .FirstOrDefaultAsync(s => s.RequestNumber == requestNumber);

            if (sparePartRequest == null)
            {
                return NotFound(new { message = $"Spare part request with number {requestNumber} not found" });
            }

            return Ok(sparePartRequest);
        }

        // POST: api/SparePartRequest
        [HttpPost]
        public async Task<ActionResult> CreateSparePartRequest([FromBody] CreateSparePartRequestDto createDto)
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
                if (createDto.VehicleId == Guid.Empty)
                {
                    return BadRequest(new { message = "Vehicle ID is required" });
                }

                if (createDto.UserId == Guid.Empty)
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                if (createDto.Items == null || !createDto.Items.Any())
                {
                    return BadRequest(new { message = "At least one item is required" });
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

                // Generate request number (e.g., SPR-2024-001)
                var currentYear = DateTime.UtcNow.Year;
                var lastRequest = await _context.SparePartRequests
                    .Where(s => s.RequestNumber.StartsWith($"SPR-{currentYear}"))
                    .OrderByDescending(s => s.RequestNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastRequest != null)
                {
                    var lastNumber = lastRequest.RequestNumber.Split('-').Last();
                    int.TryParse(lastNumber, out nextNumber);
                    nextNumber++;
                }

                var requestNumber = $"SPR-{currentYear}-{nextNumber:D3}";

                var sparePartRequest = new SparePartRequest
                {
                    Id = Guid.NewGuid(),
                    RequestNumber = requestNumber,
                    VehicleId = createDto.VehicleId,
                    UserId = createDto.UserId,
                    Priority = createDto.Priority ?? "Medium",
                    Status="Submitted",
                    
                    RequestType = createDto.RequestType ?? "Maintenance",
                    RequiredByDate = createDto.RequiredByDate,
                    IsUrgent = createDto.IsUrgent,
                    
                    Created = DateTime.UtcNow,
                    Items = new List<Item>()
                };

                // Add items
                foreach (var itemDto in createDto.Items)
                {
                    var item = new Item
                    {
                        Id = Guid.NewGuid(),
                        VehicleId = createDto.VehicleId,
                        UserId = createDto.UserId,
                        
                        Brand = itemDto.Brand,
                        Category = itemDto.Category,
                        QuantityRequested = itemDto.QuantityRequested,
                       
                        UnitOfMeasure = itemDto.UnitOfMeasure ?? "Pcs",
                    
                        Specification = itemDto.Specification,
                        IsCritical = itemDto.IsCritical,
                        ItemStatus = "Pending",
                        Created = DateTime.UtcNow,
                        SparePartRequestId = sparePartRequest.Id
                    };
                    sparePartRequest.Items.Add(item);
                }

                Console.WriteLine($"Saving spare part request to database");
                _context.SparePartRequests.Add(sparePartRequest);
                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Created Spare Part Request {requestNumber}", createDto.VehicleId, createDto.UserId);

                return Ok(new
                {
                    id = sparePartRequest.Id,
                    requestNumber = sparePartRequest.RequestNumber,
                    message = "Spare part request created successfully",
                    created = sparePartRequest.Created,
                    status = sparePartRequest.Status,
                    itemsCount = sparePartRequest.Items.Count
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error occurred while creating the spare part request", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while creating the spare part request", error = ex.Message });
            }
        }

        // PUT: api/SparePartRequest/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSparePartRequest(Guid id, [FromBody] UpdateSparePartRequestDto updateDto)
        {
            var sparePartRequest = await _context.SparePartRequests
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sparePartRequest == null)
            {
                return NotFound(new { message = $"Spare part request with ID {id} not found" });
            }

            try
            {
                // Update main request fields
                if (!string.IsNullOrWhiteSpace(updateDto.Priority))
                    sparePartRequest.Priority = updateDto.Priority;

                if (!string.IsNullOrWhiteSpace(updateDto.RequestType))
                    sparePartRequest.RequestType = updateDto.RequestType;

                if (updateDto.RequiredByDate != null)
                    sparePartRequest.RequiredByDate = updateDto.RequiredByDate;

                sparePartRequest.IsUrgent = updateDto.IsUrgent;

              

               

              

              

               

                // Update items (including allocator fields)
                if (updateDto.Items != null && updateDto.Items.Any())
                {
                    // Remove existing items
                    _context.Items.RemoveRange(sparePartRequest.Items);

                    // Add updated items with all fields
                    foreach (var itemDto in updateDto.Items)
                    {
                        var item = await _context.Items.FindAsync(itemDto.Id);



                        item.Brand = itemDto.Brand;
                        item.Category = itemDto.Category;
                        item.QuantityRequested = itemDto.QuantityRequested;
                        item.UnitOfMeasure = itemDto.UnitOfMeasure;
                        

                        item.Specification = itemDto.Specification;
                        item.IsCritical = itemDto.IsCritical;
                        
                        _context.Items.Update(item);
                    }
                }

                sparePartRequest.Updated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Updated Spare Part Request {sparePartRequest.RequestNumber}",
                    sparePartRequest.VehicleId, sparePartRequest.UserId);

                // Return updated request with all items
                var updatedRequest = await _context.SparePartRequests
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == id);

                return Ok(new { message = "Spare part request updated successfully", sparePartRequest = updatedRequest });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the spare part request", error = ex.Message });
            }
        }

       

      

        // PATCH: api/SparePartRequest/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveSparePartRequest(Guid id, [FromBody] ApproveSparePartRequestDto approveDto)
        {
            var sparePartRequest = await _context.SparePartRequests
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sparePartRequest == null)
            {
                return NotFound(new { message = $"Spare part request with ID {id} not found" });
            }

            try
            {
                sparePartRequest.Status = "Approved";
                sparePartRequest.ApprovedByUserId = approveDto.ApprovedByUserId;
                sparePartRequest.ApprovedDate = DateTime.UtcNow;
                sparePartRequest.ApprovalRemarks = approveDto.ApprovalRemarks;
                sparePartRequest.Updated = DateTime.UtcNow;

                // Update item statuses
                foreach (var it in approveDto.Items)
                {
                    var item = _context.Items.Find(it.Id);
                    item.ItemStatus = "Approved";
                    item.QuantityApproved = it.QuantityApproved;
                    item.PartNumber = it.PartNumber;
                    item.SupplierPartNumber = it.SupplierPartNumber;
                    item.SupplierName = it.SupplierName;
                    item.UnitPrice = it.UnitPrice;
                    item.IsStockItem = it.IsStockItem;
                    item.Updated = DateTime.UtcNow;

                    _context.Update(item);
                }

                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Approved Spare Part Request {sparePartRequest.RequestNumber}", sparePartRequest.VehicleId, sparePartRequest.ApprovedByUserId ?? sparePartRequest.UserId);

                return Ok(new { message = "Spare part request approved successfully", status = sparePartRequest.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving the spare part request", error = ex.Message });
            }
        }

        // PATCH: api/SparePartRequest/{id}/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectSparePartRequest(Guid id, [FromBody] RejectSparePartRequestDto rejectDto)
        {
            var sparePartRequest = await _context.SparePartRequests
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sparePartRequest == null)
            {
                return NotFound(new { message = $"Spare part request with ID {id} not found" });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(rejectDto.RejectionReason))
                {
                    return BadRequest(new { message = "Rejection reason is required" });
                }

                sparePartRequest.Status = "Rejected";
                sparePartRequest.ApprovedByUserId = rejectDto.RejectedByUserId;
                sparePartRequest.ApprovedDate = DateTime.UtcNow;
                sparePartRequest.RejectionReason = rejectDto.RejectionReason;
                sparePartRequest.Updated = DateTime.UtcNow;

                // Update item statuses
                foreach (var item in sparePartRequest.Items)
                {
                    item.ItemStatus = "Rejected";
                    item.Updated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _userService.ActivityLog($"Rejected Spare Part Request {sparePartRequest.RequestNumber} - Reason: {rejectDto.RejectionReason}", sparePartRequest.VehicleId, sparePartRequest.UserId);

                return Ok(new { message = "Spare part request rejected successfully", status = sparePartRequest.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while rejecting the spare part request", error = ex.Message });
            }
        }

        // DELETE: api/SparePartRequest/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSparePartRequest(Guid id)
        {
            try
            {
                var sparePartRequest = await _context.SparePartRequests
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sparePartRequest == null)
                {
                    return NotFound(new { message = $"Spare part request with ID {id} not found" });
                }

                _userService.ActivityLog($"Deleted Spare Part Request {sparePartRequest.RequestNumber}", sparePartRequest.VehicleId, sparePartRequest.UserId);

                _context.SparePartRequests.Remove(sparePartRequest);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Spare part request deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the spare part request", error = ex.Message });
            }
        }
    }
}