@description('Name prefix for resources')
param namePrefix string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Environment name')
param environment string

var managedIdentityName = '${namePrefix}-mi-${environment}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

output managedIdentityName string = managedIdentity.name
output managedIdentityId string = managedIdentity.id
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityClientId string = managedIdentity.properties.clientId
