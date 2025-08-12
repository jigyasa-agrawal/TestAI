using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LogFix.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorGenerateController : ControllerBase
    {

        [HttpGet]
        public string Get()
        {
            var logger = new ExceptionLogger();
            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    switch (i)
                    {
                        case 1: throw new ArgumentNullException("param", "Parameter cannot be null");
                        case 2: throw new ArgumentException("Invalid argument");
                        case 3: throw new InvalidOperationException("Invalid operation");
                        case 4: throw new NotImplementedException("Feature not implemented");
                        case 5: throw new FormatException("Invalid format");
                        case 6: throw new IndexOutOfRangeException("Index out of range");
                        case 7: throw new DivideByZeroException("Division by zero");
                        case 8: throw new TimeoutException("Operation timed out");
                        case 9: throw new FileNotFoundException("File not found");
                        case 10: throw new UnauthorizedAccessException("Access denied");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogException(
                        ex,
                        $"Exception {i}: {ex.GetType().Name} occurred.",
                        i
                    );
                }
            }
            return "Successfully generated exceptions.";
        }
    }
}


public class ExceptionLogger
{
    private readonly string _logFilePath;

    public ExceptionLogger(string logFilePath = "exception_log.json")
    {
        _logFilePath = logFilePath;
    }

    public void LogException(Exception ex, string message = "An exception occurred.", int eventId = 1)
    {
        var logEntry = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            level = "Information",
            source = "Microsoft.AspNetCore.Hosting.Diagnostics",
            message = message,
            eventId = eventId,
            StackTrace = ex.StackTrace
        };

        List<object> logEntries;
        if (File.Exists(_logFilePath))
        {
            var fileContent = File.ReadAllText(_logFilePath);
            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                try
                {
                    logEntries = JsonSerializer.Deserialize<List<object>>(fileContent) ?? new List<object>();
                }
                catch
                {
                    // If file is not a valid array, start fresh
                    logEntries = new List<object>();
                }
            }
            else
            {
                logEntries = new List<object>();
            }
        }
        else
        {
            logEntries = new List<object>();
        }

        logEntries.Add(logEntry);
        var json = JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_logFilePath, json);
    }
}