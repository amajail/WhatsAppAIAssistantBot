namespace WhatsAppAIAssistantBot.Services;
public interface IOrchestrationService
{
    Task<string> HandleMessageAsync(string userId, string message);
}

public class OrchestrationService(ISemanticKernelService sk, IAssistantApiService assistant) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantApiService _assistant = assistant;
    private static Dictionary<string, string> _userThreads = new();

    public async Task<string> HandleMessageAsync(string userId, string message)
    {
        // if (message.ToLower().Contains("time"))
        // {
        //     return await _sk.RunLocalSkillAsync(message);
        // }

        // if (!_userThreads.ContainsKey(userId))
        //     _userThreads[userId] = await _assistant.CreateOrGetThreadAsync(userId);

        // return await _assistant.GetAssistantReplyAsync(_userThreads[userId], message);

        return await Task.FromResult("This is a placeholder response. Implement your logic here.");
    }
}