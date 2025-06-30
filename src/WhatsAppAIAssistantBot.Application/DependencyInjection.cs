using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatsAppAIAssistantBot.Infrastructure;
using WhatsAppAIAssistantBot.Domain.Services;

namespace WhatsAppAIAssistantBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAIAssistantBotApplication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Register infrastructure services first
        services.AddWhatsAppAIAssistantBotInfrastructure(configuration, environment);
        
        // Register application/business logic services
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<IAssistantService, AssistantOpenAIService>();
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddScoped<IUserDataExtractionService, Services.UserDataExtractionService>();
        services.AddScoped<IUserContextService, Services.UserContextService>();

        return services;
    }
}
