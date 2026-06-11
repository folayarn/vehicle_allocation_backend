using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Models
{
    public class BaseEntity
    {
      
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            


    
}
}
