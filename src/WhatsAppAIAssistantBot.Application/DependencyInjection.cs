using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAIAssistantBotApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register infrastructure services first
        services.AddWhatsAppAIAssistantBotInfrastructure(configuration);
        
        // Register application/business logic services
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<IAssistantService, AssistantOpenAIService>();
        services.AddScoped<IOrchestrationService, OrchestrationService>();

        return services;
    }
}
