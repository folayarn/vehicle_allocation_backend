namespace Vehicle_Information_System.Models
{
    public class Staff: BaseEntity
    {
        public string FullName { get; set; }
        public string ServiceNumber { get; set; }
        public DateTime HireDate { get; set; }
        public string Rank { get; set; }
        public string Phone { get; set; }
        public string Zone { get; set; }
        public string? Command { get; set; }
        public string Email { get; set; }
        public string MaritalStatus { get; set; }
    }
}
