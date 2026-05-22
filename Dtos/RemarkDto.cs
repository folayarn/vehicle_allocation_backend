using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Dtos
{
    public class RemarkDto
    {
        

        [Column(TypeName = "text")]
        public string RemarkText { get; set; }
                public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
    }
}
