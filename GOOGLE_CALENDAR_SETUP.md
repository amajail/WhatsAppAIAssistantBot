# Google Calendar Integration Setup

This guide explains how to configure Google Calendar credentials for the WhatsApp AI Assistant Bot.

## 🔐 Credentials Configuration

The bot supports two methods for Google Calendar authentication:

### Method 1: Environment Variable (Recommended for Production)
Set the credentials JSON content as an environment variable:

```bash
# Set environment variable with JSON content
export GoogleCalendar__ServiceAccountCredentialsJson='{"type":"service_account","project_id":"your-project","private_key_id":"key-id","private_key":"-----BEGIN PRIVATE KEY-----\n...","client_email":"service-account@project.iam.gserviceaccount.com","client_id":"123456789","auth_uri":"https://accounts.google.com/o/oauth2/auth","token_uri":"https://oauth2.googleapis.com/token","auth_provider_x509_cert_url":"https://www.googleapis.com/oauth2/v1/certs","client_x509_cert_url":"https://www.googleapis.com/robot/v1/metadata/x509/service-account%40project.iam.gserviceaccount.com"}'
```

### Method 2: Local File (Development Only)
Place your credentials file in the API project directory:

```bash
# Copy your credentials file to the API project
cp your-credentials.json src/WhatsAppAIAssistantBot.Api/optimum-reactor-465413-n3-2fc2437c0785.json
```

## 🔧 Development Setup

### Local Development
1. Place your credentials file in the API directory
2. Update `appsettings.Development.json`:
```json
{
  "GoogleCalendar": {
    "ServiceAccountCredentialsPath": "optimum-reactor-465413-n3-2fc2437c0785.json"
  }
}
```

### Production Deployment
1. Set the environment variable:
```bash
# Azure App Service
az webapp config appsettings set \
  --resource-group "rg-whatsapp-bot-dev" \
  --name "your-app-name" \
  --settings "GoogleCalendar__ServiceAccountCredentialsJson=<your-json-content>"
```

## 🚀 Azure Deployment

### Option A: Azure App Service Configuration
```bash
# Set app settings via Azure CLI
az webapp config appsettings set \
  --resource-group "rg-whatsapp-bot-dev" \
  --name "whatsapp-ai-bot-6fc86fd7" \
  --settings \
    "GoogleCalendar__ServiceAccountCredentialsJson=<your-credentials-json>" \
    "GoogleCalendar__CalendarId=primary"
```

### Option B: Azure Key Vault (Recommended for Production)
```bash
# Store credentials in Key Vault
az keyvault secret set \
  --vault-name "your-keyvault" \
  --name "google-calendar-credentials" \
  --value '<your-credentials-json>'

# Reference in App Service
az webapp config appsettings set \
  --resource-group "rg-whatsapp-bot-dev" \
  --name "whatsapp-ai-bot-6fc86fd7" \
  --settings "GoogleCalendar__ServiceAccountCredentialsJson=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/google-calendar-credentials/)"
```

## 📋 Configuration Settings

All Google Calendar settings in `appsettings.json`:

```json
{
  "GoogleCalendar": {
    "CalendarId": "primary",
    "ServiceAccountCredentialsPath": "",
    "ServiceAccountCredentialsJson": "",
    "TimeZone": "America/Argentina/Buenos_Aires",
    "BusinessHours": {
      "StartHour": 9,
      "EndHour": 18
    },
    "DefaultSlotDurationMinutes": 30
  }
}
```

### Environment Variables
- `GoogleCalendar__ServiceAccountCredentialsJson`: JSON credentials content
- `GoogleCalendar__ServiceAccountCredentialsPath`: Path to credentials file
- `GoogleCalendar__CalendarId`: Google Calendar ID (default: "primary")
- `GoogleCalendar__TimeZone`: Timezone for appointments
- `GoogleCalendar__BusinessHours__StartHour`: Business start hour (24h format)
- `GoogleCalendar__BusinessHours__EndHour`: Business end hour (24h format)

## 🔒 Security Best Practices

1. **Never commit credentials to git**
2. **Use environment variables in production**
3. **Consider Azure Key Vault for sensitive data**
4. **Rotate credentials regularly**
5. **Use least privilege service account permissions**

## 🧪 Testing

Run tests to verify configuration:
```bash
dotnet test
```

## 🎯 WhatsApp Commands

Users can interact with the calendar via these commands:
- `/slots` or `/disponibilidad` - View available time slots
- `/book` or `/reservar` - Get booking instructions

## 📞 Support

The system supports:
- ✅ Business hours filtering
- ✅ Weekend exclusion
- ✅ Conflict detection
- ✅ Multi-language support (English/Spanish)
- ✅ Timezone handling
- ✅ Environment variable configuration