namespace BotTester.Models;

/// <summary>
/// Model for receiving Twilio webhook responses from the bot
/// </summary>
public class TwilioWebhookModel
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string MessageSid { get; set; } = string.Empty;
    public string AccountSid { get; set; } = string.Empty;
    public string NumMedia { get; set; } = "0";
}

/// <summary>
/// Model for sending messages to the bot API
/// </summary>
public class BotMessageRequest
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string MessageSid { get; set; } = string.Empty;
    public string AccountSid { get; set; } = string.Empty;
    public string NumMedia { get; set; } = "0";
}