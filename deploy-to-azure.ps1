# Azure Deployment Script for WhatsApp AI Assistant Bot

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServiceName
)

# Login to Azure (if not already logged in)
Write-Host "Checking Azure login status..."
$context = Get-AzContext
if (-not $context) {
    Write-Host "Please login to Azure"
    Connect-AzAccount
}

# Generate deterministic app service name if not provided
if (-not $AppServiceName) {
    $hash = [System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($ResourceGroupName))
    $hashString = [System.BitConverter]::ToString($hash).Replace("-", "").Substring(0, 8).ToLower()
    $AppServiceName = "whatsapp-ai-bot-$hashString"
}

# Create resource group if it doesn't exist
Write-Host "Creating resource group: $ResourceGroupName"
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Host "Resource group doesn't exist, creating..."
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
} else {
    Write-Host "Resource group already exists, using existing one."
}

# Deploy Bicep template
Write-Host "Deploying Azure resources with App Service Name: $AppServiceName"
$deployment = New-AzResourceGroupDeployment `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile "azure-deploy.bicep" `
    -appServiceName $AppServiceName `
    -location $Location

if ($deployment.ProvisioningState -eq "Succeeded") {
    Write-Host "Azure resources deployed successfully!"
    Write-Host "App Service URL: $($deployment.Outputs.appServiceUrl.Value)"
    Write-Host "App Service Name: $($deployment.Outputs.appServiceName.Value)"
    
    Write-Host "`nNext steps:"
    Write-Host "1. Configure app settings in Azure Portal:"
    Write-Host "   - OpenAI__ApiKey: Your OpenAI API key"
    Write-Host "   - OpenAI__AssistantId: Your OpenAI Assistant ID"
    Write-Host "   - Twilio__AccountSid: Your Twilio Account SID"
    Write-Host "   - Twilio__AuthToken: Your Twilio Auth Token"
    Write-Host "   - Twilio__FromNumber: Your Twilio WhatsApp number"
    Write-Host "2. Deploy your code using GitHub Actions or Azure CLI"
    Write-Host "3. Configure Twilio webhook to point to: $($deployment.Outputs.appServiceUrl.Value)/api/whatsapp"
} else {
    Write-Host "Deployment failed: $($deployment.ProvisioningState)"
}