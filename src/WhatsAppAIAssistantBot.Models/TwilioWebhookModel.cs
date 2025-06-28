namespace WhatsAppAIAssistantBot.Models;

public class TwilioWebhookModel
{
    public required string Body { get; set; }
    public required string From { get; set; }
}