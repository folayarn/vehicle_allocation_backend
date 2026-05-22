using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class VehicleAssessment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column(TypeName = "text")]
        public string Timestamp { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? RegistrationNumber { get; set; }

        [Column(TypeName = "text")]
        public string? ChassisNumber { get; set; }
        

        [Column(TypeName = "text")]
        public string? VehicleTypeModel { get; set; }

        [Column(TypeName = "text")]
        public string? EngineNumber { get; set; }
        [Column(TypeName = "text")]
        public string? VehicleLocation { get; set; }

        [Column(TypeName = "text")]
        public string? Command { get; set; }

        [Column(TypeName = "text")]
        public string? Zone { get; set; }

        [Column(TypeName = "text")]
        public string Condition { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Remark { get; set; }
        public string? MaintenanceStatus { get; set; }
        [Column(TypeName = "text")]
        public string? Comments { get; set; }

        [Column(TypeName = "text")]
        public string? PictureA { get; set; }

        [Column(TypeName = "text")]
        public string? PictureB { get; set; }

        [Column(TypeName = "text")]
        public string? PictureC { get; set; }

        [Column(TypeName = "text")]
        public string? PictureD { get; set; }

        [Column(TypeName = "text")]
        public string? PictureE { get; set; }

        [Column(TypeName = "text")]
        public string? RecommendedAction { get; set; }

        public decimal? CurrentMileage { get; set; }
        public decimal? InitialMileage { get; set; }
        public decimal? LastMaintenanceMileage { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public int? MaintenanceDueInKm { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        [Column(TypeName = "text")]
        public string? ImagePath { get; set; }
        public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
        public virtual ICollection<Driver> Drivers { get; set; } = null!;
        public virtual ICollection<Remark> Remarks { get; set; } = null!;
        public virtual ICollection<MaintenanceReport> MaintenanceReports { get; set; } = null!;
        public virtual ICollection<LogBook> LogBooks { get; set; } = null!;
        public virtual ICollection<IncidentReport> IncidentReports { get; set; } = null!;
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}