namespace LogFix.Models
{
    public class CursorAIApiResponse
    {
        public List<Choice> Choices { get; set; } = new();
        public Usage Usage { get; set; } = new();
    }
}