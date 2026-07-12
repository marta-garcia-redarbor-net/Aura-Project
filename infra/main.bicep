@description('Azure region for all resources')
param location string = 'westeurope'

@description('Environment name (dev, test, prod)')
param environment string = 'dev'

@description('Tags to apply to all resources')
param tags object = {
  project: 'Aura'
  environment: environment
  managedBy: 'bicep'
}

@description('Prefix for resource naming')
param namePrefix string = 'aura'

@description('ACR SKU (Basic, Standard, Premium)')
param acrSku string = 'Basic'

@description('SQL administrator login')
param sqlAdminLogin string = 'auraadmin'

@description('SQL administrator password (auto-generated if empty: Aura_{hash}_2024!)')
@secure()
param sqlAdminPassword string = 'Aura_${uniqueString(resourceGroup().id, sqlAdminLogin)}_2024!'

@description('SQL database SKU (free uses S0, serverless uses GP_S_Gen5_1)')
param sqlSkuName string = 'GP_S_Gen5_1'

@description('SQL database max size in GB')
param sqlMaxSizeGb int = 32

@description('SQL auto-pause delay in minutes (-1 to disable). Dev default: 60')
param autoPauseDelay int = 60

@description('Entra ID tenant ID for Aura')
param entraTenantId string = ''

@description('Entra ID client ID for Aura App Registration')
param entraClientId string = ''

@description('Entra ID client secret for OIDC auth code flow')
@secure()
param entraClientSecret string = ''

@description('Microsoft Graph scopes for delegated auth')
param graphScopes string = 'User.Read,Calendars.Read,Mail.Read,Chat.Read,Presence.Read'

@description('Image tag for container apps (default: latest)')
param imageTag string = 'latest'

// ============================================================================
// Modules
// ============================================================================

module registry 'modules/registry.bicep' = {
  name: 'registry-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    sku: acrSku
  }
}

module database 'modules/database.bicep' = {
  name: 'database-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    skuName: sqlSkuName
    maxSizeGb: sqlMaxSizeGb
    autoPauseDelay: autoPauseDelay
    environment: environment
  }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    environment: environment
    sqlDatabaseName: database.outputs.sqlDatabaseName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    sqlServerFqdn: database.outputs.sqlServerFqdn
    entraTenantId: entraTenantId
    entraClientId: entraClientId
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    environment: environment
  }
}

module identity 'modules/identity.bicep' = {
  name: 'identity-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    environment: environment
  }
}

module aca 'modules/aca.bicep' = {
  name: 'aca-module'
  params: {
    namePrefix: namePrefix
    location: location
    tags: tags
    environment: environment
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    logAnalyticsCustomerId: monitoring.outputs.logAnalyticsCustomerId
    acrLoginServer: registry.outputs.acrLoginServer
    managedIdentityId: identity.outputs.managedIdentityId
    managedIdentityPrincipalId: identity.outputs.managedIdentityPrincipalId
    keyVaultUri: keyvault.outputs.keyVaultUri
    imageTag: imageTag
    entraTenantId: entraTenantId
    entraClientId: entraClientId
    entraClientSecret: entraClientSecret
    graphScopes: graphScopes
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
  }
}

// ============================================================================
// Outputs
// ============================================================================

output acrLoginServer string = registry.outputs.acrLoginServer
output acrName string = registry.outputs.acrName
output sqlServerFqdn string = database.outputs.sqlServerFqdn
output sqlDatabaseName string = database.outputs.sqlDatabaseName
output keyVaultName string = keyvault.outputs.keyVaultName
output keyVaultUri string = keyvault.outputs.keyVaultUri
output managedIdentityClientId string = identity.outputs.managedIdentityClientId
output logAnalyticsWorkspaceId string = monitoring.outputs.logAnalyticsWorkspaceId
output applicationInsightsConnectionString string = monitoring.outputs.applicationInsightsConnectionString
output containerAppApiUrl string = aca.outputs.apiUrl
output containerAppUiUrl string = aca.outputs.uiUrl
