namespace WhatsAppAIAssistantBot.Application;

using Microsoft.SemanticKernel;
using WhatsAppAIAssistantBot.Application.Skills;
using Microsoft.Extensions.Configuration;

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;

    public SemanticKernelService(IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();
        var openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key not configured");
        builder.AddOpenAIChatCompletion("gpt-4", openAiApiKey);
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


