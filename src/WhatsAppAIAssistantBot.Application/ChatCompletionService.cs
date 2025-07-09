using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace WhatsAppAIAssistantBot.Application;

public interface IChatCompletionService
{
    Task<string> GetCompletionAsync(string prompt);
}

public class ChatCompletionService : IChatCompletionService
{
    private readonly ILogger<ChatCompletionService> _logger;
    private readonly ChatClient _chatClient;

    public ChatCompletionService(IConfiguration configuration, ILogger<ChatCompletionService> logger)
    {
        _logger = logger;
        
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is not configured.");
        var openAIClient = new OpenAI.OpenAIClient(new ApiKeyCredential(apiKey));
        _chatClient = openAIClient.GetChatClient("gpt-4o-mini");
        
        _logger.LogInformation("ChatCompletionService initialized with gpt-4o-mini model");
    }

    public async Task<string> GetCompletionAsync(string prompt)
    {
        _logger.LogDebug("Getting chat completion for prompt length: {PromptLength}", prompt?.Length ?? 0);
        
        try
        {
            var response = await _chatClient.CompleteChatAsync(prompt);
            var completion = response.Value.Content[0].Text;
            
            _logger.LogInformation("Received chat completion, length: {ResponseLength}", completion?.Length ?? 0);
            return completion ?? "No completion response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat completion");
            throw new InvalidOperationException($"Failed to get chat completion: {ex.Message}", ex);
        }
    }
}