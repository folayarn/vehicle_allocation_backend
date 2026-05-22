using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Item
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }

        // New fields
        [StringLength(50)]
        public string? PartNumber { get; set; } // OEM or manufacturer part number

        [StringLength(100)]
        public string Brand { get; set; } // Brand name

        [StringLength(50)]
        public string Category { get; set; } // Engine, Brake, Electrical, Body, etc.

        public int QuantityRequested { get; set; } = 1;

        public int? QuantityApproved { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalPrice => QuantityRequested * UnitPrice;

        [StringLength(50)]
        public string UnitOfMeasure { get; set; } = "Pcs"; // Pcs, Box, Set, Liter, Kg

        [StringLength(100)]
        public string? SupplierName { get; set; } // Preferred supplier

        [StringLength(200)]
        public string? SupplierPartNumber { get; set; } // Supplier's part number

        public bool? IsStockItem { get; set; } 

             

        [StringLength(500)]
        public string Specification { get; set; } // Technical specifications

        public bool IsCritical { get; set; } = false; // Critical for vehicle operation

        [StringLength(50)]
        public string ItemStatus { get; set; } = "Pending"; // Pending, Approved, Rejected, Ordered, Received, Installed


        // For tracking
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        // Navigation property
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; } = null!;

        public Guid? SparePartRequestId { get; set; }

        [ForeignKey("SparePartRequestId")]
        public virtual SparePartRequest? SparePartRequest { get; set; }
    }
}