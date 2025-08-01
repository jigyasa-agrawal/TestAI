using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TestAI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly HttpClient _httpClient;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet("CallDummyApiGet")]
        public async Task<IActionResult> CallDummyApiGet([FromQuery] string param)
        {
            var url = $"https://dummyapi.io/api/get?param={param}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }

        [HttpPost("CallDummyApiPost")]
        public async Task<IActionResult> CallDummyApiPost([FromBody] string param)
        {
            var url = "https://dummyapi.io/api/post";
            var content = new StringContent($"{{\"param\":\"{param}\"}}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }
}
