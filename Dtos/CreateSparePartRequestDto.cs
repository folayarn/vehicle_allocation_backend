// CreateSparePartRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Vehicle_Information_System.Dtos
{
    public class CreateSparePartRequestDto
    {
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public string? Priority { get; set; } = "Medium";
        
        public string? RequestType { get; set; } = "Maintenance";
        public string RequiredByDate { get; set; }
        public bool IsUrgent { get; set; } = false;
        
        public List<CreateItemDto> Items { get; set; } = new List<CreateItemDto>();
    }

    public class CreateItemDto
    {
       
        public string Brand { get; set; }
        public string Category { get; set; }
        public int QuantityRequested { get; set; } = 1;
        
        public string? UnitOfMeasure { get; set; } = "Pcs";
      
        public string Specification { get; set; }
        public bool IsCritical { get; set; } = false;
    }



    public class UpdateSparePartRequestDto
    {
        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }
        public string? Priority { get; set; } = "Medium";

        public string? RequestType { get; set; } = "Maintenance";
        public string RequiredByDate { get; set; }
        public bool IsUrgent { get; set; } = false;

        public List<UpdateItemDto> Items { get; set; } = new List<UpdateItemDto>();
    }

    public class UpdateItemDto
    {
        public Guid? Id { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int QuantityRequested { get; set; } = 1;

        public string? UnitOfMeasure { get; set; } = "Pcs";

        public string Specification { get; set; }
        public bool IsCritical { get; set; } = false;
    }
    public class RejectSparePartRequestDto
    {
        public Guid Id { get; set; }
        public Guid RejectedByUserId { get; set; }
        public string RejectionReason { get; set; }
    }

    public class ApproveSparePartRequestDto
    {
        public Guid ApprovedByUserId { get; set; }
        public string? ApprovalRemarks { get; set; }
        
       

        public Guid VehicleId { get; set; }
        public Guid UserId { get; set; }


        public List<ApprovalItem> Items { get; set; } = new List<ApprovalItem>();



    }

    public class ApprovalItem {

        public Guid Id { get; set; }
        public int? QuantityApproved { get; set; }
        public string? PartNumber { get; set; } // OEM or manufacturer part number
        public decimal? UnitPrice { get; set; }
        
        public string? SupplierName { get; set; } // Preferred supplier

       
        public string? SupplierPartNumber { get; set; } // Supplier's part number

        public bool? IsStockItem { get; set; }

    }

}