using BotTester.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotTester.Services;

/// <summary>
/// Service for communicating with the bot API
/// </summary>
public class BotApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BotApiClient> _logger;
    private readonly string _botApiUrl;
    private readonly string _testPhoneNumber;
    private readonly string _botPhoneNumber;

    public BotApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BotApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _botApiUrl = $"{_configuration["BotApi:BaseUrl"]}{_configuration["BotApi:WhatsAppEndpoint"]}";
        _testPhoneNumber = _configuration["TestUser:PhoneNumber"] ?? "whatsapp:+1234567890";
        _botPhoneNumber = _configuration["TestUser:BotNumber"] ?? "whatsapp:+15551234567";
    }

    /// <summary>
    /// Send a message to the bot API
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <returns>HTTP response status</returns>
    public async Task<bool> SendMessageAsync(string message)
    {
        try
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("From", _testPhoneNumber),
                new("To", _botPhoneNumber),
                new("Body", message),
                new("MessageSid", GenerateMessageSid()),
                new("AccountSid", "CONSOLE_TEST"),
                new("NumMedia", "0")
            };

            var content = new FormUrlEncodedContent(formData);
            
            _logger.LogInformation("Sending message to bot: {Message}", message);
            
            var response = await _httpClient.PostAsync(_botApiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully. Status: {StatusCode}", response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send message. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to bot API");
            return false;
        }
    }

    /// <summary>
    /// Generate a unique message SID for testing
    /// </summary>
    private static string GenerateMessageSid()
    {
        return $"SM{Guid.NewGuid().ToString("N")[..32]}";
    }
}