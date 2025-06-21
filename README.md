# WhatsApp AI Assistant Bot

This project is a .NET 8 solution for a WhatsApp AI Assistant Bot, using ASP.NET Core Web API, Semantic Kernel, and Twilio integration.

## Project Structure

```
WhatsAppAIAssistantBot/
│
├── src/
│   ├── WhatsAppAIAssistantBot.api/         # ASP.NET Core Web API project
│   ├── WhatsAppAIAssistantBot.services/    # Business logic, Semantic Kernel, Twilio, etc.
│   └── WhatsAppAIAssistantBot.models/      # Shared models
├── WhatsAppAIAssistantBot.sln              # Solution file
├── .gitignore
└── README.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- A Twilio account and credentials
- An OpenAI API key

## Getting Started

1. **Clone the repository:**
   ```sh
   git clone https://github.com/your-username/WhatsAppAIAssistantBot.git
   cd WhatsAppAIAssistantBot
   ```

2. **Restore dependencies:**
   ```sh
   dotnet restore
   ```

3. **Configure secrets:**
   - Set your OpenAI and Twilio credentials in `appsettings.Development.json` or as environment variables.

4. **Build and run the API:**
   ```sh
   dotnet run --project src/WhatsAppAIAssistantBot.api/WhatsAppAIAssistantBot.api.csproj
   ```

5. **Test the API:**
   - Visit [https://localhost:5001/swagger](https://localhost:5001/swagger) for Swagger UI and API documentation.

## Features

- WhatsApp webhook endpoint for receiving and replying to messages
- AI-powered responses using OpenAI via Semantic Kernel
- Twilio integration for WhatsApp messaging
- Extensible skill/plugin system

## License

MIT License

---

**Replace placeholder values and instructions with your actual configuration as