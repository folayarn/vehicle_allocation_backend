using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Dtos
{
    public class UserDto
    {
        
        [StringLength(50)]
        public string? Rank { get; set; }

        [Required]
        [StringLength(255)]
        public string Fullname { get; set; }

       public string? Zone { get; set; }
        public string? Password { get; set; }
        [Required]
        [StringLength(11)]
        public string Phone { get; set; }

        [Required]
        public string Command{ get; set; }
       
        public Guid? OfficerId { get; set; }

      
        
        public string? Email { get; set; }
        
        public string? Svn { get; set; }

        
        public string? AccessLevel { get; set; }

    }
}
