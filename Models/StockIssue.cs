using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class StockIssue : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string IssueVoucherNumber { get; set; }

        public Guid? RequisitionId { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid IssuedBy { get; set; }

        [Required]
        public Guid ReceivedBy { get; set; }

        [StringLength(30)]
        public string IssueStatus { get; set; } = "completed";

        [Column(TypeName = "decimal(15,2)")]
        public decimal TotalValue { get; set; }

        public string? Remarks { get; set; }

        [ForeignKey("RequisitionId")]
        public virtual StockRequisition Requisition { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        [ForeignKey("IssuedBy")]
        public virtual StoreUser Issuer { get; set; }

        public virtual ICollection<StockIssue> IssueItems { get; set; }
    }
}
