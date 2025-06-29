using WhatsAppAIAssistantBot.Domain.Models;

namespace WhatsAppAIAssistantBot.Domain.Services;

public interface ILocalizationService
{
    Task<string> GetLocalizedMessageAsync(string key, SupportedLanguage language, params object[] parameters);
    Task<string> GetLocalizedMessageAsync(string key, string languageCode, params object[] parameters);
    Task<SupportedLanguage> GetDefaultLanguageAsync();
    Task<bool> IsLanguageSupportedAsync(string languageCode);
    Task<Dictionary<string, string>> GetAllMessagesAsync(SupportedLanguage language);
}

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