using WhatsAppAIAssistantBot.Infrastructure;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;

namespace WhatsAppAIAssistantBot.Application;

public interface IOrchestrationService
{
    Task HandleMessageAsync(string userId, string message);
}

public class OrchestrationService(ISemanticKernelService sk,
                                  IAssistantService assistant,
                                  ITwilioMessenger twilioMessenger,
                                  IUserStorageService userStorageService,
                                  ILocalizationService localizationService) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;
    private readonly IUserStorageService _userStorageService = userStorageService;
    private readonly ILocalizationService _localizationService = localizationService;

    public async Task HandleMessageAsync(string userId, string message)
    {
        // Get or create thread ID (this will also create user if needed)
        var threadId = await _assistant.CreateOrGetThreadAsync(userId);
        
        // Get user from database (should exist now)
        var user = await _userStorageService.GetUserByPhoneNumberAsync(userId);
        
        if (user == null)
        {
            // This shouldn't happen, but create user with default language if it does
            user = new User
            {
                PhoneNumber = userId,
                ThreadId = threadId,
                LanguageCode = "es",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user = await _userStorageService.CreateOrUpdateUserAsync(user);
        }

        // Handle language switching commands
        if (await HandleLanguageCommandAsync(user, message))
        {
            return;
        }

        // Handle help commands
        if (await HandleHelpCommandAsync(user, message))
        {
            return;
        }

        if (!user.IsRegistered)
        {
            await HandleUserRegistrationAsync(user, message);
            return;
        }

        // if (message.ToLower().Contains("time"))
        // {
        //     return await _sk.RunLocalSkillAsync(message);
        // }

        var reply = await _assistant.GetAssistantReplyAsync(threadId, message);
        await twilioMessenger.SendMessageAsync(userId, reply);
    }

    private async Task HandleUserRegistrationAsync(User user, string message)
    {
        if (string.IsNullOrEmpty(user.Name))
        {
            if (message.ToLower().StartsWith("name:") || message.ToLower().StartsWith("my name is") ||
                message.ToLower().StartsWith("nombre:") || message.ToLower().StartsWith("mi nombre es"))
            {
                var name = ExtractNameFromMessage(message);
                if (!string.IsNullOrEmpty(name))
                {
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, name, user.Email ?? string.Empty);
                    var greetMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.GreetWithName, user.LanguageCode, name);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, greetMessage);
                    return;
                }
            }
            
            var welcomeMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.WelcomeMessage, user.LanguageCode);
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, welcomeMessage);
            return;
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            if (message.ToLower().StartsWith("email:") || message.ToLower().StartsWith("correo:"))
            {
                var email = ExtractEmailFromMessage(message);
                if (!string.IsNullOrEmpty(email) && IsValidEmail(email))
                {
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, user.Name, email);
                    var completionMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.RegistrationComplete, user.LanguageCode, user.Name);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, completionMessage);
                    return;
                }
                else
                {
                    var invalidEmailMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.InvalidEmail, user.LanguageCode);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, invalidEmailMessage);
                    return;
                }
            }
            
            var requestEmailMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.RequestEmail, user.LanguageCode);
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, requestEmailMessage);
            return;
        }
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
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, successMessage);
                }
                else
                {
                    var errorMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.LanguageNotSupported, user.LanguageCode);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, errorMessage);
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
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, helpMessage);
            return true;
        }
        
        return false;
    }

    private static string ExtractNameFromMessage(string message)
    {
        var lowerMessage = message.ToLower();
        
        if (lowerMessage.StartsWith("name:"))
        {
            return message.Substring(5).Trim();
        }
        if (lowerMessage.StartsWith("my name is"))
        {
            return message.Substring(10).Trim();
        }
        if (lowerMessage.StartsWith("nombre:"))
        {
            return message.Substring(7).Trim();
        }
        if (lowerMessage.StartsWith("mi nombre es"))
        {
            return message.Substring(12).Trim();
        }
        return string.Empty;
    }

    private static string ExtractEmailFromMessage(string message)
    {
        var lowerMessage = message.ToLower();
        
        if (lowerMessage.StartsWith("email:"))
        {
            return message.Substring(6).Trim();
        }
        if (lowerMessage.StartsWith("correo:"))
        {
            return message.Substring(7).Trim();
        }
        return string.Empty;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
