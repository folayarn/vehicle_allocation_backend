using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Remark
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column(TypeName = "text")]
        public string RemarkText { get; set; }
        public string? ChassisNumber { get; set; }

        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;
    }
}
