namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WhatsAppAIAssistantBot.Models;
    using WhatsAppAIAssistantBot.Application;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Cryptography;
    using System.Text;

    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IOrchestrationService _orchestrator;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly IConfiguration _configuration;

        public WhatsAppController(IOrchestrationService orchestulator, ILogger<WhatsAppController> logger, IConfiguration configuration)
        {
            _orchestrator = orchestulator;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromForm] TwilioWebhookModel input)
        {
            if (input == null)
            {
                _logger.LogWarning("Received null webhook input");
                return BadRequest(new { error = "Invalid request data" });
            }

            if (string.IsNullOrWhiteSpace(input.From))
            {
                _logger.LogWarning("Received webhook with missing From field");
                return BadRequest(new { error = "Invalid request data" });
            }

            if (string.IsNullOrEmpty(input.Body))
            {
                _logger.LogInformation("Received empty message from {From}", input.From);
                return Ok();
            }

            try
            {
                await _orchestrator.HandleMessageAsync(input.From, input.Body);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {From}", input.From);
                return StatusCode(500, new { error = "Internal server error occurred while processing your message" });
            }
        }
    }
}