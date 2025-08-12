namespace LogFix.Models
{
    public class LogAnalysisResponse
    {
        public string LikelyCause { get; set; } = string.Empty;
        public string PossibleCodeFix { get; set; } = string.Empty;
        public string? OptionalCodeSnippet { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? RawResponse { get; set; } // Keep the raw GPT response as fallback
    }
}