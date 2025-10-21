# Azure Resources Setup Guide

This guide walks you through creating all necessary Azure resources for the Court Listener MCP Teams Bot demo.

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed ([Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- PowerShell or Bash terminal

## 1. Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

## 2. Create Resource Group

```bash
az group create \
  --name rg-courtlistener-demo \
  --location eastus
```

## 3. Create Azure OpenAI Resource

### Create the resource

```bash
az cognitiveservices account create \
  --name openai-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --location eastus \
  --kind OpenAI \
  --sku S0
```

### Deploy a model

```bash
az cognitiveservices account deployment create \
  --name openai-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --deployment-name gpt-4 \
  --model-name gpt-4 \
  --model-version "0613" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name "Standard"
```

### Get the endpoint and key

```bash
# Get endpoint
az cognitiveservices account show \
  --name openai-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --query "properties.endpoint" \
  --output tsv

# Get API key
az cognitiveservices account keys list \
  --name openai-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --query "key1" \
  --output tsv
```

**Save these values:**
- Endpoint: `https://openai-courtlistener-demo.openai.azure.com/`
- API Key: `<your-key>`
- Deployment Name: `gpt-4`

## 4. Create Azure Bot Service

### Create App Registration

```bash
# Create app registration and get App ID
az ad app create \
  --display-name "CourtListenerBot" \
  --sign-in-audience AzureADMultiTenant

# Get the App ID (save this)
APP_ID=$(az ad app list --display-name "CourtListenerBot" --query "[0].appId" -o tsv)
echo "App ID: $APP_ID"

# Create client secret
az ad app credential reset \
  --id $APP_ID \
  --append \
  --credential-description "BotSecret"
```

**Save the output:**
- App ID: `<your-app-id>`
- App Secret: `<your-secret>`

### Create Azure Bot Resource

```bash
az bot create \
  --name bot-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --kind webapp \
  --sku F0 \
  --appid $APP_ID \
  --endpoint "https://YOUR_BOT_ENDPOINT/api/messages"
```

> **Note:** You'll update the endpoint later after deploying the Teams bot application.

## 5. Create Azure Function App for MCP Server

### Create Storage Account

```bash
az storage account create \
  --name stcourtlistenerdemo \
  --resource-group rg-courtlistener-demo \
  --location eastus \
  --sku Standard_LRS
```

### Create Function App

```bash
az functionapp create \
  --name func-courtlistener-mcp \
  --resource-group rg-courtlistener-demo \
  --storage-account stcourtlistenerdemo \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4
```

### Get Function App URL

```bash
az functionapp show \
  --name func-courtlistener-mcp \
  --resource-group rg-courtlistener-demo \
  --query "defaultHostName" \
  --output tsv
```

**Save this URL:** `https://func-courtlistener-mcp.azurewebsites.net`

## 6. Create App Service for Teams Bot

```bash
az appservice plan create \
  --name plan-courtlistener-bot \
  --resource-group rg-courtlistener-demo \
  --location eastus \
  --sku B1 \
  --is-linux

az webapp create \
  --name app-courtlistener-bot \
  --resource-group rg-courtlistener-demo \
  --plan plan-courtlistener-bot \
  --runtime "DOTNETCORE:8.0"
```

### Get App Service URL

```bash
az webapp show \
  --name app-courtlistener-bot \
  --resource-group rg-courtlistener-demo \
  --query "defaultHostName" \
  --output tsv
```

**Save this URL:** `https://app-courtlistener-bot.azurewebsites.net`

### Update Bot Endpoint

Now update the bot's messaging endpoint:

```bash
az bot update \
  --name bot-courtlistener-demo \
  --resource-group rg-courtlistener-demo \
  --endpoint "https://app-courtlistener-bot.azurewebsites.net/api/messages"
```

## 7. Configure Teams Channel

```bash
# Enable Microsoft Teams channel
az bot msteams create \
  --name bot-courtlistener-demo \
  --resource-group rg-courtlistener-demo
```

## Summary

At this point, you should have:

1. **Resource Group:** `rg-courtlistener-demo`
2. **Azure OpenAI:**
   - Name: `openai-courtlistener-demo`
   - Endpoint: `https://openai-courtlistener-demo.openai.azure.com/`
   - API Key: `<saved>`
   - Deployment: `gpt-4`

3. **Bot Registration:**
   - Bot Name: `bot-courtlistener-demo`
   - App ID: `<saved>`
   - App Secret: `<saved>`

4. **Function App (MCP Server):**
   - Name: `func-courtlistener-mcp`
   - URL: `https://func-courtlistener-mcp.azurewebsites.net`

5. **App Service (Teams Bot):**
   - Name: `app-courtlistener-bot`
   - URL: `https://app-courtlistener-bot.azurewebsites.net`

## Next Steps

- Proceed to [Dataverse Setup](./dataverse-setup.md) to configure caching
- Then deploy the MCP Server following [MCP Server Deployment](./mcp-server-setup.md)
- Then deploy the Teams Bot following [Teams Bot Deployment](./teams-bot-setup.md)
