using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class AccommodationAllocation:BaseEntity
    {
       
        [Required]
        public Guid RequestId { get; set; }

        [Required]
        public Guid StaffId { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid BarracksId { get; set; }

        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        public DateTime? EffectiveDate { get; set; }


        [StringLength(30)]
        public string Status { get; set; } = "active";

        public DateTime? VacatedDate { get; set; }

        public string? VacatedReason { get; set; }

        [ForeignKey("RequestId")]
        public virtual AccommodationRequest Request { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("UnitId")]
        public virtual AccommodationUnit Unit { get; set; }

        [ForeignKey("BarracksId")]
        public virtual Barrack Barracks { get; set; }

    }
}
