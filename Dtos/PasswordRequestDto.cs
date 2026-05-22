namespace Vehicle_Information_System.Dtos
{
    public class PasswordRequestDto
    {
      
            public string? OldPassword { get; set; }
            public string NewPassword { get; set; }
        public Guid OfficerId { get; set; } 
       
    }
}
