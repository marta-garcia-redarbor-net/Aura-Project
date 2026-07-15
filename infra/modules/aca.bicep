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

@description('Entra ID client secret (for OIDC auth code flow on the UI)')
@secure()
param entraClientSecret string

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
var ollamaName = '${namePrefix}-ollama-${environment}'
var apiName = '${namePrefix}-api-${environment}'
var uiName = '${namePrefix}-ui-${environment}'
var workersName = '${namePrefix}-workers-${environment}'

// Internal FQDNs within ACA environment
var qdrantInternalUrl = 'http://${qdrantName}:6333'
var ollamaInternalUrl = 'http://${ollamaName}'

// External FQDNs using CAE default domain (avoids circular refs between container apps)
var defaultDomain = cae.properties.defaultDomain
var apiFqdn = '${apiName}.${defaultDomain}'
var uiFqdn = '${uiName}.${defaultDomain}'
var qdrantFqdn = '${qdrantName}.${defaultDomain}'

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
        additionalPortMappings: [
          {
            // Internal gRPC port for QdrantClient SDK operations
            targetPort: 6334
            exposedPort: 6334
            external: false
          }
        ]
      }
      registries: []
      secrets: []
    }
    template: {
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
              name: 'QDRANT__SERVICE__GRPC_PORT'
              value: '6334'
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
// Container App — Ollama (internal, LLM server)
// ============================================================================

resource ollamaApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: ollamaName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: ''
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 11434
        transport: 'http'
        allowInsecure: true
      }
      registries: []
      secrets: []
    }
    template: {
      containers: [
        {
          image: 'ollama/ollama:latest'
          name: 'ollama'
          command: [
            '/bin/sh'
            '-c'
          ]
          args: [
            'ollama serve & sleep 3 && ollama pull llama3.2:1b && ollama pull nomic-embed-text && wait'
          ]
          env: [
            {
              name: 'OLLAMA_KEEP_ALIVE'
              value: '5m'
            }
          ]
          resources: {
            // llmama3.2:1b (~700 MB) fits comfortably in 1.0 CPU / 2.0 Gi on Consumption plan
            // Max memory for 1.0 CPU on ACA Consumption is 2.0 Gi
            cpu: json('1.0')
            memory: '2.0Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/api/tags'
                port: 11434
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 15
              timeoutSeconds: 120
              failureThreshold: 6
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
              // Internal container name — ACA resolves it within the environment via HTTP REST
              // (gRPC through ACA external ingress proxy is not supported on Consumption plan)
              name: 'Qdrant__Host'
              value: qdrantName
            }
            {
              name: 'Qdrant__HttpPort'
              value: '6333'
            }
            {
              name: 'Qdrant__GrpcPort'
              value: '6334'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
            {
              name: 'LlmAdvisor__Enabled'
              value: 'true'
            }
            {
              name: 'LlmAdvisor__Endpoint'
              value: ollamaInternalUrl
            }
            {
              name: 'LlmAdvisor__ModelId'
              value: 'llama3.2:1b'
            }
            {
              name: 'LlmAdvisor__Provider'
              value: 'Ollama'
            }
            {
              name: 'LlmAdvisor__TimeoutSeconds'
              value: '30'
            }
            {
              name: 'LlmAdvisor__ConfidenceThreshold'
              value: '0.7'
            }
            {
              name: 'EmbeddingProvider__Endpoint'
              value: ollamaInternalUrl
            }
            {
              name: 'EmbeddingProvider__DeploymentName'
              value: 'nomic-embed-text'
            }
            {
              // CORS: allow browser calls from the deployed UI origin
              // Uses computed FQDN (cae.properties.defaultDomain) to avoid circular ref
              name: 'Cors__UiOrigin'
              value: 'https://${uiFqdn}'
            }
            {
              // Enable EntraId token validation on the API
              name: 'UseEntraId'
              value: 'true'
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
        minReplicas: 1
        maxReplicas: 1
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
              // Double underscore maps to AuraApi:BaseUrl in .NET config (not single underscore)
              // Uses computed FQDN (cae.properties.defaultDomain) to avoid circular ref
              name: 'AuraApi__BaseUrl'
              value: 'https://${apiFqdn}'
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
              name: 'AzureAd__ClientSecret'
              value: entraClientSecret
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
        minReplicas: 1
        maxReplicas: 1
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
              // Internal container name — ACA resolves it within the environment via HTTP REST
              // (gRPC through ACA external ingress proxy is not supported on Consumption plan)
              name: 'Qdrant__Host'
              value: qdrantName
            }
            {
              name: 'Qdrant__HttpPort'
              value: '6333'
            }
            {
              name: 'Qdrant__GrpcPort'
              value: '6334'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
            {
              name: 'EmbeddingProvider__Endpoint'
              value: ollamaInternalUrl
            }
            {
              name: 'EmbeddingProvider__DeploymentName'
              value: 'nomic-embed-text'
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

output apiUrl string = 'https://${apiFqdn}'
output uiUrl string = 'https://${uiFqdn}'
output qdrantInternalUrl string = qdrantInternalUrl
output ollamaInternalUrl string = ollamaInternalUrl
output apiName string = apiName
output uiName string = uiName
output workersName string = workersName
output environmentId string = cae.id
