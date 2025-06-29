# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 WhatsApp AI Assistant Bot built with ASP.NET Core Web API, Microsoft Semantic Kernel, and Twilio integration. The bot receives WhatsApp messages via Twilio webhooks and responds using OpenAI's Assistant API.

## Architecture

The solution follows Clean Architecture principles with these layers:

- **WhatsAppAIAssistantBot.Api**: ASP.NET Core Web API project containing controllers and startup configuration
- **WhatsAppAIAssistantBot.Application**: Business logic layer with orchestration, semantic kernel services, and AI assistant services
- **WhatsAppAIAssistantBot.Domain**: Domain entities and business rules
- **WhatsAppAIAssistantBot.Infrastructure**: External integrations (Twilio messaging)
- **WhatsAppAIAssistantBot.Models**: Shared data models (Twilio webhook models)

### Key Components

- **OrchestrationService** (`src/WhatsAppAIAssistantBot.Application/OrchestrationService.cs:10`): Main business logic that handles incoming messages and coordinates AI responses
- **WhatsAppController** (`src/WhatsAppAIAssistantBot.Api/Controllers/WhatsAppController.cs:9`): Receives Twilio webhook calls at `/api/whatsapp`
- **HealthController** (`src/WhatsAppAIAssistantBot.Api/Controllers/HealthController.cs:6`): Health check endpoints at `/api/health` and `/api/health/ready`
- **AssistantOpenAIService**: Manages OpenAI Assistant API interactions with thread management
- **TwilioMessenger**: Handles outbound WhatsApp message sending via Twilio
- **SemanticKernelService**: Provides local AI skills (currently has TimeSkill)

### Message Flow

1. Twilio sends webhook to `/api/whatsapp` endpoint
2. WhatsAppController receives TwilioWebhookModel and calls OrchestrationService
3. OrchestrationService creates/retrieves OpenAI thread for user and gets AI response
4. Response is sent back to user via TwilioMessenger

## Development Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run the API project
dotnet run --project src/WhatsAppAIAssistantBot.Api/WhatsAppAIAssistantBot.Api.csproj

# Build and run with Docker
docker build -t whatsapp-bot .
docker run -d -p 8080:8080 whatsapp-bot
```

## Configuration

Required environment variables/app settings:
- `OpenAI__ApiKey`: OpenAI API key
- `OpenAI__AssistantId`: OpenAI Assistant ID
- `Twilio__AccountSid`: Twilio account SID
- `Twilio__AuthToken`: Twilio auth token
- `Twilio__FromNumber`: Twilio WhatsApp number (format: whatsapp:+1234567890)
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection string (auto-configured in Azure)

Configuration is handled through `appsettings.json` and `appsettings.Development.json`.

## Deployment

The project includes Azure deployment infrastructure:
- `azure-deploy.bicep`: Bicep template for Azure resources (App Service, Application Insights)
- `deploy-to-azure.ps1`: PowerShell deployment script (idempotent)
- `.github/workflows/azure-deploy.yml`: GitHub Actions application deployment pipeline
- `.github/workflows/infrastructure-deploy.yml`: GitHub Actions infrastructure deployment pipeline
- `Dockerfile`: Container configuration for deployment

### Deployment Commands

```powershell
# Deploy infrastructure (idempotent)
.\deploy-to-azure.ps1 -ResourceGroupName "rg-whatsapp-bot-dev" -Location "East US2"

# Configure app settings via Azure CLI
az webapp config appsettings set \
  --resource-group "rg-whatsapp-bot-dev" \
  --name "your-app-service-name" \
  --settings \
    "OpenAI__ApiKey=your-key" \
    "OpenAI__AssistantId=your-id" \
    "Twilio__AccountSid=your-sid" \
    "Twilio__AuthToken=your-token" \
    "Twilio__FromNumber=whatsapp:+1234567890"
