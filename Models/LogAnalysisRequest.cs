namespace LogFix.Models
{
    public class LogAnalysisRequest
    {
        public string Logs { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.3;
        public string? CustomPromptTemplate { get; set; }
        public bool UseStructuredResponse { get; set; } = true; // Default to structured response
    }
}