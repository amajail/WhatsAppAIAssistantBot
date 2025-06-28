using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application;

public interface IOrchestrationService
{
    Task HandleMessageAsync(string userId, string message);
}

public class OrchestrationService(ISemanticKernelService sk,
                                  IAssistantService assistant,
                                  ITwilioMessenger twilioMessenger) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;

    public async Task HandleMessageAsync(string userId, string message)
    {
        // if (message.ToLower().Contains("time"))
        // {
        //     return await _sk.RunLocalSkillAsync(message);
        // }

        var threadId = await _assistant.CreateOrGetThreadAsync(userId);
        var reply = await _assistant.GetAssistantReplyAsync(threadId, message);

        await twilioMessenger.SendMessageAsync(userId, reply);
    }
}
