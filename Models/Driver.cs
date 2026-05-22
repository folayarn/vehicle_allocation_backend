using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Driver
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SerNo { get; set; }
        public string Address { get; set; }
        public Guid UserId { get; set; }

        public string ChassisNumber { get; set; }
        public string Name { get; set; }
        public string LicenseNumber { get; set; }
        public Guid VehicleId { get; set; }
        public string PhoneNumber { get; set; }
        public string Rank { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;
    }
}
