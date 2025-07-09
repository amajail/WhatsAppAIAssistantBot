namespace WhatsAppAIAssistantBot.Domain.Services;

/// <summary>
/// Service contract for webhook security operations including signature validation and input sanitization.
/// Provides security measures to ensure webhook requests are authentic and safe to process.
/// </summary>
public interface IWebhookSecurityService
{
    /// <summary>
    /// Validates the Twilio webhook signature to ensure the request is authentic.
    /// Uses HMAC-SHA1 with the configured auth token to verify the request signature.
    /// </summary>
    /// <param name="signature">The X-Twilio-Signature header value from the request</param>
    /// <param name="url">The complete webhook URL including query parameters</param>
    /// <param name="formData">The form data from the webhook request</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    bool ValidateSignature(string signature, string url, IEnumerable<KeyValuePair<string, string>> formData);

    /// <summary>
    /// Sanitizes user input message to prevent potential security issues.
    /// Removes potentially harmful characters and limits message length.
    /// </summary>
    /// <param name="message">The raw message from the user</param>
    /// <returns>Sanitized message safe for processing</returns>
    string SanitizeMessage(string message);

    /// <summary>
    /// Determines if signature validation should be enforced based on the current environment.
    /// Typically disabled in development/testing environments for easier testing.
    /// </summary>
    /// <returns>True if signature validation should be enforced, false otherwise</returns>
    bool ShouldValidateSignature();
}