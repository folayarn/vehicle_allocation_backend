using ClosedXML.Excel;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Seeders
{
    public class ProjectSeeder
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
                    var projectDescription = GetCellValue(row, 3); // Column C - Project Description

                    if (string.IsNullOrWhiteSpace(projectDescription))
                        continue;

                    var asset = new Asset
                    {
                        AssetName = GetNullIfEmpty(projectDescription),
                        Zone = GetNullIfEmpty(GetCellValue(row, 1)),      // Column A - Zone
                        Command = GetNullIfEmpty(GetCellValue(row, 2)),   // Column B - Area Command
                        Location = GetNullIfEmpty(GetCellValue(row, 4)),  // Column D - Location
                        NoOfBuilding = ParseNullableInt(GetCellValue(row, 5)), // Column E - No of Building
                        Category = GetNullIfEmpty(GetCellValue(row, 7)),  // Column G - Category
                                                                          // Replace ParseDate with ParseDateToUtc
                        ConstructionDate = ParseDateToUtc(GetCellValue(row, 9)),     // Column I - Construction/Supply Date
                        RenovationDate = ParseDateToUtc(GetCellValue(row, 11)),     // Column K - Renovation/Rehabilitation Date
                        ConstructionCost = ParseNullableDecimal(GetCellValue(row, 8)),  // Column H - Construction/Supply Cost
                        RenovationCost = ParseNullableDecimal(GetCellValue(row, 10)), // Column J - Renovation/Rehabilitation Cost
                        Remark = GetNullIfEmpty(GetCellValue(row, 13)),    // Column M - Remark
                        AssetStatus = GetAssetStatus(GetCellValue(row, 12)), // Column L - Status
                        AssetType = "project"
                    };

                    // Map Category to BuildingType if needed
                    if (!string.IsNullOrWhiteSpace(asset.Category))
                    {
                        asset.BuildingType = asset.Category;
                    }

                    // Set default values for required fields
                    if (string.IsNullOrWhiteSpace(asset.Remark))
                    {
                        asset.Remark = GetNullIfEmpty(GetCellValue(row, 12)) ?? "No remarks provided";
                    }

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

        private static decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Remove currency symbols, commas, and other non-numeric characters
            var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

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

            // Try parsing various date formats
            string[] formats = {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd MMM, yyyy",
                "MMM, yyyy",
                "dd/MM/yyyy",
                "dd-MM-yyyy",
                "dd MMM yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                    return result;
            }

            // Fallback to standard parsing
            if (DateTime.TryParse(value, out DateTime fallbackResult))
                return fallbackResult;

            return null;
        }

        private static string GetAssetStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "ongoing";

            var lowerStatus = status.ToLower().Trim();

            if (lowerStatus.Contains("abandoned"))
                return "abandoned";
            if (lowerStatus.Contains("ongoing") || lowerStatus.Contains("in progress") || lowerStatus.Contains("work is currently ongoing"))
                return "ongoing";
            if (lowerStatus.Contains("completed"))
                return "completed";
            if (lowerStatus.Contains("yes") || lowerStatus.Contains("new"))
                return "active";

            return "ongoing";
        }
    }
}