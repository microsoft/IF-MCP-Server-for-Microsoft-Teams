# Teams Bot Deployment Guide

This guide walks you through deploying the Court Listener Teams Bot to Azure App Service.

## Prerequisites

- Completed [Azure Setup](./azure-setup.md)
- Completed [MCP Server Deployment](./mcp-server-setup.md)
- .NET 8 SDK installed

## 1. Configure Application Settings

Navigate to the Teams bot directory:

```bash
cd mcp-teams/teams-bot
```

Update `appsettings.json` with your values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MicrosoftAppId": "<YOUR_BOT_APP_ID>",
  "MicrosoftAppPassword": "<YOUR_BOT_APP_SECRET>",
  "AzureOpenAI": {
    "Endpoint": "https://openai-courtlistener-demo.openai.azure.com/",
    "ApiKey": "<YOUR_AZURE_OPENAI_KEY>",
    "DeploymentName": "gpt-4.1"
  },
  "McpServer": {
    "BaseUrl": "https://func-courtlistener-mcp.azurewebsites.net",
    "FunctionKey": "<YOUR_MCP_FUNCTION_KEY>"
  }
}
```

> **Important:** For local development, create `appsettings.Development.json` with the same structure (this file is gitignored).

## 2. Test Locally

### Build the project

```bash
dotnet restore
dotnet build
```

### Run locally

```bash
dotnet run
```

The bot should start on `https://localhost:5001` (or the port shown in the console).

### Test with Bot Framework Emulator

