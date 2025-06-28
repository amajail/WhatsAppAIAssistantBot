# Azure Deployment Guide

## Prerequisites

1. **Azure CLI** installed and configured
2. **Azure subscription** with appropriate permissions
3. **GitHub repository** (for CI/CD)
4. **OpenAI API key** and Assistant ID
5. **Twilio account** with WhatsApp sandbox/number

## Deployment Options

### Option 1: Using PowerShell Script (Recommended)

```powershell
# Run the deployment script
.\deploy-to-azure.ps1 -ResourceGroupName "rg-whatsapp-bot" -Location "East US"
```

### Option 2: Manual Azure CLI Deployment

```bash
# Create resource group
az group create --name rg-whatsapp-bot --location "East US"

# Deploy Bicep template
az deployment group create \
  --resource-group rg-whatsapp-bot \
  --template-file azure-deploy.bicep \
  --parameters appServiceName=your-unique-app-name
```

### Option 3: GitHub Actions CI/CD

1. **Set up secrets** in your GitHub repository:
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: Download from Azure Portal
   
2. **Update workflow file** (`.github/workflows/azure-deploy.yml`):
   - Change `AZURE_WEBAPP_NAME` to your app service name

3. **Push to main branch** to trigger deployment

## Configuration

### Required App Settings in Azure

Configure these in Azure Portal > App Service > Configuration:

```
OpenAI__ApiKey = your_openai_api_key
OpenAI__AssistantId = your_assistant_id
Twilio__AccountSid = your_twilio_account_sid
Twilio__AuthToken = your_twilio_auth_token
Twilio__FromNumber = whatsapp:+1234567890
```

### Twilio Webhook Configuration

Set your Twilio webhook URL to:
```
https://your-app-name.azurewebsites.net/api/whatsapp
```

## Local Testing with Docker

```bash
# Build Docker image
docker build -t whatsapp-bot .

# Run container
docker run -d -p 8080:8080 \
  -e OpenAI__ApiKey=your_key \
  -e OpenAI__AssistantId=your_id \
  -e Twilio__AccountSid=your_sid \
  -e Twilio__AuthToken=your_token \
  -e Twilio__FromNumber=whatsapp:+1234567890 \
  whatsapp-bot
```

## Troubleshooting

1. **Check Application Logs**: Azure Portal > App Service > Log stream
2. **Verify Configuration**: Ensure all app settings are properly set
3. **Test Endpoint**: Use Postman to test `/api/whatsapp` endpoint
4. **Monitor Costs**: Keep an eye on Azure billing

## Security Considerations

- Use Azure Key Vault for sensitive configuration
- Enable HTTPS only
- Configure proper CORS settings
- Monitor application insights for errors