using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class StockItem : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; }

        public string? Description { get; set; }

        public Guid CategoryId { get; set; }

        [StringLength(30)]
        public string UnitOfMeasure { get; set; }

        public int ReorderLevel { get; set; } = 0;

        public int ReorderQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal UnitCost { get; set; }

        public Guid? SupplierId { get; set; }

        [StringLength(100)]
        public string? LocationInStore { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "active";

      

        public virtual ICollection<StockBalance> StockBalances { get; set; }
    }
}
