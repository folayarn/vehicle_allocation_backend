using ClosedXML.Excel;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Seeders
{
    public class ElectricalSeeder
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
                    var equipmentName = GetCellValue(row, 3); // Column C - Equipment Name
                    var equipmentDescription = GetCellValue(row, 4); // Column D - Equipment Description

                    if (string.IsNullOrWhiteSpace(equipmentName) && string.IsNullOrWhiteSpace(equipmentDescription))
                        continue;

                    // Build asset name combining equipment name and description
                    var assetName = equipmentName;
                    if (!string.IsNullOrWhiteSpace(equipmentDescription) && !equipmentDescription.Equals(equipmentName))
                    {
                        assetName = string.IsNullOrWhiteSpace(assetName)
                            ? equipmentDescription
                            : $"{equipmentName} - {equipmentDescription}";
                    }

                    var asset = new Asset
                    {
                        AssetName = GetNullIfEmpty(assetName),
                        Description = GetNullIfEmpty(equipmentDescription),
                        Zone = GetNullIfEmpty(GetCellValue(row, 1)),      // Column A - Zone
                        Command = GetNullIfEmpty(GetCellValue(row, 2)),   // Column B - Area Command
                        Capacity = ParseCapacity(GetCellValue(row, 5)),   // Column E - Capacity/Rating (kVA)
                        Location = GetNullIfEmpty(GetCellValue(row, 6)),  // Column F - Location
                        BrandName = GetNullIfEmpty(GetCellValue(row, 7)), // Column G - Brand Name
                                                                          // Replace ParseDate with ParseDateToUtc
                        AcquisitionDate = ParseDateToUtc(GetCellValue(row, 11)),      // Column K - Acquisition Date
                        SerialNumber = GetNullIfEmpty(GetCellValue(row, 8)), // Column H - Serial No.
                        InsurancePolicyNo = GetNullIfEmpty(GetCellValue(row, 9)), // Column I - Insurance Policy No.
                        AcquisitionCost = ParseNullableDecimal(GetCellValue(row, 10)), // Column J - Acquisition Cost
                        Condition = ParseCondition(GetCellValue(row, 12)), // Column L - Current Condition
                        Remark = GetNullIfEmpty(GetCellValue(row, 13)),    // Column M - Remark
                        AssetType = "electrical",

                        // Map additional fields
                        AssetStatus = ParseAssetStatus(GetCellValue(row, 12)), // From Condition column
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

        private static string? ParseCapacity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Extract kVA value from strings like "500kVA", "500 KVA", "500kva", etc.
            var cleaned = value.ToLower().Trim();

            // Remove "kva" or "kvar" suffix
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"kva$|kvar$|kva\s|kvar\s", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            cleaned = cleaned.Trim();

            // Return the cleaned capacity string
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                // If it's just a number, add "kVA" suffix for consistency
                if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    return $"{cleaned} kVA";
                }
                return cleaned;
            }

            return value;
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
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(cleaned, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                    return result;
            }

            // Handle dates like "19/08/2024" or "15/2/2026"
            if (DateTime.TryParse(cleaned, out DateTime fallbackResult))
                return fallbackResult;

            return null;
        }

        private static string ParseCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return "unknown";

            var lowerCondition = condition.ToLower().Trim();

            if (lowerCondition.Contains("serviceable") || lowerCondition.Contains("good") || lowerCondition.Contains("operational"))
                return "good";
            if (lowerCondition.Contains("non-serviceable") || lowerCondition.Contains("non serviceable"))
                return "poor";
            if (lowerCondition.Contains("faulty"))
                return "faulty";
            if (lowerCondition.Contains("decommissioned"))
                return "decommissioned";
            if (lowerCondition.Contains("fair"))
                return "fair";

            return lowerCondition;
        }

        private static string ParseAssetStatus(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return "serviceable";

            var lowerCondition = condition.ToLower().Trim();

            if (lowerCondition.Contains("serviceable") || lowerCondition.Contains("good") || lowerCondition.Contains("operational"))
                return "serviceable";
            if (lowerCondition.Contains("non-serviceable") || lowerCondition.Contains("non serviceable"))
                return "unserviceable";
            if (lowerCondition.Contains("faulty"))
                return "faulty";
            if (lowerCondition.Contains("decommissioned"))
                return "decommissioned";

            return "serviceable";
        }
    }
}