# Azure Resources Setup Guide

This guide walks you through creating all necessary Azure resources for the Court Listener MCP Teams Bot demo.

## Prerequisites

- Completed [Resource Identification](./resource-identification.md)
- All variables from Resource Identification set in your terminal
- Azure CLI installed and logged in
- PowerShell or Bash terminal

## 1. Create Resource Group

```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

## 2. Create Azure OpenAI Resource

### Create the resource

```bash
az cognitiveservices account create \
  --name $OPENAI_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --kind OpenAI \
  --sku S0
```

### Deploy a model

> NOTE: Models are changing quickly. The below code uses a model that was available _when this document was written_. However, if you receive an error running the below command or you'd like to use an updated model, run the following command to view which models are currently available:
> ```bash
> az cognitiveservices model list --location eastus2 | \
> jq '.[] | select(.model.name | contains("gpt-4")) | select(.model.lifecycleStatus == "GenerallyAvailable" or .model.lifecycleStatus == "Preview") | {name: .model.name, version: .model.version, format: .model.format, lifecycleStatus: .model.lifecycleStatus}'
> ```

```bash
# Get endpoint
OPENAI_ENDPOINT=$(az cognitiveservices account show \
  --name $OPENAI_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.endpoint" \
  --output tsv)

echo "OpenAI Endpoint: $OPENAI_ENDPOINT"

# Get API key
OPENAI_API_KEY=$(az cognitiveservices account keys list \
  --name $OPENAI_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "key1" \
  --output tsv)

echo "OpenAI API Key: $OPENAI_API_KEY"
```

## 3. Create Azure Bot Service

### Create App Registration

```bash
# Create app registration
az ad app create \
  --display-name "$BOT_DISPLAY_NAME" \
  --sign-in-audience AzureADMyOrg

# Get the App ID
APP_ID=$(az ad app list --display-name "$BOT_DISPLAY_NAME" --query "[0].appId" -o tsv)
echo "App ID: $APP_ID"

# Add Microsoft Graph API permissions
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Grant admin consent
az ad app permission admin-consent --id $APP_ID

# Create client secret
APP_SECRET=$(az ad app credential reset \
  --id $APP_ID \
  --append \
  --display-name "BotSecret" \
  --query "password" \
  --output tsv)

echo "App Secret: $APP_SECRET"
```

### Create Azure Bot Resource

```bash
# Create bot resource (endpoint will be updated after deploying Teams bot)
az bot create \
  --app-type SingleTenant \
  --name $BOT_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku F0 \
  --appid $APP_ID \
  --endpoint "https://$APP_SERVICE_NAME.azurewebsites.net/api/messages" \
  --tenant-id $TENANT_ID
```

> **Note:** You'll update the endpoint later after deploying the Teams bot application.

## 4. Create Azure Function App for MCP Server

### Create Storage Account

```bash
az storage account create \
  --name $STORAGE_ACCOUNT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS
```

### Enable Shared Key Access

```bash
az storage account update \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --set allowSharedKeyAccess=true
```

### Create Function App

```bash
az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT_NAME \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4
```

### Get Function App URL

```bash
az functionapp show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "defaultHostName" \
  --output tsv
```

**Save this URL:** `https://<FUNCTION_APP_NAME>.azurewebsites.net`

## 5. Create App Service for Teams Bot

```bash
az appservice plan create \
  --name $APP_SERVICE_PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku B1 \
  --is-linux

az webapp create \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN_NAME \
  --runtime "DOTNETCORE:8.0"
```

### Get App Service URL

```bash
az webapp show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "defaultHostName" \
  --output tsv
```

**Save this URL:** `https://<APP_SERVICE_NAME>.azurewebsites.net`

### Update Bot Endpoint

Now update the bot's messaging endpoint:

```bash
az bot update \
  --name $BOT_NAME \
  --resource-group $RESOURCE_GROUP \
  --endpoint "https://<APP_SERVICE_NAME>.azurewebsites.net/api/messages"
```

## 6. Configure Teams Channel

```bash
# Enable Microsoft Teams channel
az bot msteams create \
  --name $BOT_NAME \
  --resource-group $RESOURCE_GROUP
```

## Summary

At this point, you should have the following resources created:

1. **Resource Group:** `$RESOURCE_GROUP`
2. **Azure OpenAI:**
    - Name: `$OPENAI_NAME`
    - Endpoint: `$OPENAI_ENDPOINT` (regional format)
    - API Key: `$OPENAI_API_KEY`
    - Deployment: `$OPENAI_DEPLOYMENT_NAME`

3. **Bot Registration:**
    - Bot Name: `$BOT_NAME`
    - App ID: `$APP_ID`
    - App Secret: `$APP_SECRET`
    - Tenant ID: `$TENANT_ID`

4. **Function App (MCP Server):**
    - Name: `$FUNCTION_APP_NAME`
    - URL: `https://$FUNCTION_APP_NAME.azurewebsites.net`

5. **App Service (Teams Bot):**
    - Name: `$APP_SERVICE_NAME`
    - URL: `https://$APP_SERVICE_NAME.azurewebsites.net`

All values are stored in environment variables for use in subsequent steps.

## Next Steps

- Proceed to [Dataverse Setup](./dataverse-setup.md) to configure caching
- Then deploy the MCP Server following [MCP Server Deployment](./mcp-server-setup.md)
- Then deploy the Teams Bot following [Teams Bot Deployment](./teams-bot-setup.md)
