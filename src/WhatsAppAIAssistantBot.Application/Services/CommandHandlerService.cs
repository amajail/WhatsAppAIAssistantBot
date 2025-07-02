using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application.Services;

public interface ICommandHandlerService
{
    Task<bool> HandleCommandAsync(User user, string message);
}

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