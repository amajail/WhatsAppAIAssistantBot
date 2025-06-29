using System.ClientModel;
using Microsoft.Extensions.Configuration;
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
    private readonly string apiKey;
    private readonly string assistantId;
    private readonly OpenAI.OpenAIClient openAIClient;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly AssistantClient assistantClient;
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    // Suppress warning for evaluation-only API usage
#pragma warning disable OPENAI001
    public AssistantOpenAIService(IConfiguration configuration, IUserStorageService userStorageService)
    {
        this.configuration = configuration;
        _userStorageService = userStorageService;
        apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is not configured.");
        assistantId = configuration["OpenAI:AssistantId"] ?? throw new InvalidOperationException("OpenAI Assistant ID is not configured.");
        openAIClient = new OpenAI.OpenAIClient(new ApiKeyCredential(apiKey));
        assistantClient = openAIClient.GetAssistantClient();
    }
#pragma warning restore OPENAI001

    // Create the thread for the user if it doesn't exist
    // and return the thread ID.
    public async Task<string> CreateOrGetThreadAsync(string userId)
    {
        // Check if user already exists in database with a thread
        var existingUser = await _userStorageService.GetUserByPhoneNumberAsync(userId);
        if (existingUser != null && !string.IsNullOrEmpty(existingUser.ThreadId))
        {
            return existingUser.ThreadId;
        }

        // Create a new OpenAI thread
        var threadResult = await assistantClient.CreateThreadAsync();
        var newThreadId = threadResult.Value.Id;

        // Store or update the user with the new thread ID
        if (existingUser != null)
        {
            // Update existing user's thread ID
            existingUser.ThreadId = newThreadId;
            await _userStorageService.CreateOrUpdateUserAsync(existingUser);
        }
        else
        {
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

    public async Task<string> GetAssistantReplyAsync(string threadId, string userMessage)
    {
        return await GetAssistantReplyWithContextAsync(threadId, userMessage);
    }

    public async Task<string> GetAssistantReplyWithContextAsync(string threadId, string contextualMessage)
    {
        try
        {
            // Add the contextual message to the thread
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            await assistantClient.CreateMessageAsync(
                threadId,
                MessageRole.User,
                [MessageContent.FromText(contextualMessage)]
            );
            // Run the assistant on the thread
            var threadRun = await assistantClient.CreateRunAsync(threadId, assistantId);

            // Poll until the run is complete
            while (!threadRun.Value.Status.IsTerminal)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // Fixed: Use Task.Delay instead of Thread.Sleep
                threadRun = await assistantClient.GetRunAsync(threadRun.Value.ThreadId, threadRun.Value.Id);
            }

            // Check if the run completed successfully
            if (threadRun.Value.Status == RunStatus.Failed)
            {
                throw new InvalidOperationException($"Assistant run failed: {threadRun.Value.LastError?.Message}");
            }

            // Get the latest messages from the thread
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
                        return textContent.Text;
                    }
                }
#pragma warning restore OPENAI001
            }

            return "No response from assistant.";
        }
        catch (Exception ex)
        {
            // Added proper error handling
            throw new InvalidOperationException($"Failed to get assistant reply: {ex.Message}", ex);
        }
    }
}