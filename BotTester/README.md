# Bot Tester Console App

A console application for testing the WhatsApp AI Assistant Bot during development.

## Features

- **Interactive Testing**: Send messages to the bot via command line
- **Webhook Receiver**: Receives bot responses via HTTP webhook
- **Real-time Responses**: See bot responses immediately in the console
- **Command Support**: Test all bot commands (/slots, /book, /lang, /help)
- **Easy Setup**: Pre-configured for local development

## How It Works

1. **Console App** → Sends HTTP POST to bot API endpoint (`/api/whatsapp`)
2. **Bot API** → Processes message and generates response
3. **MockTwilioMessenger** → Sends response to console app webhook (`/mock-twilio`)
4. **Console App** → Displays bot response in real-time

## Usage

### 1. Start the Bot API
```bash
# From the main solution directory
dotnet run --project src/WhatsAppAIAssistantBot.Api/WhatsAppAIAssistantBot.Api.csproj
```

### 2. Start the Console App
```bash
# From the BotTester directory
dotnet run
```

### 3. Send Messages
```
💬 Enter message: Hello
📤 Sending to bot: Hello
✅ Message sent successfully
⏳ Waiting for bot response...

🤖 Bot Response:
From: whatsapp:+15551234567
To: whatsapp:+1234567890
Message: Hello! I'm your WhatsApp AI Assistant. How can I help you today?
MessageSid: SM1234567890abcdef1234567890abcdef
--------------------------------------------------
```

## Available Commands

### Console Commands
- `quit` or `exit` - Close the application
- `clear` - Clear the console
- `help` - Show available bot commands

### Bot Commands
- `/slots` - Get available calendar slots
- `/book <datetime>` - Book an appointment
- `/lang <en|es>` - Change language
- `/help` - Get bot help
- `/ayuda` - Spanish help
- `/idioma <en|es>` - Spanish language change

## Configuration

The app uses `appsettings.json` for configuration:

```json
{
  "BotApi": {
    "BaseUrl": "http://localhost:5000",
    "WhatsAppEndpoint": "/api/whatsapp"
  },
  "Webhook": {
    "Port": 5001,
    "Path": "/mock-twilio"
  },
  "TestUser": {
    "PhoneNumber": "whatsapp:+1234567890",
    "BotNumber": "whatsapp:+15551234567"
  }
}
```

## Requirements

- .NET 8.0
- Bot API running on `http://localhost:5000`
- Available port `5001` for webhook server

## Architecture

```
┌─────────────────┐    HTTP POST     ┌─────────────────┐
│   Console App   │ ───────────────→ │    Bot API      │
│   (BotTester)   │                  │  (localhost:5000)│
└─────────────────┘                  └─────────────────┘
         ▲                                    │
         │                                    │
         │        HTTP POST                   │
         │   (/mock-twilio)                   ▼
         │                           ┌─────────────────┐
         └─────────────────────────── │ MockTwilioMessenger │
                                     │  (Webhook Client)   │
                                     └─────────────────┘
```

## Example Session

```
🚀 WhatsApp Bot Tester Console
===============================
Webhook server running on: http://localhost:5001/mock-twilio
Bot API URL: http://localhost:5000/api/whatsapp
Test Phone: whatsapp:+1234567890
Bot Phone: whatsapp:+15551234567

💬 Enter message: /slots
📤 Sending to bot: /slots
✅ Message sent successfully
⏳ Waiting for bot response...

🤖 Bot Response:
From: whatsapp:+15551234567
To: whatsapp:+1234567890
Message: Available time slots for today:
1. 2024-01-15 14:00:00 - 2024-01-15 14:30:00
2. 2024-01-15 15:00:00 - 2024-01-15 15:30:00
3. 2024-01-15 16:00:00 - 2024-01-15 16:30:00
MessageSid: SM1234567890abcdef1234567890abcdef
--------------------------------------------------

💬 Enter message: /book 2024-01-15T14:00:00
📤 Sending to bot: /book 2024-01-15T14:00:00
✅ Message sent successfully
⏳ Waiting for bot response...

🤖 Bot Response:
From: whatsapp:+15551234567
To: whatsapp:+1234567890
Message: ✅ Appointment booked successfully for 2024-01-15 14:00:00
MessageSid: SM1234567890abcdef1234567890abcdef
--------------------------------------------------

💬 Enter message: quit
👋 Goodbye!
```