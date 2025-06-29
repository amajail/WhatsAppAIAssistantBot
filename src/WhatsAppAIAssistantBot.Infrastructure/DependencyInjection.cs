using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Repositories;
using WhatsAppAIAssistantBot.Infrastructure.Data;
using WhatsAppAIAssistantBot.Infrastructure.Services;
using WhatsAppAIAssistantBot.Infrastructure.Repositories;

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

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Register services
        services.AddScoped<ITwilioMessenger, TwilioMessenger>();
        services.AddScoped<IUserStorageService, UserStorageService>();

        return services;
    }
}
