using Microsoft.AspNetCore.Mvc;
using LogFix.Models;
using LogFix.Services;

namespace LogFix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CursorAIController : ControllerBase
    {
        private readonly ICursorAIService _cursorAIService;
        private readonly ILogger<CursorAIController> _logger;

        public CursorAIController(ICursorAIService cursorAIService, ILogger<CursorAIController> logger)
        {
            _cursorAIService = cursorAIService;
            _logger = logger;
        }

        /// <summary>
        /// Analyze logs with the predefined template for root cause analysis
        /// </summary>
        /// <param name="logs">The log content to analyze</param>
        /// <returns>AI analysis with root cause, code location, and suggested fix</returns>
        [HttpPost("analyze-logs")]
        public async Task<ActionResult<LogAnalysisResponse>> AnalyzeLogs([FromBody] string logs)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logs))
                {
                    return BadRequest(new LogAnalysisResponse
                    {
                        Success = false,
                        Error = "Logs cannot be empty"
                    });
                }

                _logger.LogInformation("Received log analysis request with {Length} characters", logs.Length);

                var response = await _cursorAIService.AnalyzeLogsStructuredAsync(logs);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing log analysis request");
                return StatusCode(500, new LogAnalysisResponse
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Analyze logs with the original simple text response format
        /// </summary>
        /// <param name="logs">The log content to analyze</param>
        /// <returns>AI analysis as simple text response</returns>
        [HttpPost("analyze-logs-simple")]
        public async Task<ActionResult<CursorAIResponse>> AnalyzeLogsSimple([FromBody] string logs)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logs))
                {
                    return BadRequest(new CursorAIResponse
                    {
                        Success = false,
                        Error = "Logs cannot be empty"
                    });
                }

                _logger.LogInformation("Received simple log analysis request with {Length} characters", logs.Length);

                var response = await _cursorAIService.AnalyzeLogsAsync(logs);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simple log analysis request");
                return StatusCode(500, new CursorAIResponse
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Analyze logs with custom parameters and optional custom prompt template
        /// </summary>
        /// <param name="request">The log analysis request with custom parameters</param>
        /// <returns>AI analysis based on the provided parameters</returns>
        [HttpPost("analyze-logs-advanced")]
        public async Task<ActionResult<object>> AnalyzeLogsAdvanced([FromBody] LogAnalysisRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Logs))
                {
                    if (request.UseStructuredResponse)
                    {
                        return BadRequest(new LogAnalysisResponse
                        {
                            Success = false,
                            Error = "Logs cannot be empty"
                        });
                    }
                    else
                    {
                        return BadRequest(new CursorAIResponse
                        {
                            Success = false,
                            Error = "Logs cannot be empty"
                        });
                    }
                }

                _logger.LogInformation("Received advanced log analysis request with model: {Model}, maxTokens: {MaxTokens}, temperature: {Temperature}, structured: {Structured}",
                    request.Model, request.MaxTokens, request.Temperature, request.UseStructuredResponse);

                if (request.UseStructuredResponse && string.IsNullOrWhiteSpace(request.CustomPromptTemplate))
                {
                    // Use structured analysis
                    var structuredResponse = await _cursorAIService.AnalyzeLogsStructuredAsync(
                        request.Logs,
                        request.Model,
                        request.MaxTokens,
                        request.Temperature);

                    if (structuredResponse.Success)
                    {
                        return Ok(structuredResponse);
                    }
                    else
                    {
                        return BadRequest(structuredResponse);
                    }
                }
                else
                {
                    // Use simple analysis (original behavior)
                    var response = await _cursorAIService.AnalyzeLogsAsync(
                        request.Logs,
                        request.Model,
                        request.MaxTokens,
                        request.Temperature,
                        request.CustomPromptTemplate);

                    if (response.Success)
                    {
                        return Ok(response);
                    }
                    else
                    {
                        return BadRequest(response);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing advanced log analysis request");
                
                if (request.UseStructuredResponse)
                {
                    return StatusCode(500, new LogAnalysisResponse
                    {
                        Success = false,
                        Error = "Internal server error occurred"
                    });
                }
                else
                {
                    return StatusCode(500, new CursorAIResponse
                    {
                        Success = false,
                        Error = "Internal server error occurred"
                    });
                }
            }
        }

        /// <summary>
        /// Send a prompt to Cursor AI and get a response
        /// </summary>
        /// <param name="prompt">The prompt to send to Cursor AI</param>
        /// <returns>AI response</returns>
        [HttpPost("chat")]
        public async Task<ActionResult<CursorAIResponse>> Chat([FromBody] string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return BadRequest(new CursorAIResponse
                    {
                        Success = false,
                        Error = "Prompt cannot be empty"
                    });
                }

                _logger.LogInformation("Received chat request with prompt length: {Length}", prompt.Length);

                var response = await _cursorAIService.GetResponseAsync(prompt);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, new CursorAIResponse
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Send a prompt to Cursor AI with custom parameters
        /// </summary>
        /// <param name="request">The chat request with custom parameters</param>
        /// <returns>AI response</returns>
        [HttpPost("chat-advanced")]
        public async Task<ActionResult<CursorAIResponse>> ChatAdvanced([FromBody] CursorAIRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return BadRequest(new CursorAIResponse
                    {
                        Success = false,
                        Error = "Prompt cannot be empty"
                    });
                }

                _logger.LogInformation("Received advanced chat request with model: {Model}, maxTokens: {MaxTokens}, temperature: {Temperature}", 
                    request.Model, request.MaxTokens, request.Temperature);

                var response = await _cursorAIService.GetResponseAsync(
                    request.Prompt, 
                    request.Model, 
                    request.MaxTokens, 
                    request.Temperature);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing advanced chat request");
                return StatusCode(500, new CursorAIResponse
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Simple GET endpoint for quick testing
        /// </summary>
        /// <param name="prompt">The prompt as a query parameter</param>
        /// <returns>AI response</returns>
        [HttpGet("quick-chat")]
        public async Task<ActionResult<CursorAIResponse>> QuickChat([FromQuery] string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return BadRequest(new CursorAIResponse
                    {
                        Success = false,
                        Error = "Prompt cannot be empty"
                    });
                }

                _logger.LogInformation("Received quick chat request via GET");

                var response = await _cursorAIService.GetResponseAsync(prompt);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing quick chat request");
                return StatusCode(500, new CursorAIResponse
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }
    }
}