namespace Vehicle_Information_System.Dtos
{
    public class CreateIncidentReportDto
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public IFormFile? FilePath { get; set; }
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
    }
}
