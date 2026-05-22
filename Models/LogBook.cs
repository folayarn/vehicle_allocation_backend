using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class LogBook
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string From { get; set; }
        public string To { get; set; }
        public decimal MileageBefore { get; set; }
        public decimal MileageAfter { get; set; }
        public decimal MileageTotal { get; set; }

        public string? OfficerCarried { get; set; }
        public string Purpose { get; set; }

        public string TimeOut { get; set; }
        public string TimeIn { get; set; }
        public string Status { get; set; }

 
        [Column(TypeName = "text")]
        public string Remarks { get; set; }

        [Column(TypeName = "text")]
        public string? ReasonForRjection { get; set; }

        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;
    }

    
}
