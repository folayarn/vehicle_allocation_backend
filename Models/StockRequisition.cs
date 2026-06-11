using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class StockRequisition : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string RequisitionNumber { get; set; }

        public DateTime RequisitionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid RequestedBy { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        public string? Department { get; set; }

        public string? Command { get; set; }

        [StringLength(30)]
        public string Priority { get; set; } = "normal";

        [StringLength(100)]
        public string? Purpose { get; set; }

        public Guid? RelatedAssetId { get; set; }

        public Guid? RelatedAllocationId { get; set; }

        [StringLength(30)]
        public string RequisitionStatus { get; set; } = "draft";

        public Guid? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public string? Remarks { get; set; }

        [ForeignKey("RequestedBy")]
        public virtual StoreUser Requester { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        public virtual ICollection<StockRequisition> RequisitionItems { get; set; }
        public virtual StockIssue StockIssue { get; set; }
    }
}
