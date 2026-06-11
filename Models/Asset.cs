using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Asset : BaseEntity
    {
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [Required]
        [Column(TypeName = "text")]  // Unlimited text for remarks
        public string? AssetName { get; set; }

        [Column(TypeName = "text")]  // Unlimited text for remarks
        public string? Remark { get; set; }

        [Column(TypeName = "text")]  // Unlimited text for description
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Zone { get; set; }

        [StringLength(200)]
        public string? Command { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RenovationCost { get; set; }

        public DateTime? RenovationDate { get; set; }

        [Column(TypeName = "text")]  // Unlimited text for remarks
        public string? BrandName { get; set; }

        [Column(TypeName = "text")]  // Location can be long
        public string? Location { get; set; }

        public int? NoOfBuilding { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ConstructionCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LastRenovationCost { get; set; }

        [Column(TypeName = "text")]  // Physical condition can be descriptive
        public string? CurrentPhysicalCondition { get; set; }

        [Column(TypeName = "text")]
        public string? AssetStatus { get; set; } = "serviceable";

        [Column(TypeName = "text")]  // Unlimited text for remarks

        public string? Condition { get; set; } = "good";

        public DateTime? ConstructionDate { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? BuildingType { get; set; }

        public bool? AvailableDocument { get; set; }

        [Column(TypeName = "text")]  // Litigation status can be detailed
        public string? LitigationStatus { get; set; }

        [StringLength(100)]
        public string? Capacity { get; set; }

        [StringLength(50)]
        public string? AssetType { get; set; } = "project";

        public DateTime? AcquisitionDate { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? AcquisitionCost { get; set; }

        [Column(TypeName = "text")]  // Physical location can be detailed
        public string? PhysicalLocation { get; set; }

        [Column(TypeName = "text")]  // Unlimited text for remarks
        public string? InsurancePolicyNo { get; set; }
    }
}