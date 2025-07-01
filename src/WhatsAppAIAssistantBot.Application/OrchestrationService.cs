using Microsoft.Extensions.Logging;
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
                                  IUserContextService userContextService,
                                  IUserRegistrationService userRegistrationService,
                                  ILogger<OrchestrationService> logger) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;
    private readonly ITwilioMessenger _twilioMessenger = twilioMessenger;
    private readonly IUserStorageService _userStorageService = userStorageService;
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IUserDataExtractionService _userDataExtractionService = userDataExtractionService;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserRegistrationService _userRegistrationService = userRegistrationService;
    private readonly ILogger<OrchestrationService> _logger = logger;

    public async Task HandleMessageAsync(string userId, string message)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId,
            ["MessageLength"] = message?.Length ?? 0
        });

        _logger.LogInformation("Starting message processing for user {UserId} with correlation {CorrelationId}", 
            userId, correlationId);

        try
        {
            // Initialize user and thread
            var (user, threadId) = await GetOrCreateUserAsync(userId);

            _logger.LogDebug("User initialized - IsRegistered: {IsRegistered}, Language: {Language}, ThreadId: {ThreadId}", 
                user.IsRegistered, user.LanguageCode, threadId);

            // Try to handle as command first
            if (await HandleCommandAsync(user, message))
            {
                _logger.LogInformation("Message processed as command for user {UserId}", userId);
                return;
            }

            // Handle registration process for unregistered users
            if (!user.IsRegistered)
            {
                _logger.LogInformation("Processing registration flow for unregistered user {UserId}", userId);
                var registrationResult = await _userRegistrationService.ProcessRegistrationAsync(user, message);
                
                if (registrationResult.RequiresResponse && !string.IsNullOrEmpty(registrationResult.ResponseMessage))
                {
                    await _twilioMessenger.SendMessageAsync(user.PhoneNumber, registrationResult.ResponseMessage);
                }
                
                return;
            }

            // Handle regular conversation for registered users
            _logger.LogInformation("Processing conversation for registered user {UserId} - {UserName}", 
                userId, user.Name);
            await HandleConversationAsync(user, threadId, message);
            
            _logger.LogInformation("Message processing completed successfully for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for user {UserId} with correlation {CorrelationId}", 
                userId, correlationId);
            throw;
        }
    }

    private async Task<(User user, string threadId)> GetOrCreateUserAsync(string userId)
    {
        _logger.LogDebug("Initializing user and thread for {UserId}", userId);
        
        try
        {
            // Get or create thread ID (this will also create user if needed)
            var threadId = await _assistant.CreateOrGetThreadAsync(userId);
            _logger.LogDebug("Thread created/retrieved: {ThreadId} for user {UserId}", threadId, userId);
            
            // Get user from database (should exist now)
            var user = await _userStorageService.GetUserByPhoneNumberAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("User not found in database after thread creation for {UserId}, creating new user", userId);
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
                _logger.LogInformation("Created new user {UserId} with default language {Language}", userId, user.LanguageCode);
            }

            return (user, threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize user and thread for {UserId}", userId);
            throw;
        }
    }

    private async Task<bool> HandleCommandAsync(User user, string message)
    {
        _logger.LogDebug("Checking for commands in message for user {UserId}", user.PhoneNumber);
        
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

    private async Task HandleConversationAsync(User user, string threadId, string message)
    {
        _logger.LogDebug("Starting conversation handling for user {UserId} with thread {ThreadId}", 
            user.PhoneNumber, threadId);
        
        try
        {
            // Use context-aware LLM interaction for registered users
            string reply;
            var shouldIncludeContext = await _userContextService.ShouldIncludeContextAsync(message);
            
            if (shouldIncludeContext)
            {
                var contextLevel = await _userContextService.DetermineContextLevelAsync(message);
                _logger.LogDebug("Using context-aware reply with level {ContextLevel} for user {UserId}", 
                    contextLevel, user.PhoneNumber);
                
                var contextualMessage = await _userContextService.FormatUserContextAsync(user, message, contextLevel);
                reply = await _assistant.GetAssistantReplyWithContextAsync(threadId, contextualMessage);
            }
            else
            {
                _logger.LogDebug("Using standard reply without context for user {UserId}", user.PhoneNumber);
                reply = await _assistant.GetAssistantReplyAsync(threadId, message);
            }
            
            _logger.LogInformation("Generated reply for user {UserId}, length: {ReplyLength}", 
                user.PhoneNumber, reply?.Length ?? 0);
            
            await _twilioMessenger.SendMessageAsync(user.PhoneNumber, reply);
            
            _logger.LogDebug("Conversation handling completed for user {UserId}", user.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in conversation handling for user {UserId} with thread {ThreadId}", 
                user.PhoneNumber, threadId);
            throw;
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