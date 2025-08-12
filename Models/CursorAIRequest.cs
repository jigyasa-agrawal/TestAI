namespace LogFix.Models
{
    public class CursorAIRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
    }
}