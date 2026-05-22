namespace Vehicle_Information_System.Dtos
{
    public class DriverDto
    {
        
        public string SerNo { get; set; }
        public string Address { get; set; }
        public string ChassisNumber { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string LicenseNumber { get; set; }
        public Guid VehicleId { get; set; }
        public string PhoneNumber { get; set; }
        public string Rank { get; set; }
    }
}
