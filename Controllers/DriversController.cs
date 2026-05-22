using DocumentFormat.OpenXml.Spreadsheet;
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
    public class DriversController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public DriversController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/drivers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers()
        {
            var drivers = await _context.Drivers
                .Include(d => d.VehicleAssessment)
                .ToListAsync();

            return Ok(drivers);
        }

        // GET: api/drivers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Driver>> GetDriver(Guid id)
        {
            var driver = await _context.Drivers
                .Include(d => d.VehicleAssessment)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                return NotFound($"Driver with ID {id} not found.");
            }

            return Ok(driver);
        }

        // GET: api/drivers/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetDriversByVehicle(Guid vehicleId)
        {
            var drivers = await _context.Drivers
                .Include(d => d.VehicleAssessment)
                .Where(d => d.VehicleId == vehicleId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(drivers);
        }

       

        // POST: api/drivers
        [HttpPost]
        public async Task<IActionResult> CreateDriver([FromBody] DriverDto driver)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if vehicle exists
            var vehicleExists = await _context.VehicleAssessments
                .AnyAsync(v => v.Id == driver.VehicleId);

            if (!vehicleExists)
            {
                return BadRequest($"Vehicle with ID {driver.VehicleId} does not exist.");
            }

            // Check if license number is unique
            var licenseExists = await _context.Drivers
                .AnyAsync(d => d.LicenseNumber == driver.LicenseNumber && d.VehicleId == driver.VehicleId);

            if (licenseExists)
            {
                return BadRequest("License number already exists for this vehicle.");
            }

            var newDriver = new Driver
            {
               
                SerNo = driver.SerNo,
                Address = driver.Address,
                ChassisNumber = driver.ChassisNumber,
                Name = driver.Name,
                LicenseNumber = driver.LicenseNumber,
                VehicleId = driver.VehicleId,
                PhoneNumber = driver.PhoneNumber,
                Rank = driver.Rank,
                UserId = driver.UserId
              
            };

            _context.Drivers.Add(newDriver);
            await _context.SaveChangesAsync();


            _userService.ActivityLog($"Created -Driver for {driver.SerNo} {driver.Name}", driver.VehicleId, driver.UserId);


            return Ok(new {message ="Driver created successfully"});
        }

        // PUT: api/drivers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDriver(Guid id, [FromBody] DriverDto driver)
        {
            if (id == null)
            {
                return BadRequest("ID mismatch.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDriver = await _context.Drivers.FindAsync(id);
            if (existingDriver == null)
            {
                return NotFound($"Driver with ID {id} not found.");
            }

            // Check if vehicle exists
            var vehicleExists = await _context.VehicleAssessments
                .AnyAsync(v => v.Id == driver.VehicleId);

            if (!vehicleExists)
            {
                return BadRequest($"Vehicle with ID {driver.VehicleId} does not exist.");
            }

            // Check if license number is unique (excluding current driver)
            //var licenseExists = await _context.Drivers
            //    .AnyAsync(d => d.LicenseNumber == driver.LicenseNumber && d.Id != id);

            //if (licenseExists)
            //{
            //    return BadRequest("License number already exists.");
            //}

            // Update fields
            existingDriver.SerNo = driver.SerNo;
            existingDriver.Name = driver.Name;
            existingDriver.Address = driver.Address;
            existingDriver.LicenseNumber = driver.LicenseNumber;
            existingDriver.VehicleId = driver.VehicleId;
            existingDriver.PhoneNumber = driver.PhoneNumber;
            existingDriver.Rank = driver.Rank;

            _context.Entry(existingDriver).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _userService.ActivityLog($"Updated -Driver for {driver.SerNo} {driver.Name}", driver.VehicleId, driver.UserId);

            return Ok(new {message="updated"});
        }

       
        // DELETE: api/drivers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDriver(Guid id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                return NotFound($"Driver with ID {id} not found.");
            }

            _userService.ActivityLog($"Driver -Driver {driver.SerNo} {driver.Name}", driver.VehicleId, driver.UserId);

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Driver deleted successfully." });
        }

       

           
    }
}