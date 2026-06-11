using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class AccommodationUnit: BaseEntity
    {
        public Guid BarrackId { get; set; }
        public string UnitNumber { get; set; }
        public string UnitType { get; set; }
        public string UnitDescription { get; set; }
        public int BedRoomCount { get; set; }
        public string Status { get; set; } = "Active";
      

        [ForeignKey("BarrackId")]

        public virtual Barrack Barrack { get; set; }
        public virtual ICollection<Allocation> Allocations { get; set; }
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }

    }
}
