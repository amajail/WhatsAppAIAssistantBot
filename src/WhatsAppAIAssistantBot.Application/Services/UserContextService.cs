using System.Text.Json;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;

namespace WhatsAppAIAssistantBot.Application.Services;

public class UserContextService : IUserContextService
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(
        ILocalizationService localizationService,
        ILogger<UserContextService> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task<string> FormatUserContextAsync(User user, string message, ContextLevel level = ContextLevel.Standard)
    {
        _logger.LogInformation("Formatting user context for user {PhoneNumber} with level {Level}", user.PhoneNumber, level);

        try
        {
            return level switch
            {
                ContextLevel.None => message,
                ContextLevel.Minimal => await FormatMinimalContextAsync(user, message),
                ContextLevel.Standard => await FormatStandardContextAsync(user, message),
                ContextLevel.Full => await FormatFullContextAsync(user, message),
                _ => message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting user context for user {PhoneNumber}", user.PhoneNumber);
            return message; // Fallback to original message if context formatting fails
        }
    }

    public async Task<bool> ShouldIncludeContextAsync(string message)
    {
        // Skip context for system commands
        var lowerMessage = message.ToLower().Trim();
        if (lowerMessage.StartsWith("/") || lowerMessage.StartsWith("help") || lowerMessage.StartsWith("ayuda"))
        {
            return false;
        }

        // Always include context for personal questions
        var personalPatterns = await GetPersonalQuestionPatternsAsync("en"); // Default to English for pattern matching
        if (personalPatterns.Any(pattern => lowerMessage.Contains(pattern.ToLower())))
        {
            return true;
        }

        // Include context for most user interactions (default behavior)
        return true;
    }

    public async Task<ContextLevel> DetermineContextLevelAsync(string message)
    {
        var lowerMessage = message.ToLower().Trim();

        try
        {
            // Check for personal information questions
            var namePatterns = await GetNameQuestionPatternsAsync("en");
            var emailPatterns = await GetEmailQuestionPatternsAsync("en");
            var personalPatterns = await GetPersonalQuestionPatternsAsync("en");

            // Full context for personal information questions
            if (namePatterns.Any(pattern => lowerMessage.Contains(pattern.ToLower())) ||
                emailPatterns.Any(pattern => lowerMessage.Contains(pattern.ToLower())) ||
                personalPatterns.Any(pattern => lowerMessage.Contains(pattern.ToLower())))
            {
                return ContextLevel.Full;
            }

            // Minimal context for short, simple messages
            if (message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 3)
            {
                return ContextLevel.Minimal;
            }

            // Standard context for regular conversation
            return ContextLevel.Standard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining context level for message: {Message}", message);
            return ContextLevel.Standard; // Default to standard on error
        }
    }

    private async Task<string> FormatMinimalContextAsync(User user, string message)
    {
        var template = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplateMinimal, user.LanguageCode);

        return string.Format(template, 
            user.Name ?? "Usuario", 
            message);
    }

    private async Task<string> FormatStandardContextAsync(User user, string message)
    {
        var template = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplate, user.LanguageCode);

        var languageDisplay = user.Language.ToDisplayName();

        return string.Format(template,
            user.Name ?? "Usuario",
            user.Email ?? "No proporcionado", 
            languageDisplay,
            message);
    }

    private async Task<string> FormatFullContextAsync(User user, string message)
    {
        var template = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.ContextTemplateFull, user.LanguageCode);

        var languageDisplay = user.Language.ToDisplayName();
        var memberSince = user.CreatedAt.ToString("yyyy-MM-dd");
        var timezone = "UTC"; // Default timezone, could be configurable

        return string.Format(template,
            user.Name ?? "Usuario",
            user.Email ?? "No proporcionado",
            languageDisplay, 
            memberSince,
            timezone,
            message);
    }

    private async Task<string[]> GetPersonalQuestionPatternsAsync(string languageCode)
    {
        try
        {
            var patternsJson = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.PersonalQuestionPatterns, languageCode);
            return JsonSerializer.Deserialize<string[]>(patternsJson) ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personal question patterns for language {LanguageCode}", languageCode);
            return Array.Empty<string>();
        }
    }

    private async Task<string[]> GetNameQuestionPatternsAsync(string languageCode)
    {
        try
        {
            var patternsJson = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.NameQuestionPatterns, languageCode);
            return JsonSerializer.Deserialize<string[]>(patternsJson) ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading name question patterns for language {LanguageCode}", languageCode);
            return Array.Empty<string>();
        }
    }

    private async Task<string[]> GetEmailQuestionPatternsAsync(string languageCode)
    {
        try
        {
            var patternsJson = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.EmailQuestionPatterns, languageCode);
            return JsonSerializer.Deserialize<string[]>(patternsJson) ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email question patterns for language {LanguageCode}", languageCode);
            return Array.Empty<string>();
        }
    }
}