namespace Vehicle_Information_System.Dtos
{
    public class CreateMaintenanceReportDto
    {
        public string Title { get; set; }
        public string? Body { get; set; }
        public decimal LastMaintenanceMileage { get; set; }

        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
    }

}
