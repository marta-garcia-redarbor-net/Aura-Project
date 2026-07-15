@description('Location for the API container app.')
param location string

@description('Container App name for API.')
param appName string

@description('Managed Environment resource ID for Container Apps.')
param containerAppEnvironmentId string

@description('API container image reference (ghcr.io).')
param image string

@secure()
@description('Connection string for Aura Azure SQL database.')
param sqlConnectionString string

@description('Allowed CORS origins for API ingress.')
param corsAllowedOrigins array = []

@description('Internal Ollama endpoint URL (e.g., http://aura-ollama:11434).')
param ollamaEndpoint string = ''

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: corsAllowedOrigins
          allowedMethods: [
            'GET'
            'POST'
            'PUT'
            'DELETE'
            'OPTIONS'
          ]
          allowedHeaders: [
            '*'
          ]
        }
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: image
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:8080'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__AuraDb'
              secureValue: sqlConnectionString
            }
            {
              name: 'LlmAdvisor__Enabled'
              value: 'true'
            }
            {
              name: 'LlmAdvisor__Endpoint'
              value: ollamaEndpoint
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
              value: ollamaEndpoint
            }
            {
              name: 'EmbeddingProvider__DeploymentName'
              value: 'nomic-embed-text'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 20
              periodSeconds: 10
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
