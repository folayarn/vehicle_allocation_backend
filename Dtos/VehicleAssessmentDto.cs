using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Dtos
{
    public class VehicleAssessmentDto
    {
        public string Condition { get; set; }
        public string? ChassisNumber { get; set; }
        public string? VehicleTypeModel { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Command { get; set; }
        public string? Zone { get; set; }
        public decimal InitialMileage { get; set; }
        public string? Comments { get; set; }
        public string? Remark { get; set; }
        public string? EngineNumber { get; set; }
        public string? VehicleLocation { get; set; }

        // Image files
        public IFormFile? PictureAFile { get; set; }
        public IFormFile? PictureBFile { get; set; }
        public IFormFile? PictureCFile { get; set; }
        public IFormFile? PictureDFile { get; set; }
        public IFormFile? PictureEFile { get; set; }
    }

    public class PicturePaths
    {
        public string? PictureA { get; set; }
        public string? PictureB { get; set; }
        public string? PictureC { get; set; }
        public string? PictureD { get; set; }
        public string? PictureE { get; set; }
    }
}
