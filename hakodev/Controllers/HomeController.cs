using Microsoft.AspNetCore.Mvc;

namespace hakodev.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hello, World! This is a .NET 8.0 application running on Windows.");
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok("Service is healthy!");
        }

        [HttpGet("version")]
        public IActionResult Version()
        {
            return Ok(new { Version = "1.0.8", Framework = "net8.0" });
        }
    }
}
