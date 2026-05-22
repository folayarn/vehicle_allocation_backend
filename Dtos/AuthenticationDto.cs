using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Dtos
{
    public class AuthenticationDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
