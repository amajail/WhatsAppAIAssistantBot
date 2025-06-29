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
                                  ILocalizationService localizationService,
                                  IUserDataExtractionService userDataExtractionService,
                                  IUserContextService userContextService) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;
    private readonly IUserStorageService _userStorageService = userStorageService;
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IUserDataExtractionService _userDataExtractionService = userDataExtractionService;
    private readonly IUserContextService _userContextService = userContextService;

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

        // Use context-aware LLM interaction for registered users
        string reply;
        if (await _userContextService.ShouldIncludeContextAsync(message))
        {
            var contextLevel = await _userContextService.DetermineContextLevelAsync(message);
            var contextualMessage = await _userContextService.FormatUserContextAsync(user, message, contextLevel);
            reply = await _assistant.GetAssistantReplyWithContextAsync(threadId, contextualMessage);
        }
        else
        {
            reply = await _assistant.GetAssistantReplyAsync(threadId, message);
        }
        
        await twilioMessenger.SendMessageAsync(userId, reply);
    }

    private async Task HandleUserRegistrationAsync(User user, string message)
    {
        // Try to extract user data using the hybrid extraction service
        var extractionResult = await _userDataExtractionService.ExtractUserDataAsync(message, user.LanguageCode);

        // Handle name extraction when user doesn't have a name yet
        if (string.IsNullOrEmpty(user.Name))
        {
            if (extractionResult.Name?.IsSuccessful == true)
            {
                var extractedName = extractionResult.Name.ExtractedValue!;
                
                // If we also got email in the same message, update both
                if (extractionResult.Email?.IsSuccessful == true)
                {
                    var extractedEmail = extractionResult.Email.ExtractedValue!;
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, extractedName, extractedEmail);
                    var completionMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.RegistrationComplete, user.LanguageCode, extractedName);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, completionMessage);
                    return;
                }
                else
                {
                    // Just update name and ask for email
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, extractedName, string.Empty);
                    var greetMessage = await _localizationService.GetLocalizedMessageAsync(
                        LocalizationKeys.GreetWithName, user.LanguageCode, extractedName);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, greetMessage);
                    return;
                }
            }
            
            // No name extracted, ask for name
            var welcomeMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.WelcomeMessage, user.LanguageCode);
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, welcomeMessage);
            return;
        }

        // Handle email extraction when user has name but no email
        if (string.IsNullOrEmpty(user.Email))
        {
            if (extractionResult.Email?.IsSuccessful == true)
            {
                var extractedEmail = extractionResult.Email.ExtractedValue!;
                await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, user.Name, extractedEmail);
                var completionMessage = await _localizationService.GetLocalizedMessageAsync(
                    LocalizationKeys.RegistrationComplete, user.LanguageCode, user.Name);
                await twilioMessenger.SendMessageAsync(user.PhoneNumber, completionMessage);
                return;
            }
            else if (extractionResult.Email != null && !extractionResult.Email.IsSuccessful)
            {
                // Extraction was attempted but failed
                var invalidEmailMessage = await _localizationService.GetLocalizedMessageAsync(
                    LocalizationKeys.InvalidEmail, user.LanguageCode);
                await twilioMessenger.SendMessageAsync(user.PhoneNumber, invalidEmailMessage);
                return;
            }
            
            // No email extracted, ask for email
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

}
