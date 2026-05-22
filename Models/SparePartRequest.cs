using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class SparePartRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }

        // New fields
        [StringLength(50)]
        public string RequestNumber { get; set; } // Auto-generated like SPR-2024-001

        [StringLength(20)]
        public string Priority { get; set; } // Low, Medium, High, Urgent

        [StringLength(30)]
        public string Status { get; set; } // Draft, Submitted, Approved, Rejected, InProgress, Completed, Cancelled

        public string RequiredByDate { get; set; } // When parts are needed


        [StringLength(50)]
        public string RequestType { get; set; } // Maintenance, Repair, Emergency, Preventive

        [StringLength(200)]
        public string? ApprovalRemarks { get; set; } // Remarks from approver

        public Guid? ApprovedByUserId { get; set; } // Who approved/rejected

        public DateTime? ApprovedDate { get; set; }


        public bool IsUrgent { get; set; } = false;

        [StringLength(500)]
        public string? RejectionReason { get; set; } // If rejected

        public virtual ICollection<Item> Items { get; set; } = null!;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;

        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedByUser { get; set; }
    }
}