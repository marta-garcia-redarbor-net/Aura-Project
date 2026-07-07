@description('Name prefix for resources')
param namePrefix string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Environment name')
param environment string

@description('Log Analytics Workspace resource ID')
param logAnalyticsWorkspaceId string

@description('Log Analytics Customer ID (workspace GUID)')
param logAnalyticsCustomerId string

@description('ACR login server (e.g., myacr.azurecr.io)')
param acrLoginServer string

@description('Managed Identity resource ID (user-assigned)')
param managedIdentityId string

@description('Managed Identity principal ID')
param managedIdentityPrincipalId string

@description('Key Vault URI (e.g., https://myvault.vault.azure.net)')
param keyVaultUri string

@description('Image tag for application containers')
param imageTag string = 'latest'

@description('Entra ID tenant ID')
param entraTenantId string

@description('Entra ID client ID')
param entraClientId string

@description('Microsoft Graph scopes')
param graphScopes string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string

// ============================================================================
// Container Apps Environment
// ============================================================================

resource cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-cae-${environment}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: []
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

// Log Analytics workspace name extracted from resource ID for listKeys call
var logAnalyticsName = (split(logAnalyticsWorkspaceId, '/')[length(split(logAnalyticsWorkspaceId, '/')) - 1])

// ============================================================================
// Role Assignments
// ============================================================================

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(resourceGroup().id, managedIdentityPrincipalId, 'AcrPull')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(resourceGroup().id, managedIdentityPrincipalId, 'KeyVaultSecretsUser')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Helper: internal app URLs
// ============================================================================

var qdrantName = '${namePrefix}-qdrant-${environment}'
var apiName = '${namePrefix}-api-${environment}'
var uiName = '${namePrefix}-ui-${environment}'
var workersName = '${namePrefix}-workers-${environment}'

// Internal FQDN within ACA environment
var qdrantInternalUrl = 'http://${qdrantName}:6333'

// ============================================================================
// Container App — Qdrant (internal, sidecar)
// ============================================================================

resource qdrantApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: qdrantName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: ''
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6333
        transport: 'http'
        allowInsecure: true
      }
      registries: []
      secrets: []
    }
    template: {
      revisionSuffix: '01'
      containers: [
        {
          image: 'qdrant/qdrant:latest'
          name: 'qdrant'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'QDRANT__SERVICE__HTTP_PORT'
              value: '6333'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 6333
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 3
            }
          ]
          volumeMounts: []
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
        rules: []
      }
      volumes: []
    }
  }
}

// ============================================================================
// Container App — Aura API
// ============================================================================

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: ''
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentityId
        }
      ]
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVaultUri}secrets/SqlConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'app-insights-connection-string'
          value: applicationInsightsConnectionString
        }
      ]
    }
    template: {
      revisionSuffix: '01'
      containers: [
        {
          image: '${acrLoginServer}/aura-api:${imageTag}'
          name: 'aura-api'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'SQL_CONNECTION_STRING'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: entraClientId
            }
            {
              name: 'GRAPH_SCOPES'
              value: graphScopes
            }
            {
              name: 'QDRANT_HOST'
              value: qdrantName
            }
            {
              name: 'QDRANT_HTTP_PORT'
              value: '6333'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 15
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 5
            }
          ]
          volumeMounts: []
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-scale'
            custom: {
              type: 'http'
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
      volumes: []
    }
  }
}

// ============================================================================
// Container App — Aura UI
// ============================================================================

resource uiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: uiName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: ''
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentityId
        }
      ]
      secrets: [
        {
          name: 'app-insights-connection-string'
          value: applicationInsightsConnectionString
        }
      ]
    }
    template: {
      revisionSuffix: '01'
      containers: [
        {
          image: '${acrLoginServer}/aura-ui:${imageTag}'
          name: 'aura-ui'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'AURA_API_BASE_URL'
              value: apiApp.properties.configuration.ingress.fqdn
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: entraClientId
            }
            {
              name: 'UseEntraId'
              value: 'true'
            }
            {
              name: 'GRAPH_SCOPES'
              value: graphScopes
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 15
              timeoutSeconds: 5
              failureThreshold: 3
            }
          ]
          volumeMounts: []
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-scale'
            custom: {
              type: 'http'
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
      volumes: []
    }
  }
}

// ============================================================================
// Container App — Aura Workers
// ============================================================================

resource workersApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: workersName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: ''
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentityId
        }
      ]
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVaultUri}secrets/SqlConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'app-insights-connection-string'
          value: applicationInsightsConnectionString
        }
      ]
    }
    template: {
      revisionSuffix: '01'
      containers: [
        {
          image: '${acrLoginServer}/aura-workers:${imageTag}'
          name: 'aura-workers'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'SQL_CONNECTION_STRING'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: entraClientId
            }
            {
              name: 'GRAPH_SCOPES'
              value: graphScopes
            }
            {
              name: 'QDRANT_HOST'
              value: qdrantName
            }
            {
              name: 'QDRANT_HTTP_PORT'
              value: '6333'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
          ]
          volumeMounts: []
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
        rules: []
      }
      volumes: []
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output uiUrl string = 'https://${uiApp.properties.configuration.ingress.fqdn}'
output qdrantInternalUrl string = qdrantInternalUrl
output apiName string = apiName
output uiName string = uiName
output workersName string = workersName
output environmentId string = cae.id
