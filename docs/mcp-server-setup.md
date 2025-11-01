# MCP Server Deployment Guide

This guide walks you through deploying the Court Listener MCP Server to Azure Functions.

## Prerequisites

- Completed [Azure Setup](./azure-setup.md)
- Completed [Dataverse Setup](./dataverse-setup.md)
- .NET 8 SDK installed
- Azure Functions Core Tools installed

## 1. Configure Local Settings

Navigate to the MCP server directory:

```bash
cd mcp-teams/mcp-server
```

Update `local.settings.json` with your values:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CourtListener__BaseUrl": "https://www.courtlistener.com/api/rest/v3/",
    "CourtListener__ApiKey": "",
    "Dataverse__Environment": "https://yourorg.crm.dynamics.com",
    "Dataverse__ClientId": "<YOUR_DATAVERSE_APP_ID>",
    "Dataverse__ClientSecret": "<YOUR_DATAVERSE_SECRET>",
    "Dataverse__TenantId": "<YOUR_TENANT_ID>",
    "Cache__ExpirationDays": "30"
  }
}
```

> **Note:** Court Listener API key is optional for read-only operations but recommended to avoid rate limiting.

## 2. Test Locally

### Restore packages and build

```bash
dotnet restore
dotnet build
```

### Run locally

```bash
func start
```

You should see:

```
Functions:
  health: [GET] http://localhost:7071/api/health
  mcp: [POST] http://localhost:7071/api/mcp
```

### Test the health endpoint

```bash
curl http://localhost:7071/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-20T...",
  "service": "court-listener-mcp-server"
}
```

### Test MCP endpoint

```bash
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "initialize"
  }'
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {}
    },
    "serverInfo": {
      "name": "court-listener-mcp-server",
      "version": "1.0.0"
    }
  }
}
```

### Test a tool call

```bash
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "2",
    "method": "tools/call",
    "params": {
      "name": "search_opinions",
      "arguments": {
        "q": "copyright",
        "court": "scotus"
      }
    }
  }'
```

## 3. Deploy to Azure

### Configure Azure Function App Settings

```bash
# Set the Function App name
FUNC_APP_NAME="func-courtlistener-mcp"
RESOURCE_GROUP="rg-courtlistener-demo"

# Configure application settings
az functionapp config appsettings set \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "CourtListener__BaseUrl=https://www.courtlistener.com/api/rest/v3/" \
    "CourtListener__ApiKey=" \
    "Dataverse__Environment=https://yourorg.crm.dynamics.com" \
    "Dataverse__ClientId=<YOUR_DATAVERSE_APP_ID>" \
    "Dataverse__ClientSecret=<YOUR_DATAVERSE_SECRET>" \
    "Dataverse__TenantId=<YOUR_TENANT_ID>" \
    "Cache__ExpirationDays=30"
```

### Publish the Function App

```bash
# From the mcp-server directory
func azure functionapp publish $FUNC_APP_NAME
```

Wait for the deployment to complete. You should see:

```
Deployment successful.
Remote build succeeded!
```

### Get the Function URL and Key

```bash
# Get the default host key (function key)
FUNCTION_KEY=$(az functionapp keys list \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "functionKeys.default" \
  --output tsv)

echo "Function Key: $FUNCTION_KEY"

# Get the Function App URL
FUNCTION_URL="https://$FUNC_APP_NAME.azurewebsites.net"
echo "Function URL: $FUNCTION_URL"
```

**Save these values:**
- MCP Server URL: `https://func-courtlistener-mcp.azurewebsites.net`
- Function Key: `<your-function-key>`

## 4. Test the Deployed Function

### Test health endpoint

```bash
curl "https://$FUNC_APP_NAME.azurewebsites.net/api/health"
```

### Test MCP endpoint with authentication

```bash
curl -X POST "https://$FUNC_APP_NAME.azurewebsites.net/api/mcp?code=$FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/list"
  }'
```

Expected response should list all 4 tools: `search_opinions`, `get_opinion_details`, `search_dockets`, `get_court_info`.

## 5. Monitor and Troubleshoot

### View logs

```bash
# Stream logs
az functionapp log tail \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

### Check Application Insights

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Function App
3. Click **Application Insights** in the left menu
4. View logs, metrics, and traces

### Common Issues

**Issue:** Dataverse connection fails
- **Solution:** Verify the Service Principal has Application User access in Dataverse
- Check the Client ID, Secret, and Tenant ID are correct
- Ensure the Dataverse environment URL is correct

**Issue:** Court Listener API rate limiting
- **Solution:** Add an API key to the configuration
- Visit [Court Listener](https://www.courtlistener.com/) to sign up for an API key

**Issue:** Function timeout
- **Solution:** Increase the function timeout in `host.json`
- Consider implementing request queuing for long-running queries

## 6. Security Best Practices

### Use Azure Key Vault (Recommended for Production)

```bash
# Create Key Vault
az keyvault create \
  --name kv-courtlistener \
  --resource-group $RESOURCE_GROUP \
  --location eastus

# Enable system-assigned managed identity for Function App
az functionapp identity assign \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the identity
IDENTITY=$(az functionapp identity show \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant access to Key Vault
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee $IDENTITY \
    --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/kv-courtlistener"

# Store secrets
az keyvault secret set --vault-name kv-courtlistener --name "DataverseClientSecret" --value "<YOUR_SECRET>"
az keyvault secret set --vault-name kv-courtlistener --name "CourtListenerApiKey" --value "<YOUR_KEY>"

# Update app settings to reference Key Vault
az functionapp config appsettings set \
  --name $FUNC_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "Dataverse__ClientSecret=@Microsoft.KeyVault(SecretUri=https://kv-courtlistener.vault.azure.net/secrets/DataverseClientSecret/)" \
    "CourtListener__ApiKey=@Microsoft.KeyVault(SecretUri=https://kv-courtlistener.vault.azure.net/secrets/CourtListenerApiKey/)"
```

## Configuration Summary

Your MCP Server should now be deployed with:

- **URL:** `https://func-courtlistener-mcp.azurewebsites.net`
- **Function Key:** `<saved>`
- **Endpoints:**
  - Health: `GET /api/health`
  - MCP: `POST /api/mcp?code=<function-key>`
- **Tools:**
  - `search_opinions`
  - `get_opinion_details`
  - `search_dockets`
  - `get_court_info`
- **Caching:** Dataverse with 30-day TTL

## Next Steps

- Proceed to [Teams Bot Deployment](./teams-bot-setup.md)
