@description('Name of storage account')
param storageAccountName string

@description('Location for storage account')
param location string = resourceGroup().location

@description('Performance tier of storage account')
param performanceTier string = 'Standard'

@description('Redundancy of storage account')
param redundancy string = 'LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
	name: storageAccountName
	location: location
	sku: {
		name: '${performanceTier}_${redundancy}'
	}
	kind: 'StorageV2'
	properties: {}
}

output storageAccountId string = storageAccount.id


