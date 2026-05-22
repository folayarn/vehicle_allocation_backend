using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class MaintenanceReport
    {
        [Key]
        public Guid Id { get; set; } =  Guid.NewGuid();
        public string Title { get; set; }
        [Column(TypeName = "text")]

      public decimal  LastMaintenanceMileage { get; set; }
        public string Body { get; set; }
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Created { get; set; }= DateTime.UtcNow;
        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;

    }
}
