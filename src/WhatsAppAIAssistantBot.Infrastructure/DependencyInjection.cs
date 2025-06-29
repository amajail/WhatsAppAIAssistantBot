using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Infrastructure.Data;
using WhatsAppAIAssistantBot.Infrastructure.Services;

namespace WhatsAppAIAssistantBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWhatsAppAIAssistantBotInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (connectionString?.Contains("Data Source") == true)
            {
                // SQLite connection
                options.UseSqlite(connectionString);
            }
            else
            {
                // SQL Server connection for Azure
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<ITwilioMessenger, TwilioMessenger>();
        services.AddScoped<IUserStorageService, UserStorageService>();

        return services;
    }
}
