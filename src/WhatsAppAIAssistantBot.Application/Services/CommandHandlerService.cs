using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application.Services;

/// <summary>
/// Service responsible for processing bot commands such as language switching and help requests.
/// Commands are special messages that start with '/' and trigger specific bot functionality
/// rather than conversational AI responses.
/// </summary>
public interface ICommandHandlerService
{
    /// <summary>
    /// Processes a message to determine if it contains a valid command and executes it.
    /// </summary>
    /// <param name="user">The user entity making the command request</param>
    /// <param name="message">The message content to check for commands</param>
    /// <returns>True if a command was recognized and processed, false if no command was found</returns>
    /// <exception cref="InvalidOperationException">Thrown when command processing fails</exception>
    Task<bool> HandleCommandAsync(User user, string message);
}

/// <summary>
/// Implementation of command handling service that processes bot commands.
/// Supports language switching commands (/lang, /idioma) and help commands (/help, /ayuda).
/// Commands are case-insensitive and support both English and Spanish variants.
/// </summary>
/// <remarks>
/// Supported commands:
/// - /lang [code] or /idioma [code]: Switch user's language preference
/// - /help or /ayuda: Display help information in user's current language
/// </remarks>
public class CommandHandlerService(
    ILocalizationService localizationService,
    IUserStorageService userStorageService,
    ITwilioMessenger twilioMessenger,
    ILogger<CommandHandlerService> logger) : ICommandHandlerService
{
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IUserStorageService _userStorageService = userStorageService;
    private readonly ITwilioMessenger _twilioMessenger = twilioMessenger;
    private readonly ILogger<CommandHandlerService> _logger = logger;

    /// <summary>
    /// Processes a message to check for and execute bot commands.
    /// Returns true if a command was processed, false otherwise.
    /// </summary>
    /// <param name="user">The user making the command request</param>
    /// <param name="message">The message to process for commands</param>
    /// <returns>True if a command was found and processed, false if no valid command</returns>
    public async Task<bool> HandleCommandAsync(User user, string message)
    {
        _logger.LogDebug("Checking for commands in message for user {UserId}", user.PhoneNumber);
        
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }
        
        // Handle language switching commands
        if (await HandleLanguageCommandAsync(user, message))
        {
            _logger.LogInformation("Processed language command for user {UserId}", user.PhoneNumber);
            return true;
        }

        // Handle help commands
        if (await HandleHelpCommandAsync(user, message))
        {
            _logger.LogInformation("Processed help command for user {UserId}", user.PhoneNumber);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes language switching commands (/lang [code] or /idioma [code]).
    /// Validates the requested language code and updates the user's language preference if supported.
    /// </summary>
    /// <param name="user">The user requesting the language change</param>
    /// <param name="message">The command message containing the language code</param>
    /// <returns>True if a language command was processed, false otherwise</returns>
    private async Task<bool> HandleLanguageCommandAsync(User user, string message)
    {
        var lowerMessage = message.ToLower().Trim();
        
        // Handle language switching commands
        if (lowerMessage.StartsWith("/lang ") || lowerMessage.StartsWith("/idioma "))
        {
            var parts = lowerMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var requestedLanguage = parts[1];
                var supportedLanguage = SupportedLanguageExtensions.FromCode(requestedLanguage);
                
                if (await _localizationService.IsLanguageSupportedAsync(supportedLanguage.ToCode()))
                {
                    user.LanguageCode = supportedLanguage.ToCode();
                    await _userStorageService.CreateOrUpdateUserAsync(user);
                    
                    var successMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.LanguageChanged, user.LanguageCode, supportedLanguage.ToDisplayName());
                    await _twilioMessenger.SendMessageAsync(user.PhoneNumber, successMessage);
                }
                else
                {
                    var errorMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.LanguageNotSupported, user.LanguageCode);
                    await _twilioMessenger.SendMessageAsync(user.PhoneNumber, errorMessage);
                }
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Processes help commands (/help or /ayuda) by sending localized help information to the user.
    /// The help message is delivered in the user's current language preference.
    /// </summary>
    /// <param name="user">The user requesting help</param>
    /// <param name="message">The command message to check for help commands</param>
    /// <returns>True if a help command was processed, false otherwise</returns>
    private async Task<bool> HandleHelpCommandAsync(User user, string message)
    {
        var lowerMessage = message.ToLower().Trim();
        
        if (lowerMessage == "/help" || lowerMessage == "/ayuda")
        {
            var helpMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.HelpMessage, user.LanguageCode);
            await _twilioMessenger.SendMessageAsync(user.PhoneNumber, helpMessage);
            return true;
        }
        
        return false;
    }
}