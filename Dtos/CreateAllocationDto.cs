using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Dtos
{
    public class CreateAllocationDto
    {
        [Required]
        public Guid VehicleId { get; set; }

        
        public Guid UserId { get; set; }

        public string? ChassisNumber { get; set; }

        
        [MaxLength(100)]
        public string? OfficerName { get; set; }

        
        [MaxLength(50)]
        public string? OfficerSerNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Type { get; set; }

        
        [MaxLength(50)]
        public string? Rank { get; set; }

        [Required]
        [MaxLength(100)]
        public string Command { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Zone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Office { get; set; }

        [MaxLength(100)]
        public string? Unit { get; set; }

        
        public IFormFile? FilePath { get; set; }


        [Required]
        public int YearOfAllocation { get; set; }
    }

    public class AllocationResponseDto
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public string ChassisNumber { get; set; } = string.Empty;
        public string? OfficerName { get; set; }
        public string? OfficerSerNo { get; set; }
        public string? Type { get; set; }
        public string? Rank { get; set; }
        public string Command { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Office { get; set; }
        public string? Unit { get; set; }
        public int YearOfAllocation { get; set; }
        public DateTime CreatedAt { get; set; }
        public VehicleAssessmentDto? Vehicle { get; set; }
    }


}
