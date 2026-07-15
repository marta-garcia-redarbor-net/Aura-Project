@description('Location for the UI container app.')
param location string

@description('Container App name for UI.')
param appName string

@description('Managed Environment resource ID for Container Apps.')
param containerAppEnvironmentId string

@description('UI container image reference (ghcr.io).')
param image string

@description('Public API base URL that UI will call.')
param apiBaseUrl string

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
        stickySessions: {
          affinity: 'sticky'
        }
      }
    }
    template: {
      containers: [
        {
          name: 'ui'
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
              name: 'Api__BaseUrl'
              value: apiBaseUrl
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
