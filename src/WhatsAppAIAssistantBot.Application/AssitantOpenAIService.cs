using System.ClientModel;
using System.Collections.Concurrent; // Added missing import
using Microsoft.Extensions.Configuration;
using OpenAI.Assistants;

namespace WhatsAppAIAssistantBot.Application;

public interface IAssistantService
{
    Task<string> CreateOrGetThreadAsync(string userId);
    Task<string> GetAssistantReplyAsync(string threadId, string userMessage);
}

public class AssistantOpenAIService : IAssistantService
{
    private readonly IConfiguration configuration;
    private readonly string apiKey;
    private readonly string assistantId;
    private readonly OpenAI.OpenAIClient openAIClient;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly AssistantClient assistantClient;
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ConcurrentDictionary<string, string> _userThreads = new();

    // Suppress warning for evaluation-only API usage
#pragma warning disable OPENAI001
    public AssistantOpenAIService(IConfiguration configuration)
    {
        this.configuration = configuration;
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
        // If the thread already exists for this user, return it
        if (_userThreads.TryGetValue(userId, out var threadId))
        {
            return await Task.FromResult(threadId);
        }

        // Create a new thread - Fixed: added await
        var threadResult = await assistantClient.CreateThreadAsync();
        var newThreadId = threadResult.Value.Id;

        // Store the mapping
        _userThreads[userId] = newThreadId;

        return newThreadId;
    }

    public async Task<string> GetAssistantReplyAsync(string threadId, string userMessage)
    {
        try
        {
            // Removed duplicate client creation - using class fields instead

            // Add the user message to the thread
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            await assistantClient.CreateMessageAsync(
                threadId,
                MessageRole.User,
                [MessageContent.FromText(userMessage)]
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