// Models/FuelRequest.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class FuelRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid VehicleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string RequesterName { get; set; }

        [Column(TypeName = "text")]
        public string? Command { get; set; }

        [Column(TypeName = "text")]
        public string? Zone { get; set; }
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string? Reason { get; set; }

        [Required]
        public DateTime RequiredDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal RequestedQuantity { get; set; } // In liters/gallons

        [Range(0, double.MaxValue)]
        public decimal ApprovedQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string FuelType { get; set; } // Petrol, Diesel, etc.

        [Required]
        public int CurrentMileage { get; set; }


        [Required]
        [StringLength(500)]
        public string Purpose { get; set; }

   
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Dispensed

        [StringLength(500)]
        public string? Remarks { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public Guid? DispensedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DispensedDate { get; set; }

        [Required]
        public Guid UserId { get; set; } // Request creator

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("VehicleId")]
        public virtual VehicleAssessment VehicleAssessment { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

       
    }

  
}