using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Infrastructure;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Application.Services;

namespace WhatsAppAIAssistantBot.Application;

/// <summary>
/// Orchestrates the processing of incoming WhatsApp messages, coordinating user registration,
/// command processing, and AI-powered conversations through various specialized services.
/// </summary>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes an incoming message from a user, handling the complete workflow from
    /// user initialization through message processing and response generation.
    /// </summary>
    /// <param name="userId">The user's phone number identifier (e.g., "whatsapp:+1234567890")</param>
    /// <param name="message">The incoming message content from the user</param>
    /// <returns>A task representing the asynchronous message processing operation</returns>
    /// <exception cref="ArgumentException">Thrown when userId or message parameters are invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when message processing fails due to system state</exception>
    Task HandleMessageAsync(string userId, string message);
}

/// <summary>
/// Main orchestration service that coordinates the processing of WhatsApp messages.
/// This service implements the primary business workflow: user initialization, command handling,
/// user registration management, and AI-powered conversation processing.
/// </summary>
/// <remarks>
/// The orchestration follows this flow:
/// 1. Validate and initialize user context
/// 2. Check for and process commands (language switching, help)
/// 3. Handle user registration flow for unregistered users
/// 4. Process conversations through AI assistant for registered users
/// </remarks>
public class OrchestrationService(ISemanticKernelService sk,
                                  IAssistantService assistant,
                                  ITwilioMessenger twilioMessenger,
                                  IUserStorageService userStorageService,
                                  IUserDataExtractionService userDataExtractionService,
                                  IUserContextService userContextService,
                                  IUserRegistrationService userRegistrationService,
                                  ICommandHandlerService commandHandlerService,
                                  ILogger<OrchestrationService> logger) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;
    private readonly ITwilioMessenger _twilioMessenger = twilioMessenger;
    private readonly IUserStorageService _userStorageService = userStorageService;
    private readonly IUserDataExtractionService _userDataExtractionService = userDataExtractionService;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserRegistrationService _userRegistrationService = userRegistrationService;
    private readonly ICommandHandlerService _commandHandlerService = commandHandlerService;
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
            // Validate message input
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Empty or null message received for user {UserId}", userId);
                return;
            }

            // Initialize user and thread
            var (user, threadId) = await GetOrCreateUserAsync(userId);

            _logger.LogDebug("User initialized - IsRegistered: {IsRegistered}, Language: {Language}, ThreadId: {ThreadId}", 
                user.IsRegistered, user.LanguageCode, threadId);

            // Try to handle as command first
            if (!string.IsNullOrEmpty(message) && await _commandHandlerService.HandleCommandAsync(user, message))
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

    /// <summary>
    /// Initializes user context by creating or retrieving an existing user and their OpenAI thread.
    /// This method ensures both the user record and OpenAI thread exist for message processing.
    /// </summary>
    /// <param name="userId">The user's phone number identifier</param>
    /// <returns>A tuple containing the user entity and their OpenAI thread ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when user or thread creation fails</exception>
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


    /// <summary>
    /// Processes a conversation message for a registered user through the AI assistant.
    /// Determines whether to include user context based on message content and generates an appropriate response.
    /// </summary>
    /// <param name="user">The registered user entity</param>
    /// <param name="threadId">The OpenAI thread ID for conversation continuity</param>
    /// <param name="message">The user's message content</param>
    /// <returns>A task representing the asynchronous conversation processing</returns>
    /// <exception cref="InvalidOperationException">Thrown when AI assistant interaction fails</exception>
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
            
            await _twilioMessenger.SendMessageAsync(user.PhoneNumber, reply ?? "Sorry, I couldn't generate a response.");
            
            _logger.LogDebug("Conversation handling completed for user {UserId}", user.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in conversation handling for user {UserId} with thread {ThreadId}", 
                user.PhoneNumber, threadId);
            throw;
        }
    }


}