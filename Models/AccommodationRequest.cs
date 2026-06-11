using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class AccommodationRequest:BaseEntity
    {
        [Required]
        public Guid StaffId { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public Guid? PreferredBarracksId { get; set; }

        [StringLength(50)]
        public string? PreferredUnitType { get; set; }

        public string? Reason { get; set; }

        // Approval levels
        [StringLength(30)]
        public string CoStatus { get; set; } = "pending";

        [StringLength(30)]
        public string QsStatus { get; set; } = "pending";

        [StringLength(30)]
        public string AcsStatus { get; set; } = "pending";

        [StringLength(30)]
        public string CompStatus { get; set; } = "pending";

        public string? CoComment { get; set; }
        public string? QsComment { get; set; }
        public string? AcsComment { get; set; }
        public string? CompComment { get; set; }

        public Guid? CoApprovedBy { get; set; }
        public Guid? QsApprovedBy { get; set; }
        public Guid? AcsApprovedBy { get; set; }
        public Guid? CompApprovedBy { get; set; }

        public DateTime? CoApprovalDate { get; set; }
        public DateTime? QsApprovalDate { get; set; }
        public DateTime? AcsApprovalDate { get; set; }
        public DateTime? CompApprovalDate { get; set; }

        [StringLength(30)]
        public string OverallStatus { get; set; } = "pending";

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("PreferredBarracksId")]
        public virtual Barrack PreferredBarracks { get; set; }

        public virtual Allocation Allocation { get; set; }
    }
}
