namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        [HttpGet("ready")]
        public IActionResult Ready()
        {
            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow
            });
        }
    }
}