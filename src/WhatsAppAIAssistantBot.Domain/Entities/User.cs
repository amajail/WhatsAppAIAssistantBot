using WhatsAppAIAssistantBot.Domain.Models;

namespace WhatsAppAIAssistantBot.Domain.Entities;

/// <summary>
/// Represents a WhatsApp user in the system with their profile information, 
/// conversation context, and registration status.
/// </summary>
/// <remarks>
/// Users are identified by their WhatsApp phone number and maintain state for
/// AI conversation threads, language preferences, and registration completion.
/// Registration is considered complete when both Name and Email are provided.
/// </remarks>
public class User
{
    /// <summary>
    /// Gets or sets the user's WhatsApp phone number identifier (e.g., "whatsapp:+1234567890").
    /// This serves as the primary key and unique identifier for the user.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the OpenAI Assistant thread ID for maintaining conversation context.
    /// Each user has a persistent thread that preserves conversation history across sessions.
    /// </summary>
    public string ThreadId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user's display name. Required for registration completion.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the user's email address. Required for registration completion.
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Gets or sets the user's preferred language code (e.g., "en", "es").
    /// Defaults to Spanish ("es") for new users.
    /// </summary>
    public string LanguageCode { get; set; } = "es";
    /// <summary>
    /// Gets or sets the timestamp when the user record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the timestamp when the user record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>
    /// Gets a value indicating whether the user has completed registration.
    /// Registration is complete when both Name and Email are provided.
    /// </summary>
    public bool IsRegistered => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Email);
    
    /// <summary>
    /// Gets the user's language preference as a strongly-typed SupportedLanguage enum.
    /// This property converts the LanguageCode string to the corresponding enum value.
    /// </summary>
    public SupportedLanguage Language => SupportedLanguageExtensions.FromCode(LanguageCode);
}