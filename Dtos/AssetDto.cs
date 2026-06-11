using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Dtos
{
    public class AssetDto
    {
        

           
            public string? SerialNumber { get; set; }

        public string? AssetType { get; set; } = "project"; //land,electrical,project

          
            public string? AssetName { get; set; }

           
            public string? Remark { get; set; }
            public string? Description { get; set; }
            public string? Zone { get; set; }
            public string? Command { get; set; }
            public decimal? RenovationCost { get; set; }
            public DateTime? RenovationDate { get; set; }


            public string? BrandName { get; set; }
            public string? Location { get; set; }
            public int? NoOfBuilding { get; set; }

            public decimal? ConstructionCost { get; set; }
            public decimal? LastRenovationCost { get; set; }

            public string? CurrentPhysicalCondition { get; set; }


            public DateTime? ConstructionDate { get; set; }
            public string? Category { get; set; }
            public string? BuildingType { get; set; }

            public bool? AvailableDocument { get; set; }

            public string? LitigationStatus { get; set; }

            public string? Capacity { get; set; }




            public DateTime? AcquisitionDate { get; set; }

         
            public decimal? AcquisitionCost { get; set; }

            public string? AssetStatus { get; set; } = "serviceable";

            
            public string? PhysicalLocation { get; set; }

          
            public string? Condition { get; set; } = "good";


            
            public string? InsurancePolicyNo { get; set; }



        }

    }


