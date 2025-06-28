using Microsoft.Extensions.DependencyInjection;
using WhatsAppAIAssistantBot.Infrastructure;

namespace WhatsAppAIAssistantBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAIAssistantBotApplication(this IServiceCollection services)
    {
        // Register application/business logic services here
        // Example: services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<IAssistantService, AssistantOpenAIService>();
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddWhatsAppAIAssistantBotInfrastructure();


        return services;
    }
}
