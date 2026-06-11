using ClosedXML.Excel;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Seeders
{
    public class LandSeeder
    {
        public static List<Asset> GetSeedData(string excelFilePath)
        {
            var assets = new List<Asset>();

            if (!File.Exists(excelFilePath))
                return assets;

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                foreach (var row in rows)
                {
                    var buildingDescription = GetCellValue(row, 3); // Column C - Building/Land Description

                    if (string.IsNullOrWhiteSpace(buildingDescription))
                        continue;

                    var asset = new Asset
                    {
                        AssetName = GetNullIfEmpty(buildingDescription),
                        Zone = GetNullIfEmpty(GetCellValue(row, 1)),      // Column A - Zone
                        Command = GetNullIfEmpty(GetCellValue(row, 2)),   // Column B - Area Command
                        Location = GetNullIfEmpty(GetCellValue(row, 4)),  // Column D - Location
                        AvailableDocument = ParseAvailableDocuments(GetCellValue(row, 5)), // Column E - Available Documents
                        NoOfBuilding = ParseNullableInt(GetCellValue(row, 6)), // Column F - No of Building
                        Category = GetNullIfEmpty(GetCellValue(row, 7)),  // Column G - Category
                        ConstructionCost = ParseNullableDecimal(GetCellValue(row, 8)),  // Column H - Construction Cost
                        LastRenovationCost = ParseNullableDecimal(GetCellValue(row, 10)), // Column J - Last Renovation Cost
                        CurrentPhysicalCondition = GetNullIfEmpty(GetCellValue(row, 12)), // Column L - Current Physical Condition
                        AcquisitionCost = ParseNullableDecimal(GetCellValue(row, 13)), // Column M - Acquisition/Allocation Cost
                        Capacity = GetNullIfEmpty(GetCellValue(row, 15)),   // Column O - Size
                        LitigationStatus = GetNullIfEmpty(GetCellValue(row, 16)), // Column P - Litigation Status
                        Remark = GetNullIfEmpty(GetCellValue(row, 17)),      // Column Q - Remark
                                                                             // Replace ParseDate with ParseDateToUtc
                        ConstructionDate = ParseDateToUtc(GetCellValue(row, 9)),      // Column I - Construction Date
                        RenovationDate = ParseDateToUtc(GetCellValue(row, 11)),       // Column K - Last Renovation Date
                        AcquisitionDate = ParseDateToUtc(GetCellValue(row, 14)),      // Column N - Acquisition/Allocation Date
                        AssetType = "land",

                        // Map additional fields
                        BuildingType = GetBuildingType(GetCellValue(row, 7)), // Derive from Category
                        AssetStatus = GetAssetStatus(GetCellValue(row, 12)),  // Derive from Condition
                        Condition = GetCondition(GetCellValue(row, 12)),      // Map condition
                    };

                    assets.Add(asset);
                }
            }

            return assets;
        }
        private static DateTime? ParseDateToUtc(string value)
        {
            var date = ParseDate(value);
            if (date.HasValue)
            {
                // Convert to UTC with DateTimeKind.Utc
                return DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
            }
            return null;
        }
        private static string GetCellValue(IXLRow row, int columnIndex)
        {
            var cell = row.Cell(columnIndex);
            return cell != null && !cell.IsEmpty() ? cell.GetString()?.Trim() ?? string.Empty : string.Empty;
        }

        private static string? GetNullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static bool? ParseAvailableDocuments(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var lowerValue = value.ToLower().Trim();

            if (lowerValue == "yes" || lowerValue == "y" || lowerValue == "available" || lowerValue == "processing documents")
                return true;

            if (lowerValue == "no" || lowerValue == "n" || lowerValue == "nil" || lowerValue == "na" || lowerValue == "n/a")
                return false;

            // "Processing documents" suggests documents are being worked on
            if (lowerValue.Contains("processing") || lowerValue.Contains("documents"))
                return true;

            return null;
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Remove currency symbols, commas, and other non-numeric characters
            var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

            if (string.IsNullOrWhiteSpace(cleaned))
                return null;

            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;

            return null;
        }

        private static int? ParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = value.Replace(".0", "").Trim();

            if (int.TryParse(cleaned, out int result))
                return result;

            return null;
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Remove any trailing .0 if present
            var cleaned = value.Replace(".0", "").Trim();

            // Try parsing various date formats
            string[] formats = {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd MMM, yyyy",
                "MMM, yyyy",
                "dd/MM/yyyy",
                "dd-MM-yyyy",
                "dd MMM yyyy",
                "yyyy",
                "dd/MM/yyyy HH:mm:ss"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(cleaned, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                    return result;
            }

            // Fallback to standard parsing
            if (DateTime.TryParse(cleaned, out DateTime fallbackResult))
                return fallbackResult;

            return null;
        }

        private static string? GetBuildingType(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return null;

            var lowerCategory = category.ToLower().Trim();

            if (lowerCategory.Contains("residential"))
                return "Residential";
            if (lowerCategory.Contains("administrative") || lowerCategory.Contains("admin"))
                return "Administrative";
            if (lowerCategory.Contains("religious") || lowerCategory.Contains("mosque") || lowerCategory.Contains("chapel"))
                return "Religious";
            if (lowerCategory.Contains("recreational") || lowerCategory.Contains("sporting"))
                return "Recreational";
            if (lowerCategory.Contains("commercial"))
                return "Commercial";
            if (lowerCategory.Contains("medical") || lowerCategory.Contains("clinic"))
                return "Medical";
            if (lowerCategory.Contains("institutional"))
                return "Institutional";
            if (lowerCategory.Contains("land"))
                return "Land";

            return category;
        }

        private static string GetAssetStatus(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return "active";

            var lowerCondition = condition.ToLower().Trim();

            if (lowerCondition.Contains("dilapidated") || lowerCondition.Contains("delapidated"))
                return "dilapidated";
            if (lowerCondition.Contains("abandoned"))
                return "abandoned";
            if (lowerCondition.Contains("ongoing") || lowerCondition.Contains("in progress") || lowerCondition.Contains("work in progress"))
                return "ongoing";
            if (lowerCondition.Contains("good") || lowerCondition.Contains("functional"))
                return "serviceable";
            if (lowerCondition.Contains("fair"))
                return "fair";
            if (lowerCondition.Contains("poor") || lowerCondition.Contains("bad"))
                return "poor";
            if (lowerCondition.Contains("renovation") || lowerCondition.Contains("needs"))
                return "needs_renovation";

            return "active";
        }

        private static string GetCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return "unknown";

            var lowerCondition = condition.ToLower().Trim();

            if (lowerCondition.Contains("good"))
                return "good";
            if (lowerCondition.Contains("fair"))
                return "fair";
            if (lowerCondition.Contains("dilapidated") || lowerCondition.Contains("delapidated"))
                return "poor";
            if (lowerCondition.Contains("abandoned"))
                return "abandoned";
            if (lowerCondition.Contains("ongoing"))
                return "under_construction";
            if (lowerCondition.Contains("renovation"))
                return "under_renovation";
            if (lowerCondition.Contains("functional"))
                return "good";

            return condition.ToLower();
        }
    }
}