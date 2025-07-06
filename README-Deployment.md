# WhatsApp AI Assistant Bot - Deployment Guide

Complete deployment guide for production-ready WhatsApp AI Assistant Bot deployment to Azure with comprehensive monitoring and scaling options.

## 📋 Prerequisites

### Required Accounts & Services
1. **Azure Subscription** with contributor permissions
2. **Azure CLI** installed and authenticated (`az login`)
3. **OpenAI Account** with API key and Assistant ID
4. **Twilio Account** with WhatsApp Business API access
5. **GitHub Repository** (for automated CI/CD)

### Development Tools
- **PowerShell** 5.1+ or PowerShell Core 7+ (for deployment scripts)
- **Docker Desktop** (for containerized testing)
- **Git** for version control
- **.NET 8 SDK** for local development and testing

## 🚀 Deployment Options

### Option 1: Automated PowerShell Deployment (Recommended)

The fastest way to deploy with infrastructure provisioning:

```powershell
# Clone and navigate to repository
git clone https://github.com/your-username/WhatsAppAIAssistantBot.git
cd WhatsAppAIAssistantBot

# Run automated deployment
.\deploy-to-azure.ps1 -ResourceGroupName "rg-whatsapp-bot-prod" -Location "East US 2"
```

**What this does:**
- ✅ Creates Azure Resource Group
- ✅ Deploys App Service + Application Insights
- ✅ Configures auto-scaling and health checks
- ✅ Sets up monitoring and alerting
- ✅ Deploys application code
- ✅ Configures SSL and custom domains

### Option 2: Manual Azure CLI Deployment

For more control over the deployment process:

```bash
# 1. Create resource group
az group create --name rg-whatsapp-bot --location "East US 2"

# 2. Deploy infrastructure using Bicep
az deployment group create \
  --resource-group rg-whatsapp-bot \
  --template-file azure-deploy.bicep \
  --parameters appServiceName=whatsapp-bot-$(date +%s)

# 3. Get deployment outputs
az deployment group show \
  --resource-group rg-whatsapp-bot \
  --name azure-deploy \
  --query properties.outputs

# 4. Deploy application
dotnet publish src/WhatsAppAIAssistantBot.Api -c Release -o ./publish
az webapp deployment source config-zip \
  --resource-group rg-whatsapp-bot \
  --name your-app-service-name \
  --src ./publish.zip
```

### Option 3: GitHub Actions CI/CD (Production)

Automated deployment pipeline with staging and production environments:

#### 1. Set up GitHub Secrets

In your GitHub repository, add these secrets:

```bash
# Azure authentication
AZURE_WEBAPP_PUBLISH_PROFILE    # Download from Azure Portal
AZURE_CREDENTIALS               # Service Principal JSON

# Application configuration
OPENAI_API_KEY                  # Your OpenAI API key
OPENAI_ASSISTANT_ID             # Your OpenAI Assistant ID
TWILIO_ACCOUNT_SID              # Your Twilio Account SID
TWILIO_AUTH_TOKEN               # Your Twilio Auth Token
TWILIO_FROM_NUMBER              # Your Twilio WhatsApp number
```

#### 2. Workflow Configuration

The included GitHub Actions workflows provide:
- **Continuous Integration**: `.github/workflows/ci.yml`
- **Staging Deployment**: `.github/workflows/deploy-staging.yml`
- **Production Deployment**: `.github/workflows/deploy-production.yml`

#### 3. Deployment Process

```bash
# Trigger staging deployment
git push origin develop

# Trigger production deployment
git push origin main
```

## ⚙️ Configuration

### Azure App Service Settings

Configure these application settings in Azure Portal > App Service > Configuration:

#### Required Settings
```bash
# OpenAI Configuration
OpenAI__ApiKey                 = sk-your-openai-api-key
OpenAI__AssistantId            = asst_your-assistant-id

# Twilio Configuration  
Twilio__AccountSid             = ACyour-twilio-account-sid
Twilio__AuthToken              = your-twilio-auth-token
Twilio__FromNumber             = whatsapp:+1234567890

# Database Configuration (Production)
ConnectionStrings__DefaultConnection = Server=tcp:your-server.database.windows.net;Database=whatsapp-bot;User ID=your-user;Password=your-password;Encrypt=true;

# Application Insights (Auto-configured)
APPLICATIONINSIGHTS_CONNECTION_STRING = InstrumentationKey=your-key;IngestionEndpoint=...
```

