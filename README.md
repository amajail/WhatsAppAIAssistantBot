# WhatsApp AI Assistant Bot

A production-ready .NET 8 WhatsApp AI Assistant Bot built with Clean Architecture, featuring intelligent conversation management, multi-language support, and seamless Azure deployment.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/your-username/WhatsAppAIAssistantBot)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 🚀 Features

### 🤖 AI-Powered Conversations
- **OpenAI Integration**: Advanced conversations using OpenAI's Assistant API
- **Context Awareness**: Persistent conversation threads for each user
- **Intelligent Responses**: Context-sensitive AI replies based on user information

### 🌍 Multi-Language Support
- **Bilingual**: English and Spanish language support
- **Dynamic Switching**: Users can change language with `/lang en` or `/idioma es`
- **Localized Content**: All messages, prompts, and responses localized

### 👤 Smart User Management
- **Progressive Registration**: Natural collection of user name and email
- **Data Extraction**: AI-powered extraction from natural language inputs
- **Persistent Storage**: User data maintained across sessions with EF Core

### 🎯 Command System
- **Bot Commands**: `/help`, `/lang [code]`, `/idioma [code]`
- **Multi-format Support**: Flexible command parsing and validation
- **Contextual Help**: Language-specific help and guidance

### 🏗️ Enterprise Architecture
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Comprehensive Testing**: 90+ unit tests with high coverage
- **XML Documentation**: Complete API documentation with IntelliSense
- **Azure Ready**: Production deployment configuration included

## 📁 Project Structure

```
WhatsAppAIAssistantBot/
├── src/
│   ├── WhatsAppAIAssistantBot.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/                         # API endpoints
│   │   │   ├── WhatsAppController.cs           # Main webhook endpoint
│   │   │   └── HealthController.cs             # Health checks
│   │   └── Program.cs                          # Application startup
│   │
│   ├── WhatsAppAIAssistantBot.Application/      # Business Logic Layer
│   │   ├── Services/                           # Application services
│   │   │   ├── CommandHandlerService.cs        # Bot command processing
│   │   │   ├── UserRegistrationService.cs      # User registration flow
│   │   │   ├── UserDataExtractionService.cs    # AI data extraction
│   │   │   └── UserContextService.cs           # Context management
│   │   ├── OrchestrationService.cs             # Main workflow orchestration
│   │   └── AssistantOpenAIService.cs           # OpenAI integration
│   │
│   ├── WhatsAppAIAssistantBot.Domain/           # Domain Layer
│   │   ├── Entities/                           # Domain entities
│   │   │   └── User.cs                         # User entity
│   │   ├── Services/                           # Service contracts
│   │   └── Models/                             # Domain models
│   │
│   ├── WhatsAppAIAssistantBot.Infrastructure/   # Infrastructure Layer
│   │   ├── Services/                           # External integrations
│   │   │   └── LocalizationService.cs          # Multi-language support
│   │   ├── Data/                               # Data access
│   │   │   └── ApplicationDbContext.cs         # EF Core context
│   │   └── TwilioMessenger.cs                  # WhatsApp messaging
│   │
│   ├── WhatsAppAIAssistantBot.Models/           # Shared Models
│   │   └── TwilioWebhookModel.cs               # Twilio webhook models
│   │
│   └── WhatsAppAIAssistantBot.Tests/            # Unit Tests
│       ├── OrchestrationServiceTests.cs        # Main orchestration tests
│       ├── CommandHandlerServiceTests.cs       # Command processing tests
│       ├── UserRegistrationServiceTests.cs     # Registration flow tests
│       └── TwilioMessengerTests.cs             # Messaging tests
│
├── azure-deploy.bicep                          # Azure infrastructure
├── deploy-to-azure.ps1                         # Deployment script
├── Dockerfile                                  # Container configuration
├── .github/workflows/                          # CI/CD pipelines
└── docs/                                       # Documentation
```

## 🛠️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)
- **Twilio Account**: WhatsApp Business API access
- **OpenAI Account**: API key and Assistant ID

