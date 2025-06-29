using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Domain.Services;

public interface IUserContextService
{
    Task<string> FormatUserContextAsync(User user, string message, ContextLevel level = ContextLevel.Standard);
    Task<bool> ShouldIncludeContextAsync(string message);
    Task<ContextLevel> DetermineContextLevelAsync(string message);
}

public enum ContextLevel
{
    None,
    Minimal,    // Name only
    Standard,   // Name, email, language
    Full        // All user data + preferences, registration date
}

public static class UserContextKeys
{
    // Context templates
    public const string ContextTemplate = "context_template";
    public const string ContextTemplateMinimal = "context_template_minimal";
    public const string ContextTemplateFull = "context_template_full";
    
    // Context detection patterns
    public const string PersonalQuestionPatterns = "personal_question_patterns";
    public const string NameQuestionPatterns = "name_question_patterns";
    public const string EmailQuestionPatterns = "email_question_patterns";
}