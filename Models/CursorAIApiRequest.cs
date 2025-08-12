namespace LogFix.Models
{
    public class CursorAIApiRequest
    {
        public string model { get; set; } = string.Empty;
        public List<Message> messages { get; set; } = new();
        public int max_tokens { get; set; }
    }
}