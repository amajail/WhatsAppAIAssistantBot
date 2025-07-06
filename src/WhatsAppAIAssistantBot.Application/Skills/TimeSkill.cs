namespace WhatsAppAIAssistantBot.Application.Skills;

public class TimeSkill
{
    [Microsoft.SemanticKernel.KernelFunction, System.ComponentModel.Description("Gets the current date and time")]
    public string GetCurrentTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    [Microsoft.SemanticKernel.KernelFunction, System.ComponentModel.Description("Gets the current date")]
    public string GetCurrentDate() => DateTime.Now.ToString("yyyy-MM-dd");

    [Microsoft.SemanticKernel.KernelFunction, System.ComponentModel.Description("Gets the current time")]
    public string GetTime() => DateTime.Now.ToString("HH:mm:ss");
}