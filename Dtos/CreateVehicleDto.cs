namespace Vehicle_Information_System.Dtos
{
    // Create Vehicle DTO
  
    public class CreatedVehicleDto
    {
        public Guid? Id { get; set; }
        public string RegistrationNumber { get; set; }
        public string ChassisNumber { get; set; }
        public int? YearOfAllocation { get; set; }
        public string VehicleTypeModel { get; set; }
        public string EngineNumber { get; set; }
        public string VehicleLocation { get; set; }
        public string Command { get; set; }
        public string Zone { get; set; }
        public string Condition { get; set; }
        public string Remark { get; set; }
        public string Comments { get; set; }
        public string PictureA { get; set; }
        public string PictureB { get; set; }
        public string PictureC { get; set; }
        public string PictureD { get; set; }
        public string PictureE { get; set; }
    }

   
}