## 🚀 Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/your-username/WhatsAppAIAssistantBot.git
cd WhatsAppAIAssistantBot
```

### 2. Configuration

Create `appsettings.Development.json`:
```json
{
  \"OpenAI\": {
    \"ApiKey\": \"your-openai-api-key\",
    \"AssistantId\": \"your-assistant-id\"
  },
  \"Twilio\": {
    \"AccountSid\": \"your-twilio-account-sid\",
    \"AuthToken\": \"your-twilio-auth-token\",
    \"FromNumber\": \"whatsapp:+your-twilio-number\"
  }
}
```

### 3. Build and Run
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run the API
dotnet run --project src/WhatsAppAIAssistantBot.Api

# Or use Docker
docker build -t whatsapp-bot .
docker run -p 8080:8080 whatsapp-bot
```

### 4. Test the API
Visit `https://localhost:5001/swagger` for API documentation and testing.

## 📱 WhatsApp Setup

### Configure Twilio Webhook
Set your Twilio webhook URL to:
```
https://your-domain.com/api/whatsapp
```

### Test Your Bot
Send a WhatsApp message to your Twilio number:
- **\"Hi\"** - Start conversation and registration
- **\"/help\"** - Get help information
- **\"/lang en\"** - Switch to English
- **\"Name: John Doe\"** - Provide your name
- **\"Email: john@example.com\"** - Complete registration

## 🧪 Testing

Run the comprehensive test suite:
```bash
# Run all 90 tests
dotnet test

# Run with coverage
dotnet test --collect:\"XPlat Code Coverage\"

# Run specific test category
dotnet test --filter \"Category=Integration\"
```

## 🌐 Deployment

### Azure Deployment (Recommended)
```powershell
# Deploy infrastructure
.\\deploy-to-azure.ps1 -ResourceGroupName \"rg-whatsapp-bot\" -Location \"East US\"
```

### GitHub Actions
Automated CI/CD pipeline included:
- ✅ Build and test on every push
- ✅ Deploy to Azure on main branch
- ✅ Health checks after deployment

## 🔧 Development

### Adding New Commands
1. Add command handling to `CommandHandlerService.cs`
2. Add localized messages to resource files
3. Write unit tests for the new command
4. Update documentation

### Adding New Languages
1. Create new resource file (e.g., `fr.json`)
2. Add language to `SupportedLanguage` enum
3. Update localization service configuration
4. Test language switching functionality

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/WhatsAppAIAssistantBot.Infrastructure --startup-project src/WhatsAppAIAssistantBot.Api

# Update database
dotnet ef database update --project src/WhatsAppAIAssistantBot.Infrastructure --startup-project src/WhatsAppAIAssistantBot.Api
```

## 📊 Monitoring

### Application Insights
- **Telemetry**: Automatic request/dependency tracking
- **Custom Events**: User registration, message processing
- **Performance**: Response times and failure rates
- **Dashboards**: Real-time monitoring in Azure Portal

### Health Checks
- **`/api/health`**: Basic health status
- **`/api/health/ready`**: Readiness for traffic

## 🔒 Security

- **Input Validation**: Comprehensive message validation
- **Rate Limiting**: Protection against spam/abuse
- **Secure Configuration**: Environment-based secrets
- **HTTPS Only**: Enforced secure communication

## 📚 Documentation

- **[Developer Guide](docs/DEVELOPER-GUIDE.md)**: Comprehensive development documentation
- **[Deployment Guide](README-Deployment.md)**: Detailed deployment instructions
- **[API Documentation](https://your-domain.com/swagger)**: Interactive API docs
- **[Architecture Overview](CLAUDE.md)**: Technical architecture details

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: Check the [Developer Guide](docs/DEVELOPER-GUIDE.md)
- **Issues**: Report bugs via [GitHub Issues](https://github.com/your-username/WhatsAppAIAssistantBot/issues)
- **Discussions**: Join the [GitHub Discussions](https://github.com/your-username/WhatsAppAIAssistantBot/discussions)

---

**Built with ❤️ using .NET 8, OpenAI, and Twilio**