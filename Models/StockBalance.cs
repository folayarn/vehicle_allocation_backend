using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class StockBalance : BaseEntity
    {
        [Required]
        public Guid StockItemId { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        public int CurrentQuantity { get; set; } = 0;

        public int ReservedQuantity { get; set; } = 0;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int AvailableQuantity => CurrentQuantity - ReservedQuantity;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey("StockItemId")]
        public virtual StockItem StockItem { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }
    }
}
