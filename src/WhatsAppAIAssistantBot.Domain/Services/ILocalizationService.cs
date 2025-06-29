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
}