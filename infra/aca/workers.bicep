@description('Location for the Workers container app.')
param location string

@description('Container App name for Workers.')
param appName string

@description('Managed Environment resource ID for Container Apps.')
param containerAppEnvironmentId string

@description('Workers container image reference (ghcr.io).')
param image string

@secure()
@description('Connection string for Aura Azure SQL database.')
param sqlConnectionString string

@description('Internal Ollama endpoint URL for embeddings.')
param ollamaEndpoint string = ''

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {}
    template: {
      containers: [
        {
          name: 'workers'
          image: image
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__Aura'
              secureValue: sqlConnectionString
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
