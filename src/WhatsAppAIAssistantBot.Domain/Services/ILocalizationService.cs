using WhatsAppAIAssistantBot.Domain.Models;

namespace WhatsAppAIAssistantBot.Domain.Services;

/// <summary>
/// Service contract for managing localized messages and text resources.
/// Provides methods for retrieving localized content based on language preferences
/// and managing language support validation.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Retrieves a localized message by key for the specified language with optional parameters.
    /// </summary>
    /// <param name="key">The localization key to retrieve</param>
    /// <param name="language">The target language for localization</param>
    /// <param name="parameters">Optional parameters for string formatting</param>
    /// <returns>The localized message with parameters applied</returns>
    Task<string> GetLocalizedMessageAsync(string key, SupportedLanguage language, params object[] parameters);
    /// <summary>
    /// Retrieves a localized message by key for the specified language code with optional parameters.
    /// </summary>
    /// <param name="key">The localization key to retrieve</param>
    /// <param name="languageCode">The language code (e.g., "en", "es")</param>
    /// <param name="parameters">Optional parameters for string formatting</param>
    /// <returns>The localized message with parameters applied</returns>
    Task<string> GetLocalizedMessageAsync(string key, string languageCode, params object[] parameters);
    /// <summary>
    /// Gets the default language for the application.
    /// </summary>
    /// <returns>The default supported language</returns>
    Task<SupportedLanguage> GetDefaultLanguageAsync();
    /// <summary>
    /// Determines whether a language code is supported by the localization system.
    /// </summary>
    /// <param name="languageCode">The language code to validate</param>
    /// <returns>True if the language is supported, false otherwise</returns>
    Task<bool> IsLanguageSupportedAsync(string languageCode);
    /// <summary>
    /// Retrieves all localized messages for a specific language.
    /// </summary>
    /// <param name="language">The target language</param>
    /// <returns>A dictionary of all localization keys and their values</returns>
    Task<Dictionary<string, string>> GetAllMessagesAsync(SupportedLanguage language);
}

/// <summary>
/// Static class containing all localization keys used throughout the application.
/// These keys correspond to entries in the localization resource files.
/// </summary>
/// <remarks>
/// Keys are organized by functional area: registration, validation, language switching,
/// help messages, error messages, extraction patterns, and context templates.
/// </remarks>
public static class LocalizationKeys
{
    // Registration messages
    public const string WelcomeMessage = "welcome_message";
    public const string RequestName = "request_name";
    public const string GreetWithName = "greet_with_name";
    public const string RequestEmail = "request_email";
    public const string RegistrationComplete = "registration_complete";
    
    // Validation messages
    public const string InvalidEmail = "invalid_email";
    public const string InvalidName = "invalid_name";
    
    // Language switching
    public const string LanguageChanged = "language_changed";
    public const string LanguageNotSupported = "language_not_supported";
    public const string CurrentLanguage = "current_language";
    
    // Help messages
    public const string HelpMessage = "help_message";
    public const string AvailableCommands = "available_commands";
    
    // Error messages
    public const string GeneralError = "general_error";
    public const string ServiceUnavailable = "service_unavailable";
    
    // Extraction patterns
    public const string NamePatterns = "name_patterns";
    public const string EmailPatterns = "email_patterns";
    
    // LLM extraction prompts
    public const string LlmExtractNamePrompt = "llm_extract_name_prompt";
    public const string LlmExtractEmailPrompt = "llm_extract_email_prompt"; 
    public const string LlmExtractBothPrompt = "llm_extract_both_prompt";
    
    // Confirmation messages
    public const string ConfirmExtractedData = "confirm_extracted_data";
    public const string ConfirmExtractedName = "confirm_extracted_name";
    public const string ConfirmExtractedEmail = "confirm_extracted_email";
    public const string ExtractionFailed = "extraction_failed";
    
    // Context templates
    public const string ContextTemplate = "context_template";
    public const string ContextTemplateMinimal = "context_template_minimal";
    public const string ContextTemplateFull = "context_template_full";
    
    // Context detection patterns
    public const string PersonalQuestionPatterns = "personal_question_patterns";
    public const string NameQuestionPatterns = "name_question_patterns";
    public const string EmailQuestionPatterns = "email_question_patterns";
}