# Teams Bot Deployment Guide

This guide walks you through deploying the Court Listener Teams Bot to Azure App Service.

## Prerequisites

- Completed [Resource Identification](./resource-identification.md)
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
  "MicrosoftAppId": "Use value from $APP_ID",
  "MicrosoftAppPassword": "Use value from $APP_SECRET",
  "MicrosoftAppTenantId": "Use value from $TENANT_ID",
  "MicrosoftAppType": "SingleTenant",
  "AzureOpenAI": {
    "Endpoint": "Use value from $OPENAI_ENDPOINT",
    "ApiKey": "Use value from $OPENAI_API_KEY",
    "DeploymentName": "Use value from $OPENAI_DEPLOYMENT_NAME"
  },
  "McpServer": {
    "BaseUrl": "Use value from $FUNCTION_URL",
    "FunctionKey": "Use the mcp_extension system key from MCP server setup"
  }
}
```

> **Important:** For local development, create `appsettings.Development.json` with the same structure (this file is gitignored).

> **Note:** The `MicrosoftAppTenantId` and `MicrosoftAppType` settings are required for SingleTenant bot authentication with Bot Framework Service.

> **Note:** The `McpServer.FunctionKey` must be the `mcp_extension` system key from your MCP server deployment, NOT the default function key. See the [MCP Server Setup](./mcp-server-setup.md) guide for instructions on retrieving this key. The Teams bot calls the `/runtime/webhooks/mcp` endpoint using header-based authentication (`x-functions-key` header).

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
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "MicrosoftAppId=$APP_ID" \
    "MicrosoftAppPassword=$APP_SECRET" \
    "MicrosoftAppTenantId=$TENANT_ID" \
    "MicrosoftAppType=SingleTenant" \
    "AzureOpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "AzureOpenAI__ApiKey=$OPENAI_API_KEY" \
    "AzureOpenAI__DeploymentName=$OPENAI_DEPLOYMENT_NAME" \
    "McpServer__BaseUrl=$FUNCTION_URL" \
    "McpServer__FunctionKey=$FUNCTION_KEY"

# Note: $FUNCTION_KEY should contain the mcp_extension system key from your MCP server
# If you haven't retrieved it yet, see the MCP Server Setup guide
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
# Get the app service URL (should already be set, but verify)
APP_SERVICE_URL=$(az webapp show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "defaultHostName" \
  --output tsv)

echo "Bot URL: https://$APP_SERVICE_URL/api/messages"

# Test the endpoint
curl https://$APP_SERVICE_URL/api/messages
```

### Test with Bot Framework Emulator (Must have ngrok configured)

1. Open Bot Framework Emulator
2. Click **Open Bot**
3. Enter: `https://$APP_SERVICE_URL/api/messages`
4. Enter your Microsoft App ID and Password
5. Click **Connect**
6. Send a test message

## 5. Configure Bot in Azure Portal

### Update the messaging endpoint

```bash
az bot update \
  --name $BOT_NAME \
  --resource-group $RESOURCE_GROUP \
  --endpoint "https://$APP_SERVICE_URL/api/messages"
```

Or via Azure Portal:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Bot resource (`bot-courtlistener-demo`)
3. Go to **Settings** > **Configuration**
4. Update **Messaging endpoint**: `https://$APP_SERVICE_URL/api/messages`
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
  - Verify MicrosoftAppId, MicrosoftAppPassword, MicrosoftAppTenantId, and MicrosoftAppType are all configured correctly
  - Ensure the App Registration has API permissions (Microsoft Graph User.Read) with admin consent granted
  - Verify the bot resource has the Teams channel enabled (az bot msteams create)
  - Confirm the App Registration sign-in audience is AzureADMyOrg (SingleTenant)
  - Check that the bot resource msaAppType matches (SingleTenant)

**Issue:** MCP calls fail
- **Solution:**
  - Verify McpServer__BaseUrl is correct (e.g., `https://func-courtlistener-demo.azurewebsites.net`)
  - Ensure McpServer__FunctionKey uses the `mcp_extension` system key, NOT the default function key
  - Test MCP server independently: `curl -X POST "https://<FUNCTION_APP>.azurewebsites.net/runtime/webhooks/mcp" -H "x-functions-key: <KEY>" -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'`
  - Check logs for authentication errors (403) or protocol errors (406)

**Issue:** Azure OpenAI errors
- **Solution:**
  - Verify endpoint, API key, and deployment name
  - Check Azure OpenAI has sufficient quota
  - Ensure deployment is active

## 7. Security Best Practices

### Use Azure Key Vault (Recommended for Production)

> **Note:** The instructions below mimic the instructions for the Key Vault you setup previously. You can use the same Key Vault (recommended), if desired. To do so, simply ignore the first command.

```bash
# Create Key Vault (if not already created)
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Enable system-assigned managed identity for Web App
az webapp identity assign \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP

# Get the identity
IDENTITY=$(az webapp identity show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# For your user account to manage secrets (if not done in the previous step)
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee $USER_ID \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"

# Grant Key Vault Secrets User role to the identity
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $IDENTITY \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"

# Store secrets
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "BotAppPassword" --value $APP_SECRET
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "AzureOpenAIKey" --value $OPENAI_API_KEY
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "McpFunctionKey" --value $FUNCTION_KEY

# Update app settings to reference Key Vault
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "MicrosoftAppPassword=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/BotAppPassword/)" \
    "AzureOpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/AzureOpenAIKey/)" \
    "McpServer__FunctionKey=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/McpFunctionKey/)"
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

- **URL:** `https://$APP_SERVICE_URL`
- **Endpoint:** `https://$APP_SERVICE_URL/api/messages`
- **Bot ID:** `<YOUR_BOT_APP_ID>`
- **Connected to:**
  - Azure OpenAI (GPT-4.1) - Tool selection and response formatting
  - MCP Server - Court Listener API access with Dataverse caching
    - Endpoint: `https://<FUNCTION_APP>.azurewebsites.net/runtime/webhooks/mcp`
    - Authentication: `x-functions-key` header with `mcp_extension` system key
    - Protocol: JSON-RPC 2.0 (supports both JSON and SSE responses)

## Next Steps

- Proceed to [Teams App Registration](./teams-app-registration.md) to install the bot in Teams
