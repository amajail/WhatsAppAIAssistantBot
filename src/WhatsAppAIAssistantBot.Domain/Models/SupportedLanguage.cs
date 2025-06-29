namespace WhatsAppAIAssistantBot.Domain.Models;

public enum SupportedLanguage
{
    Spanish,
    English
}

public static class SupportedLanguageExtensions
{
    public static string ToCode(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Spanish => "es",
            SupportedLanguage.English => "en",
            _ => "es" // Default to Spanish
        };
    }

    public static SupportedLanguage FromCode(string code)
    {
        return code?.ToLower() switch
        {
            "en" or "english" => SupportedLanguage.English,
            "es" or "spanish" or "español" => SupportedLanguage.Spanish,
            _ => SupportedLanguage.Spanish // Default to Spanish
        };
    }

    public static string ToDisplayName(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Spanish => "Español",
            SupportedLanguage.English => "English",
            _ => "Español"
        };
    }
}