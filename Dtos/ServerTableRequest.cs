namespace Vehicle_Information_System.Dtos
{
    public class ServerTableRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc";
        
    }
}
