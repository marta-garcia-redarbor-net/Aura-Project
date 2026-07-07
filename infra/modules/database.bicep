@description('Name prefix for resources')
param namePrefix string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('SQL administrator login name')
param sqlAdminLogin string

@description('SQL administrator password. Auto-generated format: Aura_{hash}_2024! (meets Azure SQL complexity). Override with your own if needed.')
@secure()
param sqlAdminPassword string = 'Aura_${uniqueString(resourceGroup().id, sqlAdminLogin)}_2024!'

@description('SQL database SKU')
param skuName string = 'GP_S_Gen5_1'

@description('SQL database max size in GB')
param maxSizeGb int = 32

@description('SQL database auto-pause delay in minutes (-1 disables auto-pause)')
param autoPauseDelay int = -1

@description('Environment name')
param environment string

var sqlServerName = '${namePrefix}-sql-${environment}-${uniqueString(resourceGroup().id)}'
var sqlDatabaseName = '${namePrefix}-db'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to connect
resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: 'AllowAzureServices'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Allow API container app outbound IPs (placeholder — update after ACA creation)
resource sqlFirewallPlaceholder 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: 'AllowContainerApps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: sqlDatabaseName
  parent: sqlServer
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: maxSizeGb * 1073741824
    sampleName: ''
    zoneRedundant: false
    autoPauseDelay: autoPauseDelay
    minCapacity: json('0.5')
    readScale: 'Disabled'
    highAvailabilityReplicaCount: 0
    requestedBackupStorageRedundancy: 'Local'
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output sqlAdminLogin string = sqlAdminLogin
output sqlAdminPassword string = sqlAdminPassword
output sqlServerId string = sqlServer.id
