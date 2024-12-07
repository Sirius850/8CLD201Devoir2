@description('Name of Fucntion App')
param functionAppName string

@description('Location of Function App')
param location string = resourceGroup().location

@description('Storage account name for Function App')
param storageAccountName string

@description('Hosting plan for Function App')
param hostingPlanName string

@description('Plan SKU for Function App')
param sku string = 'Y1'

@description('Worker size for hosting plan')
param workerSize string = 'Small'

@description('Number of workers for hosting plan')
param numberOfWorkers int = 1

@description('OS of Fucntion App')
param osType string = 'Windows'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
	name: storageAccountName
	location: location
	sku: {
		name: 'Standard_LRS'
	}
	kind: 'StorageV2'
	properties: {}
}

resource functionAppPlan 'Microsoft.Web/serverFarms@2021-03-01' = {
	name: hostingPlanName
	location: location
	sku:{
		name: sku
		tier: 'Dynamic'
	}
	properties:{
		name: hostingPlanName
		perSiteScaling: true
		maximumElasticWorkerCount: 1
	}
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' =  {
	name: functionAppName
	location: location
	kind: 'functionapp'
	properties:{
		serverFarmId: functionAppPlan.id
    appSettings: {
      'FUNCTIONS_WORKER_RUNTIME': 'python'
      'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING': storageAccount.id
      'WEBSITE_CONTENTSHARE': '${functionAppName}content'
		}
	}
}

output functionAppId string = functionApp.id
