using Microsoft.AspNetCore.Mvc;
using TestAI.Models;
using TestAI.Services;

namespace TestAI.Controllers
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