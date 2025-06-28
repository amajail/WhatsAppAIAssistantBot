namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WhatsAppAIAssistantBot.Models;
    using WhatsAppAIAssistantBot.Application;

    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IOrchestrationService _orchestrator;

        public WhatsAppController(IOrchestrationService orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromForm] TwilioWebhookModel input)
        {
            try
            {
                await _orchestrator.HandleMessageAsync(input.From, input.Body);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}