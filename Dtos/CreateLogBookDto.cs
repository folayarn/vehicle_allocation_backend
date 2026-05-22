// CreateLogBookDto.cs
namespace Vehicle_Information_System.Dtos
{
    public class CreateLogBookDto
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal MileageBefore { get; set; }
        public decimal MileageAfter { get; set; }
        public string OfficerCarried { get; set; }
        public string Purpose { get; set; }
        public string TimeOut { get; set; }
        public string TimeIn { get; set; }
    
        public string Remarks { get; set; }
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
    }
}