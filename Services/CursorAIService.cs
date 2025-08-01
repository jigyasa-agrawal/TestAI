using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using TestAI.Models;

namespace TestAI.Services
{
    public interface ICursorAIService
    {
        Task<CursorAIResponse> GetResponseAsync(string prompt, string model = "gpt-4", int maxTokens = 1000, double temperature = 0.7);
        Task<CursorAIResponse> AnalyzeLogsAsync(string logs, string model = "gpt-4", int maxTokens = 2000, double temperature = 0.3, string? customPromptTemplate = null);
        Task<StructuredLogAnalysisResponse> AnalyzeLogsStructuredAsync(string logs, string model = "gpt-4", int maxTokens = 2000, double temperature = 0.3, string? customPromptTemplate = null);
    }

    public class CursorAIService : ICursorAIService
    {
        private readonly HttpClient _httpClient;
        private readonly CursorAIConfig _config;
        private readonly ILogger<CursorAIService> _logger;

        private const string DefaultLogAnalysisTemplate = @"Analyze these logs and identify the root cause, the likely code location, and the reason for the issue. Suggest a fix with a code snippet and indicate where to apply it.

Logs:
{0}";

        private const string StructuredLogAnalysisTemplate = @"Analyze these logs and provide a structured response. Please format your response EXACTLY like this:

**LIKELY CAUSE:**
[Describe the root cause of the issue in 1-2 sentences]

**POSSIBLE CODE FIX:**
[Describe the steps to fix the issue in detail]

**OPTIONAL CODE SNIPPET:**
```
[Provide actual code that fixes the issue]
```

**ADDITIONAL NOTES:**
[Any additional context, prevention tips, or related issues]

Logs to analyze:
{0}";

        public CursorAIService(HttpClient httpClient, IOptions<CursorAIConfig> config, ILogger<CursorAIService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<StructuredLogAnalysisResponse> AnalyzeLogsStructuredAsync(string logs, string model = "gpt-4", int maxTokens = 2000, double temperature = 0.3, string? customPromptTemplate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logs))
                {
                    return new StructuredLogAnalysisResponse
                    {
                        Success = false,
                        Error = "Logs cannot be empty"
                    };
                }

                // Use custom template if provided, otherwise use structured template
                var promptTemplate = !string.IsNullOrWhiteSpace(customPromptTemplate) 
                    ? customPromptTemplate 
                    : StructuredLogAnalysisTemplate;

                // Format the prompt with the logs
                var formattedPrompt = string.Format(promptTemplate, logs);

                _logger.LogInformation("Analyzing logs with structured response, length: {LogLength} characters", logs.Length);

                // Get response from Cursor AI
                var response = await GetResponseAsync(formattedPrompt, model, maxTokens, temperature);

                if (!response.Success)
                {
                    return new StructuredLogAnalysisResponse
                    {
                        Success = false,
                        Error = response.Error
                    };
                }

                // Parse the structured response
                var structuredResponse = ParseStructuredResponse(response.Response);
                structuredResponse.Success = true;

                return structuredResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing logs with structured response");
                return new StructuredLogAnalysisResponse
                {
                    Success = false,
                    Error = $"Exception occurred during structured log analysis: {ex.Message}"
                };
            }
        }

        private StructuredLogAnalysisResponse ParseStructuredResponse(string aiResponse)
        {
            var result = new StructuredLogAnalysisResponse();

            try
            {
                // Extract Likely Cause
                var likelyCauseMatch = Regex.Match(aiResponse, @"\*\*LIKELY CAUSE:\*\*\s*(.*?)(?=\*\*|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (likelyCauseMatch.Success)
                {
                    result.LikelyCause = likelyCauseMatch.Groups[1].Value.Trim();
                }

                // Extract Possible Code Fix
                var codeFixMatch = Regex.Match(aiResponse, @"\*\*POSSIBLE CODE FIX:\*\*\s*(.*?)(?=\*\*|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (codeFixMatch.Success)
                {
                    result.PossibleCodeFix = codeFixMatch.Groups[1].Value.Trim();
                }

                // Extract Optional Code Snippet
                var codeSnippetMatch = Regex.Match(aiResponse, @"\*\*OPTIONAL CODE SNIPPET:\*\*\s*```[\w]*\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (codeSnippetMatch.Success)
                {
                    result.OptionalCodeSnippet = codeSnippetMatch.Groups[1].Value.Trim();
                }

                // Extract Additional Notes
                var additionalNotesMatch = Regex.Match(aiResponse, @"\*\*ADDITIONAL NOTES:\*\*\s*(.*?)$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (additionalNotesMatch.Success)
                {
                    result.AdditionalNotes = additionalNotesMatch.Groups[1].Value.Trim();
                }

                // If no structured sections found, put everything in LikelyCause
                if (string.IsNullOrWhiteSpace(result.LikelyCause) && string.IsNullOrWhiteSpace(result.PossibleCodeFix))
                {
                    result.LikelyCause = aiResponse.Trim();
                    result.PossibleCodeFix = "Unable to parse structured response. Please check the raw response.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing structured response, returning raw response");
                result.LikelyCause = aiResponse.Trim();
                result.PossibleCodeFix = "Error occurred while parsing structured response.";
            }

            return result;
        }

        public async Task<CursorAIResponse> AnalyzeLogsAsync(string logs, string model = "gpt-4", int maxTokens = 2000, double temperature = 0.3, string? customPromptTemplate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logs))
                {
                    return new CursorAIResponse
                    {
                        Success = false,
                        Error = "Logs cannot be empty"
                    };
                }

                // Use custom template if provided, otherwise use default
                var promptTemplate = !string.IsNullOrWhiteSpace(customPromptTemplate) 
                    ? customPromptTemplate 
                    : DefaultLogAnalysisTemplate;

                // Format the prompt with the logs
                var formattedPrompt = string.Format(promptTemplate, logs);

                _logger.LogInformation("Analyzing logs with length: {LogLength} characters", logs.Length);

                // Use the existing GetResponseAsync method with the formatted prompt
                return await GetResponseAsync(formattedPrompt, model, maxTokens, temperature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing logs");
                return new CursorAIResponse
                {
                    Success = false,
                    Error = $"Exception occurred during log analysis: {ex.Message}"
                };
            }
        }

        public async Task<CursorAIResponse> GetResponseAsync(string prompt, string model = "gpt-4", int maxTokens = 1000, double temperature = 0.7)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    return new CursorAIResponse
                    {
                        Success = false,
                        Error = "API Key is not configured"
                    };
                }

                var request = new CursorAIApiRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    MaxTokens = maxTokens,
                    Temperature = temperature
                };

                var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                var url = $"{_config.BaseUrl}/chat/completions";
                _logger.LogInformation("Making request to Cursor AI: {Url}", url);

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<CursorAIApiResponse>(responseContent);
                    
                    if (apiResponse?.Choices?.Count > 0)
                    {
                        return new CursorAIResponse
                        {
                            Success = true,
                            Response = apiResponse.Choices[0].Message.Content
                        };
                    }
                    else
                    {
                        return new CursorAIResponse
                        {
                            Success = false,
                            Error = "No response received from API"
                        };
                    }
                }
                else
                {
                    _logger.LogError("API call failed with status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    
                    return new CursorAIResponse
                    {
                        Success = false,
                        Error = $"API call failed with status: {response.StatusCode}. Response: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Cursor AI API");
                return new CursorAIResponse
                {
                    Success = false,
                    Error = $"Exception occurred: {ex.Message}"
                };
            }
        }
    }
}