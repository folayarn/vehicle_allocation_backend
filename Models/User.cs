using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid UserId { get; set; } = Guid.NewGuid();

       
        [StringLength(50)]
        public string? Rank { get; set; }

        [Required]
        [StringLength(255)]
        public string Fullname { get; set; }

        [Required]
        [StringLength(11)]
        public string Phone { get; set; }

        [Required]
        public string Zone { get; set; }

        public string? Command { get; set; }
        
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
       
        [Required]
        [StringLength(67)]
        public string Email { get; set; }

        [Required]
        
        public string Password { get; set; }

        public int? PassChange { get; set; }

        
        public string? Svn { get; set; }

        [Required]
        public string AccessLevel { get; set; }
         
        

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        [Required]
        [StringLength(244)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    }
}
