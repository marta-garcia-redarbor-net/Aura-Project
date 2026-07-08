targetScope = 'subscription'

@description('Azure region for ACA and SQL resources.')
param location string

@description('Resource group name where ACA and SQL resources will be deployed.')
param resourceGroupName string

@description('Environment name prefix (used in resource names).')
param environmentName string

@description('API container image from ghcr.io.')
param apiImage string

@description('UI container image from ghcr.io.')
param uiImage string

@description('Workers container image from ghcr.io.')
param workersImage string

@description('Azure SQL administrator login.')
param sqlAdministratorLogin string

@secure()
@description('Azure SQL administrator password.')
param sqlAdministratorPassword string

@description('Allowed CORS origins for the API ingress.')
param corsAllowedOrigins array = []

var acaEnvironmentName = '${environmentName}-aca-env'
var apiAppName = '${environmentName}-api'
var uiAppName = '${environmentName}-ui'
var workersAppName = '${environmentName}-workers'
var sqlServerName = '${take(replace(toLower(environmentName), '-', ''), 18)}-sql'
var sqlDatabaseName = 'AuraDb'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  scope: rg
  name: acaEnvironmentName
  location: location
  properties: {
    zoneRedundant: false
  }
}

module sqlDatabase './sql-database.bicep' = {
  name: 'sql-database'
  scope: rg
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    administratorLogin: sqlAdministratorLogin
    administratorPassword: sqlAdministratorPassword
  }
}

module api './api.bicep' = {
  name: 'aca-api'
  scope: rg
  params: {
    location: location
    appName: apiAppName
    containerAppEnvironmentId: managedEnvironment.id
    image: apiImage
    sqlConnectionString: sqlDatabase.outputs.connectionString
    corsAllowedOrigins: corsAllowedOrigins
  }
}

module ui './ui.bicep' = {
  name: 'aca-ui'
  scope: rg
  params: {
    location: location
    appName: uiAppName
    containerAppEnvironmentId: managedEnvironment.id
    image: uiImage
    apiBaseUrl: 'https://${api.outputs.fqdn}'
  }
}

module workers './workers.bicep' = {
  name: 'aca-workers'
  scope: rg
  params: {
    location: location
    appName: workersAppName
    containerAppEnvironmentId: managedEnvironment.id
    image: workersImage
    sqlConnectionString: sqlDatabase.outputs.connectionString
  }
}

output resourceGroupId string = rg.id
output containerAppEnvironmentId string = managedEnvironment.id
output apiUrl string = 'https://${api.outputs.fqdn}'
output uiUrl string = 'https://${ui.outputs.fqdn}'
