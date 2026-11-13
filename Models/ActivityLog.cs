using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehicle_Information_System.Models
{
    public class ActivityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Action { get; set; }

        public Guid? TargetId { get; set; }

        public string TargetType { get; set; }


        public Guid? UserId { get; set; }
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}
