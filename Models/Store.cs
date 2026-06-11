using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Models
{
    public class Store : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string StoreCode { get; set; }

        [Required]
        [StringLength(100)]
        public string StoreName { get; set; }

        [StringLength(50)]
        public string StoreType { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public string? Command { get; set; }

        public Guid? OfficerInCharge { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "active";

        public virtual ICollection<StockItem> StockItems { get; set; }
        public virtual ICollection<StockIssue> StockIssues { get; set; }
    }
}
