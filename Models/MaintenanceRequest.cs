using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class MaintenanceRequest
    {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        [Column(TypeName = "text")]

        public string Body { get; set; }

        public string status { get; set; }

        [Column(TypeName = "text")]

        public string? remark { get; set; }
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;
    }
}
