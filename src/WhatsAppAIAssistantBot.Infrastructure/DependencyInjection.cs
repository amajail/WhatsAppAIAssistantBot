using Microsoft.Extensions.DependencyInjection;

namespace WhatsAppAIAssistantBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAIAssistantBotInfrastructure(this IServiceCollection services)
    {
        // services.AddRedisServices(); // Uncomment and implement AddRedisServices if needed
        // Add other services as needed
        services.AddScoped<ITwilioMessenger, TwilioMessenger>();
        
        //config redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379"; // Update with your Redis configuration
        });

        return services;
    }
}