#### Optional Settings
```bash
# Environment Configuration
ASPNETCORE_ENVIRONMENT         = Production
ASPNETCORE_URLS               = https://+:443;http://+:80

# Logging Configuration
Logging__LogLevel__Default     = Information
Logging__LogLevel__Microsoft   = Warning

# Performance Settings
WEBSITE_ENABLE_SYNC_UPDATE_SITE = true
WEBSITE_RUN_FROM_PACKAGE       = 1
```

### Database Configuration

#### Development (SQLite)
```json
{
  \"ConnectionStrings\": {
    \"DefaultConnection\": \"Data Source=whatsapp_bot.db\"
  }
}
```

#### Production (Azure SQL Database)
```bash
# Create Azure SQL Database
az sql server create \
  --name whatsapp-bot-sql-server \
  --resource-group rg-whatsapp-bot \
  --location \"East US 2\" \
  --admin-user sqladmin \
  --admin-password YourStrongPassword123!

az sql db create \
  --resource-group rg-whatsapp-bot \
  --server whatsapp-bot-sql-server \
  --name whatsapp-bot-db \
  --service-objective S0
```

## 🔗 Twilio Configuration

### 1. WhatsApp Business Account Setup
1. **Apply for WhatsApp Business API** access through Twilio
2. **Complete business verification** process
3. **Configure webhook endpoints** in Twilio Console

### 2. Webhook Configuration

Set your Twilio webhook URL to:
```
https://your-app-name.azurewebsites.net/api/whatsapp
```

**Webhook Settings:**
- **HTTP Method**: POST
- **Content Type**: application/x-www-form-urlencoded
- **Events**: Incoming Messages

### 3. Testing Your Webhook

```bash
# Test webhook locally (using ngrok for local development)
ngrok http 5001
# Use the ngrok URL: https://abc123.ngrok.io/api/whatsapp

# Test production webhook
curl -X POST https://your-app-name.azurewebsites.net/api/whatsapp \
  -H \"Content-Type: application/x-www-form-urlencoded\" \
  -d \"From=whatsapp%3A%2B1234567890&Body=Hello%20Bot\"
```

## 🐳 Docker Deployment

### Local Docker Testing

```bash
# Build image
docker build -t whatsapp-bot .

# Run with environment variables
docker run -d -p 8080:8080 \
  --name whatsapp-bot-container \
  -e OpenAI__ApiKey=your_openai_key \
  -e OpenAI__AssistantId=your_assistant_id \
  -e Twilio__AccountSid=your_twilio_sid \
  -e Twilio__AuthToken=your_twilio_token \
  -e Twilio__FromNumber=whatsapp:+1234567890 \
  whatsapp-bot

# Check logs
docker logs whatsapp-bot-container

# Health check
curl http://localhost:8080/api/health
```

### Azure Container Instances

```bash
# Deploy to Azure Container Instances
az container create \
  --resource-group rg-whatsapp-bot \
  --name whatsapp-bot-aci \
  --image your-registry.azurecr.io/whatsapp-bot:latest \
  --ports 80 443 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
  --secure-environment-variables \
    OpenAI__ApiKey=your_key \
    Twilio__AuthToken=your_token
```

## 📊 Monitoring & Observability

### Application Insights Setup

Application Insights is automatically configured and provides:

#### Key Metrics to Monitor
- **Request Rate**: Messages per minute/hour
- **Response Time**: Message processing latency
- **Error Rate**: Failed message processing
- **User Activity**: Registration completion rates

#### Custom Dashboards

Create custom dashboards in Azure Portal for:
```bash
# Key Performance Indicators
- Average Response Time: < 2 seconds
- Success Rate: > 99%
- User Registration Rate: Track daily signups
- Command Usage: Most popular bot commands
```

#### Alerts Configuration

Set up alerts for:
```bash
# Critical Alerts
- Error Rate > 5% (5-minute window)
- Response Time > 5 seconds (sustained)
- Available Memory < 20%

# Warning Alerts  
- Error Rate > 1% (15-minute window)
- Response Time > 3 seconds (sustained)
- CPU Usage > 80%
```

### Health Checks

The application includes comprehensive health checks:

