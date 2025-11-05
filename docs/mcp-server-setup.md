# MCP Server Deployment Guide

This guide walks you through deploying the Court Listener MCP Server to Azure Functions.

## Prerequisites

- Completed [Resource Identification](./resource-identification.md)
- Completed [Azure Setup](./azure-setup.md)
- Completed [Dataverse Setup](./dataverse-setup.md)
- .NET 8 SDK installed
- Azure Functions Core Tools installed

## 1. Configure Local Settings

### Get Court Listener API Key

The MCP server queries CourtListener.com for case information. In order to communicate with their API, you will need a key.

1. Go to [https://courtlistener.com/sign-in/](https://www.courtlistener.com/sign-in/) to create a free account.
2. Once you've logged in (you may need to confirm your email address), click on **Profile** in the top-right of the page, then **Account** on the drop-down.
3. Click on the **Developer Tools** tab.
4. Click on the **Your API Token** sub tab.
5. Copy the API token. (This will be your `CourtListener__ApiKey` in the next section.)

### Set configuration for server

Navigate to the MCP server directory:

```bash
cd mcp-teams/mcp-server
```

Update `local.settings.json` with your values:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "AzureWebJobsSecretStorageType": "files",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CourtListener__BaseUrl": "https://www.courtlistener.com/api/rest/v3/",
    "CourtListener__ApiKey": "",
    "Dataverse__Environment": "https://yourorg.crm.dynamics.com",
    "Dataverse__ClientId": "<YOUR_DATAVERSE_APP_ID>",
    "Dataverse__ClientSecret": "<YOUR_DATAVERSE_SECRET>",
    "Dataverse__TenantId": "<YOUR_TENANT_ID>",
    "Dataverse__TableName": "<YOUR_DATAVERSE_TABLE_SCHEMA_INCLUDING_PREFIX>",
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
  SearchOpinions: mcpToolTrigger
  GetOpinionDetails: mcpToolTrigger
  SearchDockets: mcpToolTrigger
  GetCourtInfo: mcpToolTrigger
```

> **Note:** The MCP tools use the `mcpToolTrigger` and are all accessible via the `/runtime/webhooks/mcp` endpoint provided by the Microsoft MCP extension. They won't show individual HTTP endpoints like traditional HTTP-triggered functions.

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
curl -X POST http://localhost:7071/runtime/webhooks/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
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
      "name": "CourtListenerMcpServer",
      "version": "1.0.0"
    }
  }
}
```

> **Note:** The MCP extension supports both JSON (`application/json`) and Server-Sent Events (`text/event-stream`) response formats. Include both in the Accept header for compatibility.

### Test a tool call

```bash
curl -X POST http://localhost:7071/runtime/webhooks/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
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
# Configure application settings
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "CourtListener__BaseUrl=https://www.courtlistener.com/api/rest/v3/" \
    "CourtListener__ApiKey='<YOUR_COURTLISTENER.COM_API_KEY>' \
    "Dataverse__Environment=$DATAVERSE_URL" \
    "Dataverse__ClientId=$DATAVERSE_APP_ID" \
    "Dataverse__ClientSecret=$DATAVERSE_APP_SECRET" \
    "Dataverse__TenantId=$TENANT_ID" \
    "Dataverse__TableName=$DATAVERSE_TABLE_SCHEMA" \
    "Cache__ExpirationDays=30"
```

### Publish the Function App

```bash
# From the mcp-server directory
func azure functionapp publish $FUNCTION_APP_NAME
```

Wait for the deployment to complete. You should see:

```
Deployment successful.
Remote build succeeded!
```

### Get the Function URL and Key

```bash
# Set the Function URL
FUNCTION_URL="https://$FUNCTION_APP_NAME.azurewebsites.net"
echo "Function URL: $FUNCTION_URL"

# Get the mcp_extension system key (required for MCP endpoint authentication)
# Note: This key is automatically created by the MCP extension
FUNCTION_KEY=$(az rest \
  --method post \
  --uri "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$FUNCTION_APP_NAME/host/default/listKeys?api-version=2022-03-01" \
  --query "systemKeys.mcp_extension" \
  --output tsv)

echo "MCP Extension Key: $FUNCTION_KEY"
```



**Save these values:**
- MCP Server Base URL: `https://$FUNCTION_APP_NAME.azurewebsites.net`
- MCP Endpoint: `https://$FUNCTION_APP_NAME.azurewebsites.net/runtime/webhooks/mcp`
- MCP Extension Key: `$FUNCTION_KEY`

> **Important:** The MCP endpoint uses the `mcp_extension` system key, NOT the default function key. This key is automatically created when the MCP extension initializes.

## 4. Test the Deployed Function

### Test health endpoint

```bash
curl "https://$FUNCTION_APP_NAME.azurewebsites.net/api/health"
```

### Test MCP endpoint with authentication

```bash
curl -X POST "https://$FUNCTION_APP_NAME.azurewebsites.net/runtime/webhooks/mcp" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "x-functions-key: $FUNCTION_KEY" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/list"
  }'
```

Expected response should list all 4 tools: `search_opinions`, `get_opinion_details`, `search_dockets`, `get_court_info`.

> **Note:** Authentication uses the `x-functions-key` header (not a query parameter) with the `mcp_extension` system key.

## 5. Monitor and Troubleshoot

### View logs

```bash
# Stream logs
az webapp log tail \
  --name $FUNCTION_APP_NAME \
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

> **Note:** Azure Key Vault now uses RBAC by default. If your Key Vault uses Access Policies, you can still use az keyvault set-policy, but RBAC is recommended.

```bash
# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Enable system-assigned managed identity for Function App
az functionapp identity assign \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the identity
IDENTITY=$(az functionapp identity show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# For your user account to manage secrets
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
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "DataverseClientSecret" --value $DATAVERSE_APP_SECRET
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "CourtListenerApiKey" --value "<YOUR_COURTLISTENER.COM_API_KEY>"

# Update app settings to reference Key Vault
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "Dataverse__ClientSecret=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/DataverseClientSecret/)" \
    "CourtListener__ApiKey=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/CourtListenerApiKey/)"
```

## Configuration Summary

Your MCP Server should now be deployed with:

- **Base URL:** `https://<FUNCTION_APP_NAME>.azurewebsites.net`
- **MCP Extension Key:** `<mcp_extension system key>`
- **Endpoints:**
  - Health: `GET /api/health` (no authentication)
  - MCP: `POST /runtime/webhooks/mcp` (requires `x-functions-key` header)
- **Authentication:** Header-based using `x-functions-key: <mcp_extension_key>`
- **Protocol Support:** JSON-RPC 2.0 over both `application/json` and `text/event-stream` (SSE)
- **Tools:**
  - `search_opinions` - Search court opinions by keywords, court, date range
  - `get_opinion_details` - Get detailed information about a specific opinion
  - `search_dockets` - Search dockets/cases by name, number, court
  - `get_court_info` - Get information about courts and jurisdictions
- **Caching:** Dataverse with 30-day TTL

### VS Code MCP Configuration

To use this MCP server in VS Code, add the following to your MCP configuration:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "mcpFunctionKey",
      "description": "Azure Function Key for Court Listener MCP Server",
      "password": true
    }
  ],
  "mcpServers": {
    "court-listener": {
      "url": "https://<FUNCTION_APP_NAME>.azurewebsites.net/runtime/webhooks/mcp",
      "headers": {
        "x-functions-key": "${input:mcpFunctionKey}"
      }
    }
  }
}
```

## Next Steps

- Proceed to [Teams Bot Deployment](./teams-bot-setup.md)
