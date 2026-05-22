using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemarksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public RemarksController(ApplicationDbContext context,UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/remarks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Remark>>> GetRemarks()
        {
            var remarks = await _context.Remarks
                .Include(r => r.VehicleAssessment)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(remarks);
        }

        // GET: api/remarks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Remark>> GetRemark(Guid id)
        {
            var remark = await _context.Remarks
                .Include(r => r.VehicleAssessment)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remark == null)
            {
                return NotFound($"Remark with ID {id} not found.");
            }

            return Ok(remark);
        }

        // GET: api/remarks/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<Remark>>> GetRemarksByVehicle(Guid vehicleId)
        {
            var remarks = await _context.Remarks
                
                .Where(r => r.VehicleId == vehicleId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (!remarks.Any())
            {
                return Ok(new List<Remark>()); // Return empty list instead of 404
            }

            return Ok(remarks);
        }

      


      

        // POST: api/remarks
        [HttpPost]
        public async Task<IActionResult> CreateRemark([FromBody] RemarkDto remark)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(remark.RemarkText))
            {
                return BadRequest("Remark text is required.");
            }

            // Check if vehicle exists
            var vehicleExists = await _context.VehicleAssessments.FindAsync(remark.VehicleId);

            if (vehicleExists == null)
            {
                return BadRequest($"Vehicle with ID {remark.VehicleId} does not exist.");
            }

            // Set additional properties
            var newRemark = new Remark
            {
                
                RemarkText = remark.RemarkText,
                ChassisNumber = vehicleExists.ChassisNumber,
                VehicleId = remark.VehicleId ,
                UserId = remark.UserId, 
            };
              

            _context.Remarks.Add(newRemark);
            await _context.SaveChangesAsync();
            _userService.ActivityLog($"Created Remarks {remark.RemarkText}", remark.VehicleId, remark.UserId);




            return Ok(new {message= "Remark added successfully"});
        }

       

        // PUT: api/remarks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRemark(Guid id, [FromBody] RemarkDto remark)
        {
            if (id == null)
            {
                return BadRequest("ID mismatch.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingRemark = await _context.Remarks.FindAsync(id);
            if (existingRemark == null)
            {
                return NotFound($"Remark with ID {id} not found.");
            }

            // Only allow updating RemarkText and ChassisNumber
            existingRemark.RemarkText = remark.RemarkText;

           

            _context.Entry(existingRemark).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _userService.ActivityLog($"Updated Remarks {remark.RemarkText}", remark.VehicleId, remark.UserId);

            return Ok(new {message= "Remark updated successfully."});
        }

      

        // DELETE: api/remarks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRemark(Guid id)
        {
            var remark = await _context.Remarks.FindAsync(id);
            if (remark == null)
            {
                return NotFound($"Remark with ID {id} not found.");
            }

            _userService.ActivityLog($"deleted Remarks {remark.RemarkText}", remark.VehicleId, remark.UserId);

            _context.Remarks.Remove(remark);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Remark deleted successfully." });
        }

      
      
    }
}