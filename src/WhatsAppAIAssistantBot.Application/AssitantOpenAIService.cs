using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Assistants;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Application;

public interface IAssistantService
{
    Task<string> CreateOrGetThreadAsync(string userId);
    Task<string> GetAssistantReplyAsync(string threadId, string userMessage);
    Task<string> GetAssistantReplyWithContextAsync(string threadId, string contextualMessage);
}

public class AssistantOpenAIService : IAssistantService
{
    private readonly IConfiguration configuration;
    private readonly IUserStorageService _userStorageService;
    private readonly ILogger<AssistantOpenAIService> _logger;
    private readonly string apiKey;
    private readonly string assistantId;
    private readonly OpenAI.OpenAIClient openAIClient;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly AssistantClient assistantClient;
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    // Suppress warning for evaluation-only API usage
#pragma warning disable OPENAI001
    public AssistantOpenAIService(IConfiguration configuration, IUserStorageService userStorageService, ILogger<AssistantOpenAIService> logger)
    {
        this.configuration = configuration;
        _userStorageService = userStorageService;
        _logger = logger;
        apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is not configured.");
        assistantId = configuration["OpenAI:AssistantId"] ?? throw new InvalidOperationException("OpenAI Assistant ID is not configured.");
        openAIClient = new OpenAI.OpenAIClient(new ApiKeyCredential(apiKey));
        assistantClient = openAIClient.GetAssistantClient();
        
        _logger.LogInformation("AssistantOpenAIService initialized with assistant ID: {AssistantId}", assistantId);
    }
#pragma warning restore OPENAI001

    // Create the thread for the user if it doesn't exist
    // and return the thread ID.
    public async Task<string> CreateOrGetThreadAsync(string userId)
    {
        _logger.LogDebug("Creating or retrieving thread for user {UserId}", userId);
        
        try
        {
            // Check if user already exists in database with a thread
            var existingUser = await _userStorageService.GetUserByPhoneNumberAsync(userId);
            if (existingUser != null && !string.IsNullOrEmpty(existingUser.ThreadId))
            {
                _logger.LogDebug("Found existing thread {ThreadId} for user {UserId}", existingUser.ThreadId, userId);
                return existingUser.ThreadId;
            }

            _logger.LogInformation("Creating new OpenAI thread for user {UserId}", userId);
            
            // Create a new OpenAI thread
            var threadResult = await assistantClient.CreateThreadAsync();
            var newThreadId = threadResult.Value.Id;

            _logger.LogInformation("Created new OpenAI thread {ThreadId} for user {UserId}", newThreadId, userId);

            // Store or update the user with the new thread ID
            if (existingUser != null)
            {
                _logger.LogDebug("Updating existing user {UserId} with new thread ID {ThreadId}", userId, newThreadId);
                // Update existing user's thread ID
                existingUser.ThreadId = newThreadId;
                await _userStorageService.CreateOrUpdateUserAsync(existingUser);
            }
            else
            {
                _logger.LogInformation("Creating new user {UserId} with thread ID {ThreadId}", userId, newThreadId);
                // Create new user with thread ID
                var newUser = new User
                {
                    PhoneNumber = userId,
                    ThreadId = newThreadId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _userStorageService.CreateOrUpdateUserAsync(newUser);
            }

            return newThreadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or get thread for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> GetAssistantReplyAsync(string threadId, string userMessage)
    {
        return await GetAssistantReplyWithContextAsync(threadId, userMessage);
    }

    public async Task<string> GetAssistantReplyWithContextAsync(string threadId, string contextualMessage)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting OpenAI assistant interaction for thread {ThreadId}, message length: {MessageLength}", 
            threadId, contextualMessage?.Length ?? 0);
        
        try
        {
            // Add the contextual message to the thread
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _logger.LogDebug("Adding user message to thread {ThreadId}", threadId);
            await assistantClient.CreateMessageAsync(
                threadId,
                MessageRole.User,
                [MessageContent.FromText(contextualMessage)]
            );
            
            // Run the assistant on the thread
            _logger.LogDebug("Starting assistant run for thread {ThreadId} with assistant {AssistantId}", threadId, assistantId);
            var threadRun = await assistantClient.CreateRunAsync(threadId, assistantId);
            var runId = threadRun.Value.Id;
            
            _logger.LogInformation("Created OpenAI run {RunId} for thread {ThreadId}", runId, threadId);

            // Poll until the run is complete
            var pollCount = 0;
            while (!threadRun.Value.Status.IsTerminal)
            {
                pollCount++;
                await Task.Delay(TimeSpan.FromSeconds(1));
                threadRun = await assistantClient.GetRunAsync(threadRun.Value.ThreadId, threadRun.Value.Id);
                
                if (pollCount % 5 == 0) // Log every 5 seconds
                {
                    _logger.LogDebug("Run {RunId} status: {Status}, poll count: {PollCount}", 
                        runId, threadRun.Value.Status, pollCount);
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("OpenAI run {RunId} completed with status {Status} after {Duration}ms, {PollCount} polls", 
                runId, threadRun.Value.Status, (int)duration.TotalMilliseconds, pollCount);

            // Check if the run completed successfully
            if (threadRun.Value.Status == RunStatus.Failed)
            {
                var errorMessage = threadRun.Value.LastError?.Message ?? "Unknown error";
                _logger.LogError("OpenAI run {RunId} failed: {ErrorMessage}", runId, errorMessage);
                throw new InvalidOperationException($"Assistant run failed: {errorMessage}");
            }

            // Get the latest messages from the thread
            _logger.LogDebug("Retrieving messages from thread {ThreadId}", threadId);
            var messages = assistantClient.GetMessagesAsync(threadId);

            // Find the latest assistant message
            await foreach (var message in messages)
            {
                if (message.Role == MessageRole.Assistant)
                {
                    // Extract text content from the message
                    var textContent = message.Content.OfType<MessageContent>().FirstOrDefault();
                    if (textContent != null)
                    {
                        var reply = textContent.Text;
                        _logger.LogInformation("Retrieved assistant reply for thread {ThreadId}, reply length: {ReplyLength}", 
                            threadId, reply?.Length ?? 0);
                        return reply ?? "No text content found in response.";
                    }
                }
#pragma warning restore OPENAI001
            }

            _logger.LogWarning("No assistant response found in thread {ThreadId}", threadId);
            return "No response from assistant.";
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to get assistant reply for thread {ThreadId} after {Duration}ms", 
                threadId, (int)duration.TotalMilliseconds);
            throw new InvalidOperationException($"Failed to get assistant reply: {ex.Message}", ex);
        }
    }
}