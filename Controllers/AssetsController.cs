using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Assets/get-all/{UserId}
        [HttpGet("get-all/{UserId}")]
        public async Task<ActionResult<object>> GetAllAssets(
            [FromRoute] Guid UserId,
            [FromQuery] ServerTableRequest request)
        {
            var query = _context.Assets.AsQueryable();

           // var user = await _context.AssetUsers.FindAsync(UserId);
            var user = await GetUserAccessInfo(UserId);
           
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            // Apply access level filters
            if (user.AccessLevel == "asset_zone")
            {
                query = query.Where(a => a.Zone != null && a.Zone.ToLower() == user.Zone.ToLower());
            }
            else if (user.AccessLevel == "asset_view")
            {
                query = query.Where(a => a.Command != null && a.Command.Contains(user.Command));
            }
            // For "admin" or "super_admin" - no filter, return all

            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(a =>
                    (a.AssetName.ToLower().Contains(request.Search.ToLower())) ||
                    (a.SerialNumber.Contains(request.Search.ToLower())) ||
                    (a.Location.ToLower().Contains(request.Search.ToLower())) ||
                    (a.Description.ToLower().Contains(request.Search.ToLower())) ||
                    (a.Zone.ToLower().Contains(request.Search.ToLower())) ||
                    (a.Command.ToLower().Contains(request.Search.ToLower())) ||
                    ( a.Category.ToLower().Contains(request.Search.ToLower())) ||
                    (a.BuildingType.ToLower().Contains(request.Search.ToLower())) ||
                    (a.AssetStatus.ToLower().Contains(request.Search.ToLower())) ||
                    (a.Condition.ToLower().Contains(request.Search.ToLower())) ||
                    (a.AssetType.ToLower().Contains(request.Search.ToLower()))
                );
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedAt < endDate);
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
                query = query.OrderByDescending(a => a.CreatedAt);
            }

            // Apply pagination
            var totalRecords = await query.CountAsync();
            var assets = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return Ok(new
            {
                data = assets,
                totalCount = totalRecords,
                page = request.Page,
                pageSize = request.PageSize,
                totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize)
            });
        }

        // GET: api/Assets/get-dash/{UserId}
        [HttpGet("get-dash/{UserId}")]
        public async Task<ActionResult<object>> GetAssetDashboard(
                [FromRoute] Guid UserId,
                [FromQuery] DateTime? StartDate,
                [FromQuery] DateTime? EndDate,
                [FromQuery] int PageNumber = 1,
                [FromQuery] int PageSize = 50)
        {
            var query = _context.Assets.AsQueryable();

            // Get user access information
            var userAccess = await GetUserAccessInfo(UserId);
            if (userAccess == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            // Apply access level filters
            if (userAccess.AccessLevel == "asset_zone")
            {
                query = query.Where(a => a.Zone != null && a.Zone.ToLower() == userAccess.Zone.ToLower());
            }
            else if (userAccess.AccessLevel == "asset_view")
            {
                query = query.Where(a => a.Command != null && a.Command.Contains(userAccess.Command));
            }
            // For "admin" or "super_admin" - no filter, return all

            // Apply date range filter based on CreatedAt
            if (StartDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                var endDate = EndDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedAt < endDate);
            }

            // Get total count for pagination

            // Get paginated assets
            var assets = await query
              
                .Select(a => new
                {
                    a.Id,
                    a.SerialNumber,
                    a.AssetName,
                    a.Remark,
                    a.Description,
                    a.Zone,
                    a.Command,
                    a.RenovationCost,
                    a.RenovationDate,
                    a.BrandName,
                    a.Location,
                    a.NoOfBuilding,
                    a.ConstructionCost,
                    a.LastRenovationCost,
                    a.CurrentPhysicalCondition,
                    a.ConstructionDate,
                    a.Category,
                    a.BuildingType,
                    a.AvailableDocument,
                    a.LitigationStatus,
                    a.Capacity,
                    a.AssetType,
                    a.AcquisitionDate,
                    a.AcquisitionCost,
                    a.AssetStatus,
                    a.PhysicalLocation,
                    a.Condition,
                    a.InsurancePolicyNo,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .OrderBy(a => a.Zone)
                .ThenBy(a => a.Command)
                .ThenBy(a => a.AssetName)
                .ToListAsync();

            // Calculate statistics efficiently using aggregation
            var statistics = await GetDashboardStatistics(query);

            return Ok(new
            {
                data = assets,
                statistics = statistics,
              
            });
        }

        private async Task<object> GetDashboardStatistics(IQueryable<Asset> query)
        {
            // Get all aggregations in one database query
            var stats = await query.GroupBy(a => 1).Select(g => new
            {
                TotalAssets = g.Count(),
                TotalConstructionCost = g.Sum(a => a.ConstructionCost ?? 0),
                TotalAcquisitionCost = g.Sum(a => a.AcquisitionCost ?? 0),
                TotalRenovationCost = g.Sum(a => a.RenovationCost ?? 0)
            }).FirstOrDefaultAsync();

            // Get grouped statistics
            var assetsByStatus = await query
                .GroupBy(a => a.AssetStatus ?? "unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var assetsByCondition = await query
                .GroupBy(a => a.Condition ?? "unknown")
                .Select(g => new { Condition = g.Key, Count = g.Count() })
                .ToListAsync();

            var assetsByZone = await query
                .GroupBy(a => a.Zone ?? "unknown")
                .Select(g => new { Zone = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get type counts
            var projectCount = await query.CountAsync(a => a.ConstructionCost != null || a.ConstructionDate != null);
            var landCount = await query.CountAsync(a => a.LitigationStatus != null || a.AvailableDocument != null);
            var electricalCount = await query.CountAsync(a => a.Capacity != null || a.BrandName != null || a.SerialNumber != null);

            return new
            {
                TotalAssets = stats?.TotalAssets ?? 0,
                TotalConstructionCost = stats?.TotalConstructionCost ?? 0,
                TotalAcquisitionCost = stats?.TotalAcquisitionCost ?? 0,
                TotalRenovationCost = stats?.TotalRenovationCost ?? 0,
                AssetsByStatus = assetsByStatus,
                AssetsByCondition = assetsByCondition,
                AssetsByZone = assetsByZone,
                AssetsByType = new
                {
                    Project = projectCount,
                    Land = landCount,
                    Electrical = electricalCount
                }
            };
        }

        // Helper method to get user access information
        private async Task<UserAccessDto> GetUserAccessInfo(Guid userId)
        {
            // First check AssetUsers table
            var assetUser = await _context.AssetUsers
                .Where(u => u.UserId == userId)
                .Select(u => new UserAccessDto
                {
                    Id = u.UserId,
                    AccessLevel = u.AccessLevel,
                    Zone = u.Zone,
                    Command = u.Command
                })
                .FirstOrDefaultAsync();

            if (assetUser != null)
            {
                return assetUser;
            }

            // Then check Users table
            var regularUser = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserAccessDto
                {
                    Id = u.UserId,
                    AccessLevel = u.AccessLevel,
                    Zone = u.Zone,
                    Command = u.Command
                })
                .FirstOrDefaultAsync();

            return regularUser;
        }
    

    // DTO for user access information
    public class UserAccessDto
    {
        public Guid Id { get; set; }
        public string AccessLevel { get; set; }
        public string Zone { get; set; }
        public string Command { get; set; }
    }


// GET: api/Assets
[HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets(
            [FromQuery] string? zone = null,
            [FromQuery] string? command = null,
            [FromQuery] string? assetStatus = null,
            [FromQuery] string? category = null,
            [FromQuery] string? buildingType = null,
            [FromQuery] string? condition = null,
            [FromQuery] string? assetType = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Assets.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(zone))
            {
                query = query.Where(a => a.Zone != null && a.Zone.Contains(zone));
            }

            if (!string.IsNullOrWhiteSpace(command))
            {
                query = query.Where(a => a.Command != null && a.Command.Contains(command));
            }

            if (!string.IsNullOrWhiteSpace(assetStatus))
            {
                query = query.Where(a => a.AssetStatus != null && a.AssetStatus == assetStatus);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(a => a.Category != null && a.Category.Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(buildingType))
            {
                query = query.Where(a => a.BuildingType != null && a.BuildingType.Contains(buildingType));
            }

            if (!string.IsNullOrWhiteSpace(condition))
            {
                query = query.Where(a => a.Condition != null && a.Condition == condition);
            }

            // Filter by asset type
            if (!string.IsNullOrWhiteSpace(assetType))
            {
                switch (assetType.ToLower())
                {
                    case "project":
                        query = query.Where(a => a.ConstructionCost != null || a.ConstructionDate != null);
                        break;
                    case "land":
                        query = query.Where(a => a.Capacity != null || a.LitigationStatus != null || a.AvailableDocument != null);
                        break;
                    case "electrical":
                        query = query.Where(a => a.Capacity != null || a.BrandName != null || a.SerialNumber != null);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    (a.AssetName != null && a.AssetName.Contains(searchTerm)) ||
                    (a.SerialNumber != null && a.SerialNumber.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm)) ||
                    (a.Description != null && a.Description.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var assets = await query
                .OrderBy(a => a.Zone)
                .ThenBy(a => a.Command)
                .ThenBy(a => a.AssetName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(assets);
        }

        // GET: api/Assets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);

            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            return Ok(asset);
        }

      

      

        // POST: api/Assets
        [HttpPost]
        public async Task<ActionResult<Asset>> CreateAsset([FromBody] AssetDto assetDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                SerialNumber = assetDto.SerialNumber,
                AssetName = assetDto.AssetName,
                Remark = assetDto.Remark ?? "No remarks provided",
                Description = assetDto.Description,
                Zone = assetDto.Zone,
                Command = assetDto.Command,
                RenovationCost = assetDto.RenovationCost,
                RenovationDate = assetDto.RenovationDate,
                BrandName = assetDto.BrandName,
                Location = assetDto.Location,
                NoOfBuilding = assetDto.NoOfBuilding,
                ConstructionCost = assetDto.ConstructionCost,
                LastRenovationCost = assetDto.LastRenovationCost,
                CurrentPhysicalCondition = assetDto.CurrentPhysicalCondition,
                ConstructionDate = assetDto.ConstructionDate,
                Category = assetDto.Category,
                BuildingType = assetDto.BuildingType,
                AvailableDocument = assetDto.AvailableDocument,
                LitigationStatus = assetDto.LitigationStatus,
                Capacity = assetDto.Capacity,
                AcquisitionDate = assetDto.AcquisitionDate,
                AcquisitionCost = assetDto.AcquisitionCost,
                AssetStatus = assetDto.AssetStatus ?? "serviceable",
                PhysicalLocation = assetDto.PhysicalLocation,
                Condition = assetDto.Condition ?? "good",
                InsurancePolicyNo = assetDto.InsurancePolicyNo,
                AssetType = assetDto.AssetType ?? "project",
                CreatedAt = DateTime.UtcNow,
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
        }

      

        // PUT: api/Assets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsset(Guid id, [FromBody] AssetDto updatedAssetDto)
        {
            var existingAsset = await _context.Assets.FindAsync(id);
            if (existingAsset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            // Update properties from DTO
            existingAsset.SerialNumber = updatedAssetDto.SerialNumber;
            existingAsset.AssetName = updatedAssetDto.AssetName;
            existingAsset.Remark = updatedAssetDto.Remark ?? existingAsset.Remark;
            existingAsset.Description = updatedAssetDto.Description;
            existingAsset.Zone = updatedAssetDto.Zone;
            existingAsset.Command = updatedAssetDto.Command;
            existingAsset.RenovationCost = updatedAssetDto.RenovationCost;
            existingAsset.RenovationDate = updatedAssetDto.RenovationDate;
            existingAsset.BrandName = updatedAssetDto.BrandName;
            existingAsset.Location = updatedAssetDto.Location;
            existingAsset.NoOfBuilding = updatedAssetDto.NoOfBuilding;
            existingAsset.ConstructionCost = updatedAssetDto.ConstructionCost;
            existingAsset.LastRenovationCost = updatedAssetDto.LastRenovationCost;
            existingAsset.CurrentPhysicalCondition = updatedAssetDto.CurrentPhysicalCondition;
            existingAsset.ConstructionDate = updatedAssetDto.ConstructionDate;
            existingAsset.Category = updatedAssetDto.Category;
            existingAsset.BuildingType = updatedAssetDto.BuildingType;
            existingAsset.AvailableDocument = updatedAssetDto.AvailableDocument;
            existingAsset.LitigationStatus = updatedAssetDto.LitigationStatus;
            existingAsset.Capacity = updatedAssetDto.Capacity;
            existingAsset.AcquisitionDate = updatedAssetDto.AcquisitionDate;
            existingAsset.AcquisitionCost = updatedAssetDto.AcquisitionCost;
            existingAsset.AssetStatus = updatedAssetDto.AssetStatus ?? existingAsset.AssetStatus;
            existingAsset.PhysicalLocation = updatedAssetDto.PhysicalLocation;
            existingAsset.Condition = updatedAssetDto.Condition ?? existingAsset.Condition;
            existingAsset.InsurancePolicyNo = updatedAssetDto.InsurancePolicyNo;
            existingAsset.AssetType = updatedAssetDto.AssetType ?? existingAsset.AssetType;
            existingAsset.UpdatedAt = DateTime.UtcNow;

            _context.Update(existingAsset);

            await _context.SaveChangesAsync();

            return Ok(existingAsset);
        }

        // PATCH: api/Assets/5/Status
        [HttpPatch("{id}/Status")]
        public async Task<IActionResult> UpdateAssetStatus(Guid id, [FromBody] string status)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            asset.AssetStatus = status;
            asset.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { id = asset.Id, assetStatus = asset.AssetStatus });
        }

        // PATCH: api/Assets/5/Condition
        [HttpPatch("{id}/Condition")]
        public async Task<IActionResult> UpdateAssetCondition(Guid id, [FromBody] string condition)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            asset.Condition = condition;
            asset.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { id = asset.Id, condition = asset.Condition });
        }

        // DELETE: api/Assets/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            // Soft delete - mark as inactive or remove from context based on business requirements
            // Currently just saving changes as the record remains but can be marked with an IsDeleted flag
            asset.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.AssetName}' has been deleted" });
        }

        // DELETE: api/Assets/5/Permanent
        [HttpDelete("{id}/Permanent")]
        public async Task<IActionResult> PermanentDeleteAsset(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = $"Asset with ID {id} not found" });
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.AssetName}' has been permanently deleted" });
        }

        // GET: api/Assets/Search
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<Asset>>> SearchAssets(
            [FromQuery] string keyword,
            [FromQuery] bool searchInDescription = true,
            [FromQuery] bool searchInLocation = true,
            [FromQuery] bool searchInSerialNumber = true)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "Keyword is required" });
            }

            var query = _context.Assets.AsQueryable();

            if (searchInSerialNumber)
            {
                query = query.Where(a => a.SerialNumber != null && a.SerialNumber.Contains(keyword));
            }

            if (searchInLocation)
            {
                query = query.Where(a => a.Location != null && a.Location.Contains(keyword));
            }

            if (searchInDescription)
            {
                query = query.Where(a => a.Description != null && a.Description.Contains(keyword));
            }

            // Always search in asset name
            query = query.Where(a => a.AssetName != null && a.AssetName.Contains(keyword));

            var assets = await query
                .OrderBy(a => a.Zone)
                .ThenBy(a => a.AssetName)
                .ToListAsync();

            return Ok(new { Keyword = keyword, Count = assets.Count, Assets = assets });
        }

        
    }
}