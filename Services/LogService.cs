using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;




namespace Vehicle_Information_System.Services
{
    public class LogService
    {
        private readonly IWebHostEnvironment _environment;

        public LogService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

       

        private async Task<IEnumerable<LogEntry>> ParseLogFileAsync(string filePath)
        {
            var entries = new List<LogEntry>();

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return entries;
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                Console.WriteLine($"Reading {lines.Length} lines from {Path.GetFileName(filePath)}");

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var entry = ParseLogLine(line);

                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing log file {filePath}: {ex.Message}");
            }

            return entries;
        }

        private LogEntry? ParseLogLine(string line)
        {
            try
            {
                // Try to parse JSON format first (structured logs)
                if (line.TrimStart().StartsWith("{") && line.TrimStart().EndsWith("}"))
                {
                    try
                    {
                        var jsonEntry = System.Text.Json.JsonSerializer.Deserialize<JsonLogEntry>(line);
                        if (jsonEntry != null && !string.IsNullOrEmpty(jsonEntry.Message))
                        {
                            return new LogEntry
                            {
                                Timestamp = jsonEntry.Timestamp,
                                Level = jsonEntry.Level,
                                Message = jsonEntry.Message,
                                Properties = jsonEntry.Properties != null
                                    ? string.Join(", ", jsonEntry.Properties.Select(p => $"{p.Key}={p.Value}"))
                                    : null
                            };
                        }
                    }
                    catch
                    {
                        // Not a valid JSON log line, continue with text parsing
                    }
                }

                // Parse text format: "2026-01-08 08:37:32.225 +00:00 [INF] Message here"
                var pattern = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[([A-Z]{3})\]\s+(.*?)(?:\s+\{.*\})?$";
                var match = Regex.Match(line, pattern);

                if (match.Success)
                {
                    var timestamp = DateTime.Parse(match.Groups[1].Value);
                    var level = match.Groups[2].Value switch
                    {
                        "INF" => "Information",
                        "DBG" => "Debug",
                        "WRN" => "Warning",
                        "ERR" => "Error",
                        "FTL" => "Fatal",
                        _ => match.Groups[2].Value
                    };
                    var message = match.Groups[3].Value.Trim();

                    return new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = message
                    };
                }

                // Alternative format: 2026-01-08 08:37:32.225 [INF] Message
                var altPattern = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\s+\[([A-Z]{3})\]\s+(.*)";
                var altMatch = Regex.Match(line, altPattern);

                if (altMatch.Success)
                {
                    return new LogEntry
                    {
                        Timestamp = DateTime.Parse(altMatch.Groups[1].Value),
                        Level = altMatch.Groups[2].Value switch
                        {
                            "INF" => "Information",
                            "DBG" => "Debug",
                            "WRN" => "Warning",
                            "ERR" => "Error",
                            "FTL" => "Fatal",
                            _ => altMatch.Groups[2].Value
                        },
                        Message = altMatch.Groups[3].Value.Trim()
                    };
                }

                // Simple fallback: Try to find any timestamp
                var simplePattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})";
                var simpleMatch = Regex.Match(line, simplePattern);

                if (simpleMatch.Success)
                {
                    var timestamp = DateTime.Parse(simpleMatch.Groups[1].Value);
                    var message = line.Replace(simpleMatch.Groups[1].Value, "").Trim();

                    // Try to find level
                    var levelMatch = Regex.Match(message, @"\[([A-Z]{3})\]");
                    string level = "Information";
                    string cleanMessage = message;

                    if (levelMatch.Success)
                    {
                        level = levelMatch.Groups[1].Value switch
                        {
                            "INF" => "Information",
                            "DBG" => "Debug",
                            "WRN" => "Warning",
                            "ERR" => "Error",
                            "FTL" => "Fatal",
                            _ => levelMatch.Groups[1].Value
                        };
                        cleanMessage = message.Replace(levelMatch.Value, "").Trim();
                    }

                    return new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = cleanMessage
                    };
                }

                // If nothing works, create a generic entry with current timestamp
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = "Information",
                        Message = line.Length > 200 ? line.Substring(0, 200) + "..." : line
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing line: {ex.Message}");
                return null;
            }
        }

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? Properties { get; set; }
        }

        public class JsonLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public Dictionary<string, object>? Properties { get; set; }
        }

    }
}
    