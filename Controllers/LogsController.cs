using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Restrict access to authenticated users
    public class LogsController : ControllerBase
    {
        private readonly LogService _logService;
        private readonly ILogger<LogsController> _logger;
        private readonly IWebHostEnvironment _environment;

      

        public LogsController(LogService logService, ILogger<LogsController> logger, IWebHostEnvironment environment)
        {
            _logService = logService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get logs with filtering and pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                var logDir = Path.Combine(_environment.ContentRootPath, "Logs");

                if (!Directory.Exists(logDir))
                {
                    _logger.LogWarning($"Logs directory not found: {logDir}");
                    return BadRequest(new { message = "Log directory not found" });
                }

                // Get all log files (both text and JSON)
                var logFiles = Directory.GetFiles(logDir, "log-*.txt")
                    .Concat(Directory.GetFiles(logDir, "log-structured-*.json"))
                    .OrderByDescending(f => f)
                    .ToList();

                if (!logFiles.Any())
                    return BadRequest(new { message = "No log files found" });

                var allLogs = new List<LogEntry>();
                var errors = new List<string>();

                foreach (var logFile in logFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(logFile);
                        _logger.LogInformation($"Parsing log file: {fileName}");

                        // Read file with retry logic and proper file sharing
                        var lines = await ReadLogFileWithRetryAsync(logFile);

                        if (lines == null || !lines.Any())
                        {
                            errors.Add($"File {fileName} is empty or could not be read");
                            continue;
                        }

                        // Parse based on file extension
                        if (fileName.EndsWith(".json"))
                        {
                            var jsonLogs = ParseJsonLogs(lines, fileName);
                            allLogs.AddRange(jsonLogs);
                        }
                        else
                        {
                            var textLogs = ParseTextLogs(lines, fileName);
                            allLogs.AddRange(textLogs);
                        }

                        _logger.LogInformation($"Added {allLogs.Count} logs from {fileName}");
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error parsing {Path.GetFileName(logFile)}: {ex.Message}";
                        _logger.LogWarning(ex, errorMsg);
                        errors.Add(errorMsg);
                    }
                }

                // Sort by timestamp descending (newest first)
                var sortedLogs = allLogs
                    .OrderByDescending(l => l.Timestamp)
                    .ToList();

                // Add warnings about skipped files if any
                if (errors.Any())
                {
                    _logger.LogWarning($"Encountered {errors.Count} errors while parsing log files");
                }

                return Ok(new
                {
                    success = true,
                    total = sortedLogs.Count,
                    errors = errors.Any() ? errors : null,
                    logs = sortedLogs.Take(1000).ToList() // Limit to prevent memory issues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { success = false, message = "Error retrieving logs" });
            }
        }

        // Helper method to read file with retry logic
        private async Task<List<string>> ReadLogFileWithRetryAsync(string filePath, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var lines = new List<string>();

                    // Use FileShare.ReadWrite to allow other processes to read/write simultaneously
                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        useAsync: true
                    );

                    using var reader = new StreamReader(fileStream);
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                        }
                    }

                    return lines;
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // Wait before retrying with exponential backoff
                    await Task.Delay(100 * (attempt + 1));
                    _logger.LogDebug($"Retry {attempt + 1} for file: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    _logger.LogDebug($"Retry {attempt + 1} due to error: {ex.Message}");
                    await Task.Delay(100 * (attempt + 1));
                }
            }

            // If all retries failed, try one last time with different approach
            try
            {
                // Last attempt: read as text without async
                var content = System.IO.File.ReadAllText(filePath);
                return content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch
            {
                _logger.LogError($"Failed to read file after {maxRetries} attempts: {filePath}");
                return new List<string>();
            }
        }

        // Parse JSON log entries
        private List<LogEntry> ParseJsonLogs(List<string> lines, string fileName)
        {
            var logs = new List<LogEntry>();

            foreach (var line in lines)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var jsonDoc = JsonDocument.Parse(line);
                    var root = jsonDoc.RootElement;

                    // Extract timestamp from various possible formats
                    var timestamp = DateTime.MinValue;
                    if (root.TryGetProperty("@t", out var tElement))
                    {
                        DateTime.TryParse(tElement.GetString(), out timestamp);
                    }
                    else if (root.TryGetProperty("Timestamp", out var tsElement))
                    {
                        DateTime.TryParse(tsElement.GetString(), out timestamp);
                    }
                    else if (root.TryGetProperty("timestamp", out var tsElement2))
                    {
                        DateTime.TryParse(tsElement2.GetString(), out timestamp);
                    }

                    // Extract level
                    var level = "INFO";
                    if (root.TryGetProperty("@l", out var lElement))
                    {
                        level = lElement.GetString() ?? "INFO";
                    }
                    else if (root.TryGetProperty("Level", out var levelElement))
                    {
                        level = levelElement.GetString() ?? "INFO";
                    }
                    else if (root.TryGetProperty("level", out var levelElement2))
                    {
                        level = levelElement2.GetString() ?? "INFO";
                    }

                    // Extract message
                    var message = "";
                    if (root.TryGetProperty("@m", out var mElement))
                    {
                        message = mElement.GetString() ?? "";
                    }
                    else if (root.TryGetProperty("Message", out var msgElement))
                    {
                        message = msgElement.GetString() ?? "";
                    }
                    else if (root.TryGetProperty("message", out var msgElement2))
                    {
                        message = msgElement2.GetString() ?? "";
                    }

                    // Get the full log entry as JSON string
                    var fullJson = root.GetRawText();

                    logs.Add(new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = message,
                        Raw = fullJson,
                        FileName = fileName,
                        SourceContext = root.TryGetProperty("SourceContext", out var sc) ? sc.GetString() : null,
                        Application = root.TryGetProperty("Application", out var app) ? app.GetString() : null,
                        Environment = root.TryGetProperty("Environment", out var env) ? env.GetString() : null
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug($"Failed to parse JSON log line: {ex.Message}");
                    // If JSON parsing fails, treat it as a text line
                    logs.Add(new LogEntry
                    {
                        Timestamp = DateTime.MinValue,
                        Level = "UNKNOWN",
                        Message = line,
                        Raw = line,
                        FileName = fileName
                    });
                }
            }

            return logs;
        }

        // Parse Text log entries (like your log-20260616.txt)
        private List<LogEntry> ParseTextLogs(List<string> lines, string fileName)
        {
            var logs = new List<LogEntry>();

            foreach (var line in lines)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse timestamp from format: "2026-06-16 15:40:37.038 +01:00 [INF]"
                    var timestamp = DateTime.MinValue;
                    var level = "INFO";
                    var message = line;
                    var sourceContext = "";
                    var application = "";
                    var environment = "";

                    // Try to parse using regex or manual parsing
                    // Pattern: YYYY-MM-DD HH:MM:SS.fff +TZ [LEVEL] Message
                    if (line.Length > 30 && line[0] >= '0' && line[0] <= '9')
                    {
                        try
                        {
                            // Extract timestamp (first 23 chars: "2026-06-16 15:40:37.038")
                            var datePart = line.Substring(0, 23);
                            if (DateTime.TryParseExact(datePart, "yyyy-MM-dd HH:mm:ss.fff",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                            {
                                timestamp = parsedDate;

                                // Extract level - look for [INF], [WRN], [DBG], [ERR]
                                var levelStart = line.IndexOf('[');
                                var levelEnd = line.IndexOf(']');
                                if (levelStart >= 0 && levelEnd > levelStart)
                                {
                                    level = line.Substring(levelStart + 1, levelEnd - levelStart - 1);

                                    // Extract message after the level
                                    var afterLevel = line.Substring(levelEnd + 1).TrimStart();
                                    if (afterLevel.StartsWith(" "))
                                        afterLevel = afterLevel.Substring(1);

                                    message = afterLevel;
                                }
                                else
                                {
                                    // If no level bracket found, just use the line
                                    message = line;
                                }
                            }
                        }
                        catch
                        {
                            // If parsing fails, use the whole line
                            message = line;
                        }
                    }

                    // Try to extract structured data from the message
                    var entry = new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = message,
                        Raw = line,
                        FileName = fileName
                    };

                    // Try to extract SourceContext, Application, Environment from the message
                    var contextMatch = System.Text.RegularExpressions.Regex.Match(message,
                        @"""SourceContext"":""([^""]+)""");
                    if (contextMatch.Success)
                        entry.SourceContext = contextMatch.Groups[1].Value;

                    var appMatch = System.Text.RegularExpressions.Regex.Match(message,
                        @"""Application"":""([^""]+)""");
                    if (appMatch.Success)
                        entry.Application = appMatch.Groups[1].Value;

                    var envMatch = System.Text.RegularExpressions.Regex.Match(message,
                        @"""Environment"":""([^""]+)""");
                    if (envMatch.Success)
                        entry.Environment = envMatch.Groups[1].Value;

                    logs.Add(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Failed to parse text log line: {ex.Message}");
                    // Add the raw line anyway
                    logs.Add(new LogEntry
                    {
                        Timestamp = DateTime.MinValue,
                        Level = "UNKNOWN",
                        Message = line,
                        Raw = line,
                        FileName = fileName
                    });
                }
            }

            return logs;
        }

        // Log Entry Model
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; } = "INFO";
            public string Message { get; set; } = "";
            public string Raw { get; set; } = "";
            public string FileName { get; set; } = "";
            public string? SourceContext { get; set; }
            public string? Application { get; set; }
            public string? Environment { get; set; }
        }



    }
}