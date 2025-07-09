using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Services;

namespace WhatsAppAIAssistantBot.Infrastructure.Services;

/// <summary>
/// Implementation of webhook security service that provides signature validation and input sanitization.
/// Handles Twilio webhook authentication using HMAC-SHA1 signatures and sanitizes user input messages.
/// </summary>
/// <remarks>
/// This service centralizes all webhook security concerns, ensuring that incoming requests are authentic
/// and safe to process. It supports environment-aware configuration to disable validation in development
/// while maintaining security in production environments.
/// </remarks>
public class WebhookSecurityService : IWebhookSecurityService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookSecurityService> _logger;

    /// <summary>
    /// Initializes a new instance of the WebhookSecurityService.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <param name="logger">The logger instance for this service</param>
    public WebhookSecurityService(
        IConfiguration configuration,
        ILogger<WebhookSecurityService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool ValidateSignature(string signature, string url, IEnumerable<KeyValuePair<string, string>> formData)
    {
        try
        {
            var authToken = _configuration["Twilio:AuthToken"];
            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Twilio AuthToken not configured");
                return false;
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing X-Twilio-Signature header");
                return false;
            }

            // Build the data string that Twilio signed
            var dataToSign = BuildDataString(url, formData);

            // Calculate expected signature using HMAC-SHA1
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
            var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

            var isValid = signature == expectedSignature;
            if (!isValid)
            {
                _logger.LogWarning("Signature validation failed. Expected: {Expected}, Received: {Received}", 
                    expectedSignature, signature);
            }
            else
            {
                _logger.LogDebug("Webhook signature validation successful");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Twilio signature");
            return false;
        }
    }

    public string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        // Limit message length to prevent abuse
        const int maxLength = 4000;
        if (message.Length > maxLength)
        {
            _logger.LogInformation("Message truncated from {OriginalLength} to {MaxLength} characters", 
                message.Length, maxLength);
            message = message[..maxLength];
        }

        // Remove potentially harmful characters
        message = message.Replace("\0", ""); // Null bytes
        message = message.Replace("\r\n", "\n"); // Normalize line endings
        message = message.Replace("\r", "\n"); // Normalize line endings
        
        // Remove excessive whitespace
        message = System.Text.RegularExpressions.Regex.Replace(message, @"\s+", " ");
        
        return message.Trim();
    }

    public bool ShouldValidateSignature()
    {
        // Check configuration setting for signature validation
        var value = _configuration["Twilio:ValidateSignature"];
        var shouldValidate = value == null ? true : bool.TryParse(value, out var result) ? result : true;

        _logger.LogDebug("Signature validation {Status} based on configuration", 
            shouldValidate ? "enabled" : "disabled");
        
        return shouldValidate;
    }

    /// <summary>
    /// Builds the data string that Twilio uses for signature calculation.
    /// Concatenates the URL with form parameters in alphabetical order.
    /// </summary>
    /// <param name="url">The complete webhook URL</param>
    /// <param name="formData">The form data from the request</param>
    /// <returns>The data string used for signature calculation</returns>
    private static string BuildDataString(string url, IEnumerable<KeyValuePair<string, string>> formData)
    {
        var dataBuilder = new StringBuilder(url);
        
        // Sort form data alphabetically by key and append to URL
        foreach (var kvp in formData.OrderBy(x => x.Key))
        {
            dataBuilder.Append($"{kvp.Key}{kvp.Value}");
        }
        
        return dataBuilder.ToString();
    }
}