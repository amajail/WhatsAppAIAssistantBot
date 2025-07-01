using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application.Services;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly IUserDataExtractionService _userDataExtractionService;
    private readonly IUserStorageService _userStorageService;
    private readonly ILocalizationService _localizationService;
    private readonly ITwilioMessenger _twilioMessenger;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        IUserDataExtractionService userDataExtractionService,
        IUserStorageService userStorageService,
        ILocalizationService localizationService,
        ITwilioMessenger twilioMessenger,
        ILogger<UserRegistrationService> logger)
    {
        _userDataExtractionService = userDataExtractionService;
        _userStorageService = userStorageService;
        _localizationService = localizationService;
        _twilioMessenger = twilioMessenger;
        _logger = logger;
    }

    public bool IsRegistrationComplete(User user)
    {
        return !string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.Email);
    }

    public UserRegistrationState GetRegistrationState(User user)
    {
        if (string.IsNullOrEmpty(user.Name))
            return UserRegistrationState.New;
        
        if (string.IsNullOrEmpty(user.Email))
            return UserRegistrationState.HasName;
        
        return UserRegistrationState.Complete;
    }

    public async Task<RegistrationResult> ProcessRegistrationAsync(User user, string message)
    {
        _logger.LogInformation("Starting registration process for user {UserId} - HasName: {HasName}, HasEmail: {HasEmail}", 
            user.PhoneNumber, !string.IsNullOrEmpty(user.Name), !string.IsNullOrEmpty(user.Email));
        
        try
        {
            // Extract user data using the hybrid extraction service
            var extractionRequest = new ExtractionRequest
            {
                Message = message,
                LanguageCode = user.LanguageCode,
                ThreadId = user.ThreadId
            };
            var extractionResult = await _userDataExtractionService.ExtractUserDataAsync(extractionRequest);
            
            _logger.LogDebug("Extraction completed - NameExtracted: {NameExtracted}, EmailExtracted: {EmailExtracted}", 
                extractionResult.Name?.IsSuccessful == true, extractionResult.Email?.IsSuccessful == true);

            // Handle name extraction when user doesn't have a name yet
            if (string.IsNullOrEmpty(user.Name))
            {
                return await HandleNameExtractionAsync(user, extractionResult);
            }

            // Handle email extraction when user has name but no email
            if (string.IsNullOrEmpty(user.Email))
            {
                return await HandleEmailExtractionAsync(user, extractionResult);
            }

            // Should not reach here if registration is complete
            _logger.LogWarning("ProcessRegistrationAsync called for fully registered user {UserId}", user.PhoneNumber);
            return new RegistrationResult { IsCompleted = true, RequiresResponse = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration process for user {UserId}", user.PhoneNumber);
            throw;
        }
    }

    private async Task<RegistrationResult> HandleNameExtractionAsync(User user, UserDataExtractionResult extractionResult)
    {
        if (extractionResult.Name?.IsSuccessful == true)
        {
            var extractedName = extractionResult.Name.ExtractedValue!;
            _logger.LogInformation("Extracted name '{Name}' for user {UserId} using {Method}", 
                extractedName, user.PhoneNumber, extractionResult.Name.Method);
            
            // If we also got email in the same message, complete registration
            if (extractionResult.Email?.IsSuccessful == true)
            {
                var extractedEmail = extractionResult.Email.ExtractedValue!;
                _logger.LogInformation("Completing registration with name '{Name}' and email '{Email}' for user {UserId}", 
                    extractedName, extractedEmail, user.PhoneNumber);
                
                await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, extractedName, extractedEmail);
                var completionMessage = await _localizationService.GetLocalizedMessageAsync(
                    LocalizationKeys.RegistrationComplete, user.LanguageCode, extractedName);

                return new RegistrationResult
                {
                    IsCompleted = true,
                    RequiresResponse = true,
                    ResponseMessage = completionMessage,
                    Action = RegistrationAction.CompleteRegistration
                };
            }
            else
            {
                _logger.LogInformation("Updating name '{Name}' and requesting email for user {UserId}", 
                    extractedName, user.PhoneNumber);
                
                // Just update name and ask for email
                await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, extractedName, string.Empty);
                var greetMessage = await _localizationService.GetLocalizedMessageAsync(
                    LocalizationKeys.GreetWithName, user.LanguageCode, extractedName);

                return new RegistrationResult
                {
                    IsCompleted = false,
                    RequiresResponse = true,
                    ResponseMessage = greetMessage,
                    Action = RegistrationAction.GreetWithName
                };
            }
        }
        
        _logger.LogDebug("No name extracted from message, requesting name from user {UserId}", user.PhoneNumber);
        
        // No name extracted, ask for name
        var welcomeMessage = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.WelcomeMessage, user.LanguageCode);

        return new RegistrationResult
        {
            IsCompleted = false,
            RequiresResponse = true,
            ResponseMessage = welcomeMessage,
            Action = RegistrationAction.RequestName
        };
    }

    private async Task<RegistrationResult> HandleEmailExtractionAsync(User user, UserDataExtractionResult extractionResult)
    {
        if (extractionResult.Email?.IsSuccessful == true)
        {
            var extractedEmail = extractionResult.Email.ExtractedValue!;
            _logger.LogInformation("Completing registration with email '{Email}' for user {UserId} (Name: '{Name}')", 
                extractedEmail, user.PhoneNumber, user.Name);
            
            await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, user.Name ?? "Unknown", extractedEmail);
            var completionMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.RegistrationComplete, user.LanguageCode, user.Name ?? "User");

            return new RegistrationResult
            {
                IsCompleted = true,
                RequiresResponse = true,
                ResponseMessage = completionMessage,
                Action = RegistrationAction.CompleteRegistration
            };
        }
        else if (extractionResult.Email != null && !extractionResult.Email.IsSuccessful)
        {
            _logger.LogWarning("Email extraction failed for user {UserId}, sending invalid email message", user.PhoneNumber);
            
            // Extraction was attempted but failed
            var invalidEmailMessage = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.InvalidEmail, user.LanguageCode);

            return new RegistrationResult
            {
                IsCompleted = false,
                RequiresResponse = true,
                ResponseMessage = invalidEmailMessage,
                Action = RegistrationAction.ShowInvalidEmail
            };
        }
        
        _logger.LogDebug("No email extracted from message, requesting email from user {UserId}", user.PhoneNumber);
        
        // No email extracted, ask for email
        var requestEmailMessage = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.RequestEmail, user.LanguageCode);

        return new RegistrationResult
        {
            IsCompleted = false,
            RequiresResponse = true,
            ResponseMessage = requestEmailMessage,
            Action = RegistrationAction.RequestEmail
        };
    }
}