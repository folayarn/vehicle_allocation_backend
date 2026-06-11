using ClosedXML.Excel;
using Vehicle_Information_System.Models;

namespace Vehicle_Information_System.Seeders
{
    public static class VehicleAssessmentSeedData
    {
        public static List<VehicleAssessment> GetSeedData(string excelFilePath)
        {
            var vehicles = new List<VehicleAssessment>();

            if (!File.Exists(excelFilePath))
                return vehicles;

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var registrationNumber = GetCellValue(row, 2);
                    var chassisNumber = GetCellValue(row, 3);

                    if (string.IsNullOrWhiteSpace(registrationNumber) && string.IsNullOrWhiteSpace(chassisNumber))
                        continue;

                    var vehicle = new VehicleAssessment
                    {
                        Timestamp = GetCellValue(row, 1),
                        RegistrationNumber = GetNullIfEmpty(registrationNumber),
                        ChassisNumber = GetNullIfEmpty(chassisNumber),
                        VehicleTypeModel = GetNullIfEmpty(GetCellValue(row, 4)),
                        EngineNumber = GetNullIfEmpty(GetCellValue(row, 5)),

                        VehicleLocation = GetNullIfEmpty(GetCellValue(row, 7)),
                        Command = GetNullIfEmpty(GetCellValue(row, 8)),
                        Zone = GetNullIfEmpty(GetCellValue(row, 9)),
                        Condition = GetNullIfEmpty(GetCellValue(row, 10)),
                        Remark = GetNullIfEmpty(GetCellValue(row, 11)),
                        Comments = GetNullIfEmpty(GetCellValue(row, 12)),
                        PictureA = GetNullIfEmpty(GetCellValue(row, 13)),
                        PictureB = GetNullIfEmpty(GetCellValue(row, 14)),
                        PictureC = GetNullIfEmpty(GetCellValue(row, 15)),
                        PictureD = GetNullIfEmpty(GetCellValue(row, 16)),
                        PictureE = GetNullIfEmpty(GetCellValue(row, 17)),
                    };

                    vehicles.Add(vehicle);
                }
            }

            return vehicles;
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

        private static DateTimeOffset ParseTimestamp(string timestamp)
        {
            // Try parsing as DateTimeOffset first (includes timezone info)
            if (DateTimeOffset.TryParse(timestamp, out DateTimeOffset result))
                return result;

            // If no timezone info, try parsing as DateTime and assume UTC
            if (DateTime.TryParse(timestamp, out DateTime dateTime))
            {
                // If the parsed DateTime has no timezone info, assume UTC
                if (dateTime.Kind == DateTimeKind.Unspecified)
                    return new DateTimeOffset(dateTime, TimeSpan.Zero);

                if (dateTime.Kind == DateTimeKind.Local)
                    return new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero);

                return new DateTimeOffset(dateTime, TimeSpan.Zero);
            }

            return DateTimeOffset.UtcNow;
        }

        private static int? ParseYear(string year)
        {
            if (string.IsNullOrWhiteSpace(year))
                return null;

            var cleaned = year.Replace(".0", "").Trim();

            if (int.TryParse(cleaned, out int result) && result > 1900 && result < 2030)
                return result;

            return null;
        }
    }
}