```bash
# Basic health check
GET /api/health
Response: {\"status\": \"healthy\", \"timestamp\": \"2024-01-01T12:00:00Z\", \"version\": \"1.0.0\"}

# Readiness check
GET /api/health/ready  
Response: {\"status\": \"ready\", \"timestamp\": \"2024-01-01T12:00:00Z\"}
```

## 🔐 Security Configuration

### SSL/TLS Configuration

```bash
# Enable HTTPS only
az webapp config set \
  --resource-group rg-whatsapp-bot \
  --name your-app-name \
  --https-only true

# Configure custom domain with SSL
az webapp config hostname add \
  --resource-group rg-whatsapp-bot \
  --webapp-name your-app-name \
  --hostname bot.yourdomain.com
```

### Azure Key Vault Integration

```bash
# Create Key Vault
az keyvault create \
  --name whatsapp-bot-vault \
  --resource-group rg-whatsapp-bot \
  --location \"East US 2\"

# Store secrets
az keyvault secret set --vault-name whatsapp-bot-vault --name \"OpenAI-ApiKey\" --value \"your-api-key\"
az keyvault secret set --vault-name whatsapp-bot-vault --name \"Twilio-AuthToken\" --value \"your-auth-token\"

# Configure App Service to use Key Vault
az webapp config appsettings set \
  --resource-group rg-whatsapp-bot \
  --name your-app-name \
  --settings OpenAI__ApiKey=\"@Microsoft.KeyVault(VaultName=whatsapp-bot-vault;SecretName=OpenAI-ApiKey)\"
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Deployment Failures
```bash
# Check deployment logs
az webapp log tail --resource-group rg-whatsapp-bot --name your-app-name

# View deployment history
az webapp deployment list --resource-group rg-whatsapp-bot --name your-app-name
```

#### 2. Configuration Issues
```bash
# Verify app settings
az webapp config appsettings list --resource-group rg-whatsapp-bot --name your-app-name

# Test configuration
curl https://your-app-name.azurewebsites.net/api/health
```

#### 3. Database Connection Issues
```bash
# Test database connectivity
az sql db show-connection-string \
  --server your-sql-server \
  --name whatsapp-bot-db \
  --client ado.net
```

#### 4. Twilio Webhook Issues
```bash
# Verify webhook URL accessibility
curl -X POST https://your-app-name.azurewebsites.net/api/whatsapp \
  -H \"Content-Type: application/x-www-form-urlencoded\" \
  -d \"From=whatsapp%3A%2B1234567890&Body=test\"

# Check Twilio webhook logs
# Visit: https://console.twilio.com/us1/develop/phone-numbers/manage/incoming
```

### Performance Optimization

#### 1. App Service Plan Scaling
```bash
# Scale up (vertical scaling)
az appservice plan update \
  --name your-app-service-plan \
  --resource-group rg-whatsapp-bot \
  --sku P1V2

# Scale out (horizontal scaling)
az appservice plan update \
  --name your-app-service-plan \
  --resource-group rg-whatsapp-bot \
  --number-of-workers 3
```

#### 2. Database Performance
```bash
# Upgrade SQL Database tier
az sql db update \
  --resource-group rg-whatsapp-bot \
  --server your-sql-server \
  --name whatsapp-bot-db \
  --service-objective S2
```

## 💰 Cost Optimization

### Azure Cost Management

#### Estimated Monthly Costs (East US 2)
- **App Service (B1)**: ~$13/month
- **SQL Database (S0)**: ~$15/month  
- **Application Insights**: ~$2/month (first 5GB free)
- **Storage**: ~$1/month
- **Total**: ~$31/month for basic production setup

#### Cost Optimization Tips
```bash
# Use deployment slots for zero-downtime deployments
az webapp deployment slot create \
  --name your-app-name \
  --resource-group rg-whatsapp-bot \
  --slot staging

# Enable auto-shutdown for development environments
az vm auto-shutdown \
  --resource-group rg-whatsapp-bot-dev \
  --name dev-vm \
  --time 1800
```

## 📚 Additional Resources

- **[Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)**
- **[Twilio WhatsApp API Documentation](https://www.twilio.com/docs/whatsapp)**
- **[OpenAI Assistant API Documentation](https://platform.openai.com/docs/assistants)**
- **[Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)**

---

**🎉 Congratulations!** Your WhatsApp AI Assistant Bot is now deployed and ready for production use!