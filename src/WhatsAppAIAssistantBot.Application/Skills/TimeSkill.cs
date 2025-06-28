
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace WhatsAppAIAssistantBot.Application.Skills;

public class TimeSkill
{
    [KernelFunction, Description("Gets the current date and time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    [KernelFunction, Description("Gets the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    [KernelFunction, Description("Gets the current time")]
    public string GetTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }
}