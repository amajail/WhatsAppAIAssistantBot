namespace WhatsAppAIAssistantBot.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WhatsAppAIAssistantBot.Models;
    using WhatsAppAIAssistantBot.Services;

    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IOrchestrationService _orchestrator;
        private readonly ITwilioMessenger _twilioMessenger;

        public WhatsAppController(IOrchestrationService orchestrator, ITwilioMessenger twilioMessenger)
        {
            _orchestrator = orchestrator;
            _twilioMessenger = twilioMessenger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromForm] TwilioWebhookModel input)
        {
            var reply = await _orchestrator.HandleMessageAsync(input.From, input.Body);
            await _twilioMessenger.SendMessageAsync(input.From, reply);
            return Ok();
        }
    }
}