1. Download and install [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
2. Open the emulator
3. Click **Open Bot**
4. Enter bot URL: `http://localhost:5000/api/messages` (use HTTP for local testing)
5. Enter your **Microsoft App ID** and **Password**
6. Click **Connect**

Try sending messages:
- "Find Supreme Court opinions about privacy"
- "Search for copyright cases"
- "What courts are in the federal system?"

### Test with ngrok (Optional - for Teams testing locally)

```bash
# Install ngrok if not already installed
# Download from https://ngrok.com/download

# Start ngrok
ngrok http 5000
```

Copy the HTTPS URL (e.g., `https://abc123.ngrok.io`) and update your bot's messaging endpoint in Azure Portal.

## 3. Deploy to Azure App Service

### Configure App Service Settings

```bash
# Set variables
APP_SERVICE_NAME="app-courtlistener-bot"
RESOURCE_GROUP="rg-courtlistener-demo"

# Configure application settings
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "MicrosoftAppId=<YOUR_BOT_APP_ID>" \
    "MicrosoftAppPassword=<YOUR_BOT_APP_SECRET>" \
    "AzureOpenAI__Endpoint=https://openai-courtlistener-demo.openai.azure.com/" \
    "AzureOpenAI__ApiKey=<YOUR_AZURE_OPENAI_KEY>" \
    "AzureOpenAI__DeploymentName=gpt-4.1" \
    "McpServer__BaseUrl=https://func-courtlistener-mcp.azurewebsites.net" \
    "McpServer__FunctionKey=<YOUR_MCP_FUNCTION_KEY>"
```

### Publish the Web App

```bash
# From the teams-bot directory
dotnet publish -c Release -o ./publish

# Create a zip file
cd publish
zip -r ../publish.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --src publish.zip
```

Alternative using Visual Studio:
1. Right-click the project in Solution Explorer
2. Select **Publish**
3. Choose **Azure** > **Azure App Service (Windows)**
4. Select your subscription and app service
5. Click **Publish**

## 4. Verify Deployment

### Check the bot endpoint

```bash
# Get the app service URL
APP_URL=$(az webapp show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "defaultHostName" \
  --output tsv)

echo "Bot URL: https://$APP_URL/api/messages"

# Test the endpoint (should return 405 Method Not Allowed for GET, which is expected)
curl https://$APP_URL/api/messages
```

### Test with Bot Framework Emulator (Must have ngrok configured)

1. Open Bot Framework Emulator
2. Click **Open Bot**
3. Enter: `https://$APP_URL/api/messages`
4. Enter your Microsoft App ID and Password
5. Click **Connect**
6. Send a test message

## 5. Configure Bot in Azure Portal

### Update the messaging endpoint

```bash
az bot update \
  --name bot-courtlistener-demo \
  --resource-group $RESOURCE_GROUP \
  --endpoint "https://$APP_URL/api/messages"
```

Or via Azure Portal:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Bot resource (`bot-courtlistener-demo`)
3. Go to **Settings** > **Configuration**
4. Update **Messaging endpoint**: `https://app-courtlistener-bot.azurewebsites.net/api/messages`
5. Click **Apply**

## 6. Monitor and Troubleshoot

### View logs

```bash
# Stream logs
az webapp log tail \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP
```

### Enable detailed logging

```bash
az webapp log config \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --application-logging filesystem \
  --level information
```

### Access logs via Azure Portal

1. Go to your App Service in Azure Portal
2. Click **Log stream** in the left menu
3. View real-time logs

### Common Issues

**Issue:** Bot doesn't respond in Teams
- **Solution:**
  - Verify the messaging endpoint is correct
  - Check App ID and Password are correct
  - Review logs for errors
  - Test with Bot Framework Emulator first

**Issue:** "401 Unauthorized" errors
- **Solution:**
  - Verify MicrosoftAppId and MicrosoftAppPassword are correct
  - Ensure the bot registration is active

**Issue:** MCP calls fail
- **Solution:**
  - Verify McpServer__BaseUrl is correct
  - Check McpServer__FunctionKey is valid
  - Test MCP server independently

**Issue:** Azure OpenAI errors
- **Solution:**
  - Verify endpoint, API key, and deployment name
  - Check Azure OpenAI has sufficient quota
  - Ensure deployment is active

## 7. Security Best Practices

### Use Azure Key Vault (Recommended for Production)

> **NOTE:** The instructions below mimic the instructions for the Key Vault you setup previously. You can use the same Key Vault (recommended), if desired. To do so, simply ignore the first command.

```bash
# Create Key Vault (if not already created)
az keyvault create \
  --name kv-courtlistener \
  --resource-group $RESOURCE_GROUP \
  --location eastus

# Enable system-assigned managed identity
az webapp identity assign \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP

# Get the identity
IDENTITY=$(az webapp identity show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# The web app as a secrets reader
az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee $IDENTITY \
    --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/kv-courtlistener"

# Store secrets
az keyvault secret set --vault-name kv-courtlistener --name "BotAppPassword" --value "<YOUR_BOT_SECRET>"
az keyvault secret set --vault-name kv-courtlistener --name "AzureOpenAIKey" --value "<YOUR_OPENAI_KEY>"
az keyvault secret set --vault-name kv-courtlistener --name "McpFunctionKey" --value "<YOUR_MCP_KEY>"

# Update app settings to reference Key Vault
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "MicrosoftAppPassword=@Microsoft.KeyVault(SecretUri=https://kv-courtlistener.vault.azure.net/secrets/BotAppPassword/)" \
    "AzureOpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=https://kv-courtlistener.vault.azure.net/secrets/AzureOpenAIKey/)" \
    "McpServer__FunctionKey=@Microsoft.KeyVault(SecretUri=https://kv-courtlistener.vault.azure.net/secrets/McpFunctionKey/)"
```

## 8. Enable HTTPS Only

```bash
az webapp update \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true
```

## Configuration Summary

Your Teams Bot should now be deployed with:

- **URL:** `https://app-courtlistener-bot.azurewebsites.net`
- **Endpoint:** `https://app-courtlistener-bot.azurewebsites.net/api/messages`
- **Bot ID:** `<YOUR_BOT_APP_ID>`
- **Connected to:**
  - Azure OpenAI (GPT-4.1)
  - MCP Server (Court Listener API + Dataverse cache)

## Next Steps

- Proceed to [Teams App Registration](./teams-app-registration.md) to install the bot in Teams
