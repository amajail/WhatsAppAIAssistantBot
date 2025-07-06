namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WhatsAppAIAssistantBot.Models;
    using WhatsAppAIAssistantBot.Application;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// WhatsApp webhook controller that receives and processes incoming messages from Twilio.
    /// This controller serves as the main entry point for WhatsApp messages and coordinates
    /// the processing through the orchestration service.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IOrchestrationService _orchestrator;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the WhatsAppController.
        /// </summary>
        /// <param name="orchestulator">The orchestration service for handling message processing workflow</param>
        /// <param name="logger">The logger instance for this controller</param>
        /// <param name="configuration">The application configuration</param>
        public WhatsAppController(IOrchestrationService orchestulator, ILogger<WhatsAppController> logger, IConfiguration configuration)
        {
            _orchestrator = orchestulator;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Receives incoming WhatsApp messages from Twilio webhook and processes them through the orchestration service.
        /// This endpoint validates the incoming message data and delegates processing to the orchestration layer.
        /// </summary>
        /// <param name="input">The Twilio webhook payload containing message data from WhatsApp</param>
        /// <returns>
        /// Returns OK (200) if message is processed successfully,
        /// BadRequest (400) for invalid input data,
        /// or InternalServerError (500) if processing fails
        /// </returns>
        /// <response code="200">Message processed successfully</response>
        /// <response code="400">Invalid request data (missing required fields)</response>
        /// <response code="500">Internal server error during message processing</response>
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