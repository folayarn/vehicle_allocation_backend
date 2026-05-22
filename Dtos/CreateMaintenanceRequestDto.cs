// CreateMaintenanceRequestDto.cs
namespace Vehicle_Information_System.Dtos
{
    public class CreateMaintenanceRequestDto
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Status { get; set; } = "Pending";
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
    }
}