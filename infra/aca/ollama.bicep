@description('Location for the Ollama container app.')
param location string

@description('Container App name for Ollama.')
param appName string

@description('Managed Environment resource ID for Container Apps.')
param containerAppEnvironmentId string

var ollamaInternalUrl = 'http://${appName}:11434'

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 11434
        transport: 'http'
        allowInsecure: true
      }
    }
    template: {
      containers: [
        {
          name: 'ollama'
          image: 'ollama/ollama:latest'
          command: [
            '/bin/sh'
            '-c'
          ]
          args: [
            'ollama serve & sleep 3 && ollama pull llama3.2:1b && wait'
          ]
          env: [
            {
              name: 'OLLAMA_KEEP_ALIVE'
              value: '5m'
            }
          ]
          resources: {
            cpu: json('1.0')
            memory: '4Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/api/tags'
                port: 11434
              }
              initialDelaySeconds: 30
              periodSeconds: 15
              timeoutSeconds: 120
              failureThreshold: 6
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output internalUrl string = ollamaInternalUrl
