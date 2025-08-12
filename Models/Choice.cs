namespace LogFix.Models
{
    public class Choice
    {
        public Message Message { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
    }
}