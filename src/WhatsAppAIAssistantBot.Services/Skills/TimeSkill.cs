using Microsoft.SemanticKernel;

namespace WhatsAppAIAssistantBot.Services.Skills;

public class TimeSkill
{
    [KernelFunction]
    public string GetTime(string input) => $"🕒 Current time: {DateTime.UtcNow}";
}