```

Current deployment:
- **Resource Group**: `rg-whatsapp-bot-dev`
- **App Service**: `whatsapp-ai-bot-6fc86fd7`
- **Application Insights**: `whatsapp-ai-bot-6fc86fd7-insights`
- **URL**: https://whatsapp-ai-bot-6fc86fd7.azurewebsites.net

## Testing

The solution includes a comprehensive test suite:
- **WhatsAppAIAssistantBot.Tests**: xUnit test project with Moq for mocking
- Tests cover OrchestrationService business logic
- GitHub Actions pipeline runs tests automatically on build

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal
```

## Dependencies

Key NuGet packages:
- Microsoft.ApplicationInsights.AspNetCore (2.22.0)
- Microsoft.SemanticKernel (1.55.0)
- Twilio (7.11.1)
- Swashbuckle.AspNetCore (8.1.4)

The API includes Swagger UI available at `/swagger` in development mode.

## Database

The application uses Entity Framework Core with a dual database strategy:

### Current Setup
- **Development**: SQLite database (`whatsapp_bot.db`)
- **Production Ready**: Azure SQL Database support
- **Auto-migration**: Database schema updates automatically on startup
- **User Storage**: Stores phone numbers, thread IDs, names, emails, and registration status

### User Registration Flow
- **New users**: Bot requests name first ("Name: John Doe" or "My name is John Doe")
- **Email collection**: After name, requests email ("Email: user@example.com")
- **Validation**: Basic email format validation using MailAddress
- **Persistence**: User data stored across application restarts

### Database Migration Path

#### When to Upgrade from SQLite to Azure SQL Database

**🟢 Start with SQLite when:**
- Testing/prototyping your bot
- < 50 active users
- Simple WhatsApp conversations
- Want to keep costs low initially

**🟡 Consider upgrading when you hit:**
- **Performance Issues**: App becomes slow responding to messages, database operations take > 1 second
- **Scale Indicators**: 100+ active users, 1000+ messages per day, multiple app instances needed
- **Business Growth**: Bot becomes business-critical, need backups and disaster recovery

**🔴 Must upgrade when:**
- **File size > 100MB** (SQLite performance degrades)
- **Concurrent connection errors** (SQLite locks)
- **Data loss occurs** during Azure deployments
- Need **multi-region deployment**

#### Monitoring for Upgrade Signals
1. **Database file size**: Check `whatsapp_bot.db` file size
2. **Response times**: Monitor via Application Insights
3. **User count**: Track registered users in your database
4. **Message volume**: Daily message counts

#### Easy Migration to Azure SQL
When ready to upgrade:
1. Create Azure SQL Database resources
2. Update connection string in Azure App Settings to SQL Server format
3. Deploy - auto-migration will handle schema creation
4. Verify data migrated correctly

The current setup supports both databases seamlessly - just change the connection string!

### Database Commands

```bash
# Create new migration
dotnet ef migrations add MigrationName --project src/WhatsAppAIAssistantBot.Infrastructure --startup-project src/WhatsAppAIAssistantBot.Api

# Update database manually (optional - auto-migration handles this)
dotnet ef database update --project src/WhatsAppAIAssistantBot.Infrastructure --startup-project src/WhatsAppAIAssistantBot.Api

# Remove last migration
dotnet ef migrations remove --project src/WhatsAppAIAssistantBot.Infrastructure --startup-project src/WhatsAppAIAssistantBot.Api
```

## Monitoring

Application Insights is integrated for telemetry and monitoring:
- **Automatic telemetry collection** for requests, dependencies, and exceptions
- **Connection string** auto-configured in Azure deployment
- **Log Analytics workspace** created automatically
- **Dashboards and alerts** available in Azure Portal

## Health Checks

The API includes health check endpoints for monitoring:
- **`/api/health`**: Basic health check returning status, timestamp, and version
- **`/api/health/ready`**: Readiness probe for deployment verification
- **Automated testing**: GitHub Actions pipeline includes health checks after deployment