using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Allocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public string ChassisNumber { get; set; }
        public string? OfficerName { get; set; } = string.Empty;
        public string? OfficerSerNo { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Rank { get; set; } = string.Empty;
        public string Command { get; set; }
        public string Zone { get; set; }
        public string? Department { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Office { get; set; } = string.Empty;
        public string ? Unit { get; set; } = string.Empty;
        public int YearOfAllocation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;




    }
}
