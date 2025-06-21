namespace WhatsAppAIAssistantBot.Services;

using Microsoft.SemanticKernel;
using WhatsAppAIAssistantBot.Services.Skills;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;

    public SemanticKernelService()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4", "OPENAI_API_KEY");
        _kernel = builder.Build();

   _kernel.Plugins.AddFromObject(new TimeSkill(), "time");
    }

public async Task<string> RunLocalSkillAsync(string input)
{
    // Get the plugin by name
    var plugin = _kernel.Plugins["time"];
    // Get the function by name
    var func = plugin["GetTime"];
    // Wrap input in KernelArguments
    var args = new KernelArguments { ["input"] = input };
    // Call the function
    var result = await func.InvokeAsync(_kernel, args);
    return result.ToString();
}
}

public interface ISemanticKernelService
{
    Task<string> RunLocalSkillAsync(string input);
}