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

Configuration is handled through `appsettings.json` and `appsettings.Development.json`.

## Deployment

The project includes Azure deployment infrastructure:
- `azure-deploy.bicep`: Bicep template for Azure resources
- `deploy-to-azure.ps1`: PowerShell deployment script
- `.github/workflows/azure-deploy.yml`: GitHub Actions CI/CD pipeline
- `Dockerfile`: Container configuration for deployment

## Testing

No test projects are currently configured in the solution. Tests should be added following .NET testing conventions with xUnit or NUnit.

## Dependencies

Key NuGet packages:
- Microsoft.SemanticKernel (1.55.0)
- Twilio (7.11.1)
- Swashbuckle.AspNetCore (8.1.4)

The API includes Swagger UI available at `/swagger` in development mode.