using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class Barrack: BaseEntity
    {
      
        [Required]
        public string BarrackName { get; set; }
            [Required]
            public string Location { get; set; }
        
        [Required]
        public string Status { get; set; } = "Active";
        [Required]
      public long TotalUnits { get; set; }

        public virtual ICollection<AccommodationUnit> AccommodationUnits { get; set; }
    }
}
