@description('Name prefix for resources')
param namePrefix string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Environment name')
param environment string

@description('SQL database name')
param sqlDatabaseName string

@description('SQL admin login')
param sqlAdminLogin string

@description('SQL admin password')
@secure()
param sqlAdminPassword string

@description('SQL server FQDN')
param sqlServerFqdn string

@description('Entra ID tenant ID')
param entraTenantId string

@description('Entra ID client ID')
param entraClientId string

var keyVaultName = '${namePrefix}kv${environment}${uniqueString(subscription().id)}'
var sqlConnectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    accessPolicies: []
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'SqlConnectionString'
  parent: keyVault
  properties: {
    value: sqlConnectionString
    contentType: 'string'
  }
}

resource entraTenantIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'EntraTenantId'
  parent: keyVault
  properties: {
    value: entraTenantId
    contentType: 'string'
  }
}

resource entraClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'EntraClientId'
  parent: keyVault
  properties: {
    value: empty(entraClientId) ? 'placeholder-update-in-portal' : entraClientId
    contentType: 'string'
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
output sqlConnectionStringSecretUri string = sqlConnectionStringSecret.properties.secretUri
