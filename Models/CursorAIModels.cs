namespace TestAI.Models
{
    public class CursorAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class CursorAIRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
    }

    public class LogAnalysisRequest
    {
        public string Logs { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.3;
        public string? CustomPromptTemplate { get; set; }
    }

    public class CursorAIResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class CursorAIApiRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new();
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class CursorAIApiResponse
    {
        public List<Choice> Choices { get; set; } = new();
        public Usage Usage { get; set; } = new();
    }

    public class Choice
    {
        public Message Message { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}