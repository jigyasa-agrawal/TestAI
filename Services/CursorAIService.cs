using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using TestAI.Models;

namespace TestAI.Services
{
    public interface ICursorAIService
    {
        Task<CursorAIResponse> GetResponseAsync(string prompt, string model = "gpt-4", int maxTokens = 1000, double temperature = 0.7);
        Task<CursorAIResponse> AnalyzeLogsAsync(string logs, string model = "gpt-4", int maxTokens = 2000, double temperature = 0.3, string? customPromptTemplate = null);
        Task<LogAnalysisResponse> AnalyzeLogsStructuredAsync(string logs, string model = "gpt-4o", int maxTokens = 2000, double temperature = 0.3);
    }

    public class CursorAIService : ICursorAIService
    {
        private readonly HttpClient _httpClient;
        private readonly CursorAIConfig _config;
        private readonly ILogger<CursorAIService> _logger;

        private const string DefaultLogAnalysisTemplate = @"Analyze these logs and identify the root cause, the likely code location, and the reason for the issue. Suggest a fix with a code snippet and indicate where to apply it.

Logs:
{0}";

        private const string StructuredLogAnalysisTemplate = @"Analyze the following logs and provide a structured response with exactly three sections:

1. **LIKELY CAUSE**: Explain what likely caused the issue based on the log analysis
2. **POSSIBLE CODE FIX**: Describe the recommended solution or fix for the issue
3. **OPTIONAL CODE SNIPPET**: Provide a code snippet that demonstrates the fix (if applicable)

Please format your response exactly as follows:
LIKELY CAUSE:
[Your analysis of what caused the issue]

POSSIBLE CODE FIX:
[Your recommended solution]

OPTIONAL CODE SNIPPET:
[Code snippet if applicable, otherwise write 'N/A']

Logs to analyze:
{0}";

        public CursorAIService(HttpClient httpClient, IOptions<CursorAIConfig> config, ILogger<CursorAIService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
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

        public async Task<LogAnalysisResponse> AnalyzeLogsStructuredAsync(string logs, string model = "gpt-4o", int maxTokens = 2000, double temperature = 0.3)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logs))
                {
                    return new LogAnalysisResponse
                    {
                        Success = false,
                        Error = "Logs cannot be empty"
                    };
                }

                // Format the prompt with the logs using the structured template
                var formattedPrompt = string.Format(StructuredLogAnalysisTemplate, logs);

                _logger.LogInformation("Analyzing logs with structured format, length: {LogLength} characters", logs.Length);

                // Get the raw response from GPT
                var rawResponse = await GetResponseAsync(formattedPrompt, model, maxTokens, temperature);

                if (!rawResponse.Success)
                {
                    return new LogAnalysisResponse
                    {
                        Success = false,
                        Error = rawResponse.Error,
                        RawResponse = rawResponse.Response
                    };
                }

                // Parse the structured response
                var structuredResponse = ParseStructuredResponse(rawResponse.Response);
                structuredResponse.Success = true;
                structuredResponse.RawResponse = rawResponse.Response;

                return structuredResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing logs with structured format");
                return new LogAnalysisResponse
                {
                    Success = false,
                    Error = $"Exception occurred during structured log analysis: {ex.Message}"
                };
            }
        }

        private LogAnalysisResponse ParseStructuredResponse(string response)
        {
            var result = new LogAnalysisResponse();

            try
            {
                // Split the response into sections
                var sections = response.Split(new string[] { "LIKELY CAUSE:", "POSSIBLE CODE FIX:", "OPTIONAL CODE SNIPPET:" }, 
                    StringSplitOptions.RemoveEmptyEntries);

                if (sections.Length >= 3)
                {
                    // Extract each section and clean up
                    result.LikelyCause = ExtractSection(response, "LIKELY CAUSE:", "POSSIBLE CODE FIX:");
                    result.PossibleCodeFix = ExtractSection(response, "POSSIBLE CODE FIX:", "OPTIONAL CODE SNIPPET:");
                    result.OptionalCodeSnippet = ExtractSection(response, "OPTIONAL CODE SNIPPET:", null);
                }
                else
                {
                    // Fallback: try to extract what we can or use the full response
                    result.LikelyCause = "Could not parse structured response";
                    result.PossibleCodeFix = "Please refer to the raw response";
                    result.OptionalCodeSnippet = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse structured response, using fallback");
                result.LikelyCause = "Could not parse structured response";
                result.PossibleCodeFix = "Please refer to the raw response";
                result.OptionalCodeSnippet = null;
            }

            return result;
        }

        private string ExtractSection(string response, string startMarker, string? endMarker)
        {
            try
            {
                var startIndex = response.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1) return "";

                startIndex += startMarker.Length;

                int endIndex;
                if (endMarker != null)
                {
                    endIndex = response.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (endIndex == -1) endIndex = response.Length;
                }
                else
                {
                    endIndex = response.Length;
                }

                var section = response.Substring(startIndex, endIndex - startIndex).Trim();
                
                // Clean up the section - remove extra newlines and format nicely
                section = System.Text.RegularExpressions.Regex.Replace(section, @"\n\s*\n", "\n");
                
                return section;
            }
            catch
            {
                return "";
            }
        }
    }
}