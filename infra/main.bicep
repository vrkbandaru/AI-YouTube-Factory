@description('Location for all resources')
param location string = resourceGroup().location

@description('App name prefix')
param appName string = 'youtube-factory'

@description('API container image')
param apiImage string

@description('Frontend container image')
param frontendImage string

@description('Azure OpenAI endpoint')
param azureOpenAIEndpoint string

@secure()
@description('Azure OpenAI API key')
param azureOpenAIKey string

@description('Azure OpenAI deployment name')
param azureOpenAIDeployment string = 'gpt-4o'

var uniqueSuffix = uniqueString(resourceGroup().id)
var acrName      = '${replace(appName, '-', '')}${uniqueSuffix}'
var appEnvName   = '${appName}-env'
var apiAppName   = '${appName}-api'
var webAppName   = '${appName}-web'
var logName      = '${appName}-logs'

// ── Log Analytics Workspace ────────────────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ── Container Apps Environment ─────────────────────────────────────────────────
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: appEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey:  logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ── API Container App ──────────────────────────────────────────────────────────
resource apiApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: apiAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external:   true
        targetPort: 8080
        transport:  'auto'
        corsPolicy: {
          allowedOrigins:     ['https://${webAppName}.${containerAppsEnv.properties.defaultDomain}']
          allowedMethods:     ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders:     ['*']
          allowCredentials:   true
        }
      }
      secrets: [
        { name: 'openai-key', value: azureOpenAIKey }
      ]
    }
    template: {
      containers: [
        {
          name:  'api'
          image: apiImage
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT',       value: 'Production' }
            { name: 'AzureOpenAI__Endpoint',        value: azureOpenAIEndpoint }
            { name: 'AzureOpenAI__ApiKey',          secretRef: 'openai-key' }
            { name: 'AzureOpenAI__DeploymentName',  value: azureOpenAIDeployment }
            { name: 'AllowedOrigin',
              value: 'https://${webAppName}.${containerAppsEnv.properties.defaultDomain}' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scale'
            http: { metadata: { concurrentRequests: '20' } }
          }
        ]
      }
    }
  }
}

// ── Frontend Container App ─────────────────────────────────────────────────────
resource webApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: webAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external:   true
        targetPort: 80
        transport:  'auto'
      }
    }
    template: {
      containers: [
        {
          name:  'frontend'
          image: frontendImage
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────
output apiUrl     string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output frontendUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
