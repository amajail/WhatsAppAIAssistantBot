@description('Name of the app service')
param appServiceName string = 'whatsapp-ai-bot-${uniqueString(resourceGroup().id)}'

@description('Location for all resources')
param location string = resourceGroup().location

@description('SKU for the App Service Plan')
param skuName string = 'B1'

@description('App Service Plan name')
param appServicePlanName string = '${appServiceName}-plan'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
  }
  properties: {
    reserved: true
  }
  kind: 'linux'
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
    httpsOnly: true
  }
}

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name