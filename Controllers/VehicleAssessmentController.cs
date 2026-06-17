using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleAssessmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VehicleAssessmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-dash/{UserId}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessmentDto>>> GetVehicleAssessments(
            [FromRoute] Guid UserId,
            [FromQuery] DateTime? StartDate,
            [FromQuery] DateTime? EndDate)
        {
            var query = _context.VehicleAssessments.AsQueryable();

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            else if (user.AccessLevel == "driver")
            {
                // Step 1: Get the list of Drivers with the specified serNo
                var drivers = await _context.Drivers
                    .Where(d => d.SerNo == user.Svn)
                    .ToListAsync();

                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();

                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) );

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "chief_driver_com")
            {
                // Step 1: Get the list of Drivers with the specified serNo


                var officer = _context.Users.Where(r => r.Command.Contains(user.Command)).ToList();

                var serNoList = officer.Select(d => d.Svn).ToList();
                var drivers = await _context.Drivers
                    .Where(d => serNoList.Contains(d.SerNo))
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();


                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "zone")
            {
                query = query.Where(v => v.Zone == user.Zone);
            }else if(user.AccessLevel == "mechanic")
            {
                query= query.Where(r=>r.Command.Contains(user.Command));

            }

            // Apply date range filter
            if (StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                var endDate = EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.CreatedAt < endDate);
            }

            var vehicles = await query
                .Include(e => e.Allocations)
                .Include(r => r.Drivers)
                .Include(r => r.Remarks)
                .Include(r=>r.LogBooks)
                .Include(r=>r.MaintenanceReports)
                .Include(r=>r.IncidentReports)
                .Include(r=>r.Remarks)
                .Select(v => new 
                {
                    
                    RegistrationNumber = v.RegistrationNumber,
                    ChassisNumber = v.ChassisNumber,
                    VehicleTypeModel = v.VehicleTypeModel,
                    EngineNumber = v.EngineNumber,
                    VehicleLocation = v.VehicleLocation,
                    Command = v.Command,
                    Zone = v.Zone,
                    Condition = v.Condition,
                    v.MaintenanceStatus,
                    v.MaintenanceDueInKm,
                    v.LastMaintenanceDate,
                    v.CurrentMileage,
                    
                    Comments = v.Comments,
                    PictureA = v.PictureA,
                    PictureB = v.PictureB,
                    PictureC = v.PictureC,
                    PictureD = v.PictureD,
                    PictureE = v.PictureE,
                    CreatedAt = v.CreatedAt,
                    Allocations = v.Allocations.Select(a => new 
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
                        Office = a.Office,
                        Unit = a.Unit,
                        YearOfAllocation = a.YearOfAllocation,
                        CreatedAt = a.CreatedAt
                    }).ToList(),
                    Drivers = v.Drivers.Select(d => new 
                    {
                      
                        SerNo = d.SerNo,
                        Address = d.Address,
                        Name = d.Name,
                        LicenseNumber = d.LicenseNumber,
                        PhoneNumber = d.PhoneNumber,
                        Rank = d.Rank,
                        CreatedAt = d.CreatedAt
                    }).ToList(),
                    Remarks = v.Remarks.Select(r => new 
                    {
                        
                        RemarkText = r.RemarkText,
                        ChassisNumber = r.ChassisNumber,
                        VehicleId = r.VehicleId,
                        UserId = r.UserId,
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    LogBooks= v.LogBooks.Select(r => new
                    {
                        r.Purpose,
                        r.ReasonForRjection,
                        r.Status,
                        r.TimeIn,
                        r.TimeOut,r.To,r.From,r.Created,r.OfficerCarried,r.MileageTotal,r.MileageBefore,r.MileageAfter,
                        r.Remarks,
                        

                    }),
                    MaintenanceReports = v.MaintenanceReports.Select(r => new
                    {
                        r.Body,
                        r.Title,
                        r.Created
                    }),
                    IncidentReports = v.IncidentReports.Select(r => new
                    {
                        r.Body,
                        r.Title,
                        r.Created
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                data = vehicles,
                totalRecords = vehicles.Count
            });
        }
        // GET: api/VehicleAssessment
        [HttpGet("get-all/{UserId}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetVehicleAssessments([FromRoute] Guid UserId, [FromQuery] ServerTableRequest request)
        {
            var query = _context.VehicleAssessments.AsQueryable();

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            if (user.AccessLevel == "view")
            {
                query = query.Where(v => v.Command.Contains(user.Command));
            }
            else if (user.AccessLevel == "driver")
            {
                // Step 1: Get the list of Drivers with the specified serNo
                var drivers = await _context.Drivers
                    .Where(d => d.SerNo == user.Svn)
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();
                Console.WriteLine(vehicleIds.Count);

                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains( user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "chief_driver_com")
            {
                // Step 1: Get the list of Drivers with the specified serNo


                var officer = _context.Users.Where(r=>r.Command.Contains( user.Command)).ToList();

                var serNoList = officer.Select(d=>d.Svn).ToList();
                var drivers = await _context.Drivers
                    .Where(d => serNoList.Contains(d.SerNo))
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();
              

                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "zone")
            {
                query = query.Where(v => v.Zone == user.Zone);
            }
            else if (user.AccessLevel == "mechanic")
            {
                query = query.Where(r => r.Command.Contains(user.Command));

            }

            // Apply search functionality
            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v =>
                    v.RegistrationNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.ChassisNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleTypeModel.Contains(request.Search.ToLower()) ||
                    v.EngineNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleLocation.ToLower().Contains(request.Search.ToLower()) ||
                    v.Command.ToLower().Contains(request.Search.ToLower()) ||
                    v.Zone.ToLower().Contains(request.Search.ToLower()) ||
                    v.Condition.ToLower().Contains(request.Search.ToLower()) ||
                    v.Remark.ToLower().Contains(request.Search.ToLower())
                );
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.CreatedAt < endDate);
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
                query = query.OrderByDescending(v => v.CreatedAt);
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
        // GET: api/VehicleAssessment
        [HttpGet("get-all/serviceable/{UserId}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetSevceableVehicleAssessments([FromRoute] Guid UserId, [FromQuery] ServerTableRequest request)
        {
            var query = _context.VehicleAssessments.Where(r => r.Condition == "SERVICEABLE").AsQueryable();

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            if (user.AccessLevel == "view")
            {
                query = query.Where(v => v.Command.Contains(user.Command));
            }
            else if (user.AccessLevel == "driver")
            {
                // Step 1: Get the list of Drivers with the specified serNo
                var drivers = await _context.Drivers
                    .Where(d => d.SerNo == user.Svn)
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();
                Console.WriteLine(vehicleIds.Count);

                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "chief_driver_com")
            {
                // Step 1: Get the list of Drivers with the specified serNo


                var officer = _context.Users.Where(r => r.Command.Contains(user.Command)).ToList();

                var serNoList = officer.Select(d => d.Svn).ToList();
                var drivers = await _context.Drivers
                    .Where(d => serNoList.Contains(d.SerNo))
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();


                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "zone")
            {
                query = query.Where(v => v.Zone == user.Zone);
            }
            else if (user.AccessLevel == "mechanic")
            {
                query = query.Where(r => r.Command.Contains(user.Command));

            }

            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v =>
                    v.RegistrationNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.ChassisNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleTypeModel.Contains(request.Search.ToLower()) ||
                    v.EngineNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleLocation.ToLower().Contains(request.Search.ToLower()) ||
                    v.Command.ToLower().Contains(request.Search.ToLower()) ||
                    v.Zone.ToLower().Contains(request.Search.ToLower()) ||
                    v.Condition.ToLower().Contains(request.Search.ToLower()) ||
                    v.Remark.ToLower().Contains(request.Search.ToLower())
                );
            }
            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.CreatedAt < endDate);
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
                query = query.OrderByDescending(v => v.CreatedAt);
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


        // GET: api/VehicleAssessment
        [HttpGet("get-all/allocated/{UserId}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetAllocatedVehicleAssessments([FromRoute] Guid UserId, [FromQuery] ServerTableRequest request)
        {
            var query = _context.VehicleAssessments.Where(r => r.Condition == "SERVICEABLE").AsQueryable();
            var allo = _context.Allocations.Select(d => d.VehicleId)
                    .Distinct().AsQueryable();

            query = query.Where(v => allo.Contains(v.Id));

            var user = await _context.Users.FindAsync(UserId);


            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v =>
                    v.RegistrationNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.ChassisNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleTypeModel.Contains(request.Search.ToLower()) ||
                    v.EngineNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleLocation.ToLower().Contains(request.Search.ToLower()) ||
                    v.Command.ToLower().Contains(request.Search.ToLower()) ||
                    v.Zone.ToLower().Contains(request.Search.ToLower()) ||
                    v.Condition.ToLower().Contains(request.Search.ToLower()) ||
                    v.Remark.ToLower().Contains(request.Search.ToLower())
                );
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.CreatedAt < endDate);
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
                query = query.OrderByDescending(v => v.CreatedAt);
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



        // GET: api/VehicleAssessment
        [HttpGet("get-all/unserviceable/{UserId}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetUnserviceableVehicleAssessments([FromRoute] Guid UserId, [FromQuery] ServerTableRequest request)
        {
            var query = _context.VehicleAssessments.Where(r => r.Condition == "UNSERVICEABLE").AsQueryable();

            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {UserId} not found." });
            }

            if (user.AccessLevel == "view")
            {
                query = query.Where(v => v.Command.Contains(user.Command));
            }
            else if (user.AccessLevel == "driver")
            {
                // Step 1: Get the list of Drivers with the specified serNo
                var drivers = await _context.Drivers
                    .Where(d => d.SerNo == user.Svn)
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();
                Console.WriteLine(vehicleIds.Count);

                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "chief_driver_com")
            {
                // Step 1: Get the list of Drivers with the specified serNo


                var officer = _context.Users.Where(r => r.Command.Contains(user.Command)).ToList();

                var serNoList = officer.Select(d => d.Svn).ToList();
                var drivers = await _context.Drivers
                    .Where(d => serNoList.Contains(d.SerNo))
                    .ToListAsync();
                // Step 2: Get the list of VehicleIds from those drivers
                var vehicleIds = drivers
                    .Select(d => d.VehicleId)
                    .Distinct() // In case duplicate VehicleIds exist
                    .ToList();


                // Step 3: Get the VehicleAssessments by the VehicleIds
                query = query.Where(v => vehicleIds.Contains(v.Id) && v.Command.Contains(user.Command));

                // REMOVED: .ToList() and Console.WriteLine - these break the IQueryable
            }
            else if (user.AccessLevel == "zone")
            {
                query = query.Where(v => v.Zone == user.Zone);
            }
            else if (user.AccessLevel == "mechanic")
            {
                query = query.Where(r => r.Command.Contains(user.Command));

            }

            // Apply search functionality
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(v =>
                    v.RegistrationNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.ChassisNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleTypeModel.Contains(request.Search.ToLower()) ||
                    v.EngineNumber.ToLower().Contains(request.Search.ToLower()) ||
                    v.VehicleLocation.ToLower().Contains(request.Search.ToLower()) ||
                    v.Command.ToLower().Contains(request.Search.ToLower()) ||
                    v.Zone.ToLower().Contains(request.Search.ToLower()) ||
                    v.Condition.ToLower().Contains(request.Search.ToLower()) ||
                    v.Remark.ToLower().Contains(request.Search.ToLower())
                );
            }

            // Apply date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.Date.AddDays(1);
                query = query.Where(v => v.CreatedAt < endDate);
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
                query = query.OrderByDescending(v => v.CreatedAt);
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
        // GET: api/VehicleAssessment/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleAssessment>> GetVehicleAssessment(Guid id)
        {
           


                var vehicleAssessment = await _context.VehicleAssessments
        .Include(e => e.Allocations)
        .Include(r => r.Drivers)
        .Include(r => r.Remarks)
        .Include(r=>r.MaintenanceReports)
        .Include(r=>r.IncidentReports)
        .Include(r=>r.LogBooks)
        .Include(r=>r.ActivityLogs)
        .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicleAssessment == null)
            {
                return NotFound(new { message = $"Vehicle assessment with ID {id} not found." });
            }

            return Ok(vehicleAssessment);
        }

        // GET: api/VehicleAssessment/registration/{registrationNumber}
        [HttpGet("registration/{registrationNumber}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetByRegistrationNumber(string registrationNumber)
        {
            var assessments = await _context.VehicleAssessments
                .Where(v => v.RegistrationNumber != null && v.RegistrationNumber.Contains(registrationNumber))
                .OrderByDescending(v => v.Timestamp)
                .ToListAsync();

            if (!assessments.Any())
            {
                return NotFound(new { message = $"No assessments found for registration number: {registrationNumber}" });
            }

            return assessments;
        }

        // GET: api/VehicleAssessment/fetch_vehicle/{chassisNumber}
        [HttpGet("fetch_vehicle/{chassisNumber}")]
        public async Task<ActionResult<IEnumerable<VehicleAssessment>>> GetByChassisNumber(string chassisNumber)
        {
            var assessments = await _context.VehicleAssessments
                .Where(v => v.ChassisNumber != null && v.ChassisNumber.Contains(chassisNumber))
                .Include(r=>r.Allocations)
                .OrderByDescending(v => v.Timestamp)
                .ToListAsync();

            if (!assessments.Any())
            {
                return NotFound(new { message = $"No assessments found for chassis number: {chassisNumber}" });
            }

            return assessments;
        }





        // POST: api/VehicleAssessment
        [HttpPost]
        public async Task<ActionResult<VehicleAssessment>> CreateVehicleAssessment([FromForm] VehicleAssessmentDto vehicleAssessment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(vehicleAssessment.Condition))
            {
                return BadRequest(new { message = "Condition is required." });
            }

            try
            {
                // Check for duplicate registration number
                if (!string.IsNullOrWhiteSpace(vehicleAssessment.RegistrationNumber))
                {
                    var existingVehicle = await _context.VehicleAssessments
                        .FirstOrDefaultAsync(v => v.RegistrationNumber == vehicleAssessment.RegistrationNumber);
                    if (existingVehicle != null)
                    {
                        return Conflict(new { message = $"A vehicle assessment with registration number '{vehicleAssessment.RegistrationNumber}' already exists." });
                    }
                }

                if (!string.IsNullOrWhiteSpace(vehicleAssessment.ChassisNumber))
                {
                    var existingVehicle = await _context.VehicleAssessments
                        .FirstOrDefaultAsync(v => v.ChassisNumber == vehicleAssessment.ChassisNumber);
                    if (existingVehicle != null)
                    {
                        return Conflict(new { message = $"A vehicle assessment with chassis number '{vehicleAssessment.ChassisNumber}' already exists." });
                    }
                }

                // Handle image uploads
                var picturePaths = new PicturePaths
                {
                    PictureA = await SaveImage(vehicleAssessment.PictureAFile, "PictureA"),
                    PictureB = await SaveImage(vehicleAssessment.PictureBFile, "PictureB"),
                    PictureC = await SaveImage(vehicleAssessment.PictureCFile, "PictureC"),
                    PictureD = await SaveImage(vehicleAssessment.PictureDFile, "PictureD"),
                    PictureE = await SaveImage(vehicleAssessment.PictureEFile, "PictureE")
                };

                var status = vehicleAssessment.InitialMileage >= 6000 ? "Ok" : "Overdue";
                var vehicle = new VehicleAssessment
                {
                    Condition = vehicleAssessment.Condition,
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    CreatedAt = DateTime.UtcNow,
                    ChassisNumber = vehicleAssessment.ChassisNumber,
                    MaintenanceStatus = status,
                    VehicleTypeModel = vehicleAssessment.VehicleTypeModel,
                    RegistrationNumber = vehicleAssessment.RegistrationNumber,
                    InitialMileage = vehicleAssessment.InitialMileage,
                    CurrentMileage = vehicleAssessment.InitialMileage,
                    Command = vehicleAssessment.Command,
                    Zone = vehicleAssessment.Zone,
                    Comments = vehicleAssessment.Comments,
                    Remark = vehicleAssessment.Remark,
                    EngineNumber = vehicleAssessment.EngineNumber,
                    PictureA = picturePaths.PictureA,
                    PictureB = picturePaths.PictureB,
                    PictureC = picturePaths.PictureC,
                    PictureD = picturePaths.PictureD,
                    PictureE = picturePaths.PictureE,
                    VehicleLocation = vehicleAssessment.VehicleLocation
                };

                _context.VehicleAssessments.Add(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Vehicle assessment created successfully.",

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating vehicle assessment.", error = ex.Message });
            }
        }

        // Helper method to save images
        private async Task<string?> SaveImage(IFormFile? imageFile, string pictureType)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            try
            {
                // Validate file size (max 5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                    throw new Exception($"Image {pictureType} exceeds 5MB limit.");

                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    throw new Exception($"Invalid file type for {pictureType}. Allowed types: {string.Join(", ", allowedExtensions)}");

                // Create unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";

                // Create year/month folder structure inside your existing Uploads folder
                
                var uploadFolder = Path.Combine("Uploads");

                // Get the absolute path - DON'T add wwwroot since you have Uploads folder at root
                var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), uploadFolder);

                // Log the path for debugging
                Console.WriteLine($"Saving image to: {absolutePath}");

                // Create directory if it doesn't exist
                if (!Directory.Exists(absolutePath))
                {
                    Directory.CreateDirectory(absolutePath);
                    Console.WriteLine($"Created directory: {absolutePath}");
                }

                var filePath = Path.Combine(absolutePath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Return relative path for database storage
                var relativePath = Path.Combine(uploadFolder, uniqueFileName).Replace("\\", "/");
                Console.WriteLine($"File saved. Relative path: {relativePath}");

                return relativePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image {pictureType}: {ex.Message}");
                throw new Exception($"Failed to save image {pictureType}: {ex.Message}");
            }
        }



        // PUT: api/VehicleAssessment/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicleAssessment([FromRoute] Guid id, [FromForm] VehicleAssessmentDto vehicleAssessment)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "ID cannot be empty." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingAssessment = await _context.VehicleAssessments.FindAsync(id);
            if (existingAssessment == null)
            {
                return NotFound(new { message = $"Vehicle assessment with ID {id} not found." });
            }

            // Check for duplicate registration number (excluding current vehicle)
            if (!string.IsNullOrWhiteSpace(vehicleAssessment.RegistrationNumber) &&
                vehicleAssessment.RegistrationNumber != existingAssessment.RegistrationNumber)
            {
                var duplicateReg = await _context.VehicleAssessments
                    .FirstOrDefaultAsync(v => v.RegistrationNumber == vehicleAssessment.RegistrationNumber && v.Id != id);
                if (duplicateReg != null)
                {
                    return Conflict(new { message = $"A vehicle assessment with registration number '{vehicleAssessment.RegistrationNumber}' already exists." });
                }
                existingAssessment.RegistrationNumber = vehicleAssessment.RegistrationNumber;
            }

            // Check for duplicate chassis number (excluding current vehicle)
            if (!string.IsNullOrWhiteSpace(vehicleAssessment.ChassisNumber) &&
                vehicleAssessment.ChassisNumber != existingAssessment.ChassisNumber)
            {
                var duplicateChassis = await _context.VehicleAssessments
                    .FirstOrDefaultAsync(v => v.ChassisNumber == vehicleAssessment.ChassisNumber && v.Id != id);
                if (duplicateChassis != null)
                {
                    return Conflict(new { message = $"A vehicle assessment with chassis number '{vehicleAssessment.ChassisNumber}' already exists." });
                }
                existingAssessment.ChassisNumber = vehicleAssessment.ChassisNumber;
            }

            // Handle image updates
            var updatedImages = new List<string>();

            try
            {
                // Update images if new files are provided
                if (vehicleAssessment.PictureAFile != null)
                {
                    DeleteOldImage(existingAssessment.PictureA);
                    existingAssessment.PictureA = await SaveImage(vehicleAssessment.PictureAFile, "PictureA");
                    updatedImages.Add("PictureA");
                }

                if (vehicleAssessment.PictureBFile != null)
                {
                    DeleteOldImage(existingAssessment.PictureB);
                    existingAssessment.PictureB = await SaveImage(vehicleAssessment.PictureBFile, "PictureB");
                    updatedImages.Add("PictureB");
                }

                if (vehicleAssessment.PictureCFile != null)
                {
                    DeleteOldImage(existingAssessment.PictureC);
                    existingAssessment.PictureC = await SaveImage(vehicleAssessment.PictureCFile, "PictureC");
                    updatedImages.Add("PictureC");
                }

                if (vehicleAssessment.PictureDFile != null)
                {
                    DeleteOldImage(existingAssessment.PictureD);
                    existingAssessment.PictureD = await SaveImage(vehicleAssessment.PictureDFile, "PictureD");
                    updatedImages.Add("PictureD");
                }

                if (vehicleAssessment.PictureEFile != null)
                {
                    DeleteOldImage(existingAssessment.PictureE);
                    existingAssessment.PictureE = await SaveImage(vehicleAssessment.PictureEFile, "PictureE");
                    updatedImages.Add("PictureE");
                }

                // Update text fields
                if (vehicleAssessment.VehicleTypeModel != null)
                    existingAssessment.VehicleTypeModel = vehicleAssessment.VehicleTypeModel;

                if (vehicleAssessment.EngineNumber != null)
                    existingAssessment.EngineNumber = vehicleAssessment.EngineNumber;

                if (vehicleAssessment.VehicleLocation != null)
                    existingAssessment.VehicleLocation = vehicleAssessment.VehicleLocation;

                if (vehicleAssessment.Command != null)
                    existingAssessment.Command = vehicleAssessment.Command;

                if (vehicleAssessment.Zone != null)
                    existingAssessment.Zone = vehicleAssessment.Zone;

                if (vehicleAssessment.Condition != null)
                    existingAssessment.Condition = vehicleAssessment.Condition;

                if (vehicleAssessment.Remark != null)
                    existingAssessment.Remark = vehicleAssessment.Remark;

                if (vehicleAssessment.Comments != null)
                    existingAssessment.Comments = vehicleAssessment.Comments;

                // Update timestamp
                existingAssessment.Timestamp = DateTime.UtcNow.ToString("O");
                existingAssessment.CreatedAt = DateTime.UtcNow; // Add this field to your model

                _context.Update(existingAssessment);
                await _context.SaveChangesAsync();

                var response = new
                {
                    message = "Vehicle assessment updated successfully.",
                   
                };

                return Ok(response);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleAssessmentExists(id))
                {
                    return NotFound(new { message = $"Vehicle assessment with ID {id} not found." });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating vehicle assessment.", error = ex.Message });
            }
        }

        // Helper method to delete old images
        private void DeleteOldImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            try
            {
                var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath);
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - old image deletion failure shouldn't stop the update
                Console.WriteLine($"Failed to delete old image: {ex.Message}");
            }
        }

       
       
        // DELETE: api/VehicleAssessment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleAssessment(Guid id)
        {
            var vehicleAssessment = await _context.VehicleAssessments.FindAsync(id);
            if (vehicleAssessment == null)
            {
                return NotFound(new { message = $"Vehicle assessment with ID {id} not found." });
            }

            _context.VehicleAssessments.Remove(vehicleAssessment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehicle assessment deleted successfully." });
        }

       
        private bool VehicleAssessmentExists(Guid id)
        {
            return _context.VehicleAssessments.Any(e => e.Id == id);
        }
    }
}