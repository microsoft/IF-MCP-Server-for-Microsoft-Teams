# Deployment Checklist

Use this checklist to track your deployment progress.

## Prerequisites

- [ ] Azure subscription with appropriate permissions
- [ ] Azure CLI installed and configured
- [ ] .NET 8 SDK installed
- [ ] Azure Functions Core Tools installed
- [ ] Power Platform admin access
- [ ] Teams admin access (or custom app upload permission)

## 1. Resource Identification

- [ ] Complete [Resource Identification](./docs/resource-identification.md)
- [ ] Set all resource name variables in your terminal
- [ ] Verify globally unique names are available
- [ ] (Optional) Save variables to a file for later sessions

**Documentation:** [docs/resource-identification.md](./docs/resource-identification.md)

### Variable Reference

  All variables used throughout deployment:

  | Variable | Set In | Purpose |
  |----------|--------|---------|
  | `SUBSCRIPTION_ID` | [Resource Identification](./docs/resource-identification.md#set-your-azure-context) | Your Azure subscription |
  | `TENANT_ID` | [Resource Identification](./docs/resource-identification.md#set-your-azure-context) | Azure AD tenant ID |
  | `USER_ID` | [Resource Identification](./docs/resource-identification.md#set-your-azure-context) | Your user principal ID |
  | `LOCATION` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Azure region |
  | `RESOURCE_GROUP` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Resource group name |
  | `OPENAI_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Azure OpenAI resource name |
  | `OPENAI_DEPLOYMENT_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | OpenAI deployment name |
  | `BOT_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Bot resource name |
  | `BOT_DISPLAY_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Bot display name |
  | `STORAGE_ACCOUNT_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Storage account (globally unique) |
  | `FUNCTION_APP_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Function App (globally unique) |
  | `APP_SERVICE_PLAN_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | App Service Plan name |
  | `APP_SERVICE_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | App Service (globally unique) |
  | `KEY_VAULT_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Key Vault (globally unique) |
  | `DATAVERSE_ENV_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Dataverse environment name |
  | `APP_ID` | [Azure Setup](./docs/azure-setup.md#create-app-registration) | Bot App Registration ID |
  | `APP_SECRET` | [Azure Setup](./docs/azure-setup.md#create-app-registration) | Bot App Secret |
  | `OPENAI_ENDPOINT` | [Azure Setup](./docs/azure-setup.md#get-the-endpoint-and-key) | Azure OpenAI endpoint |
  | `OPENAI_API_KEY` | [Azure Setup](./docs/azure-setup.md#get-the-endpoint-and-key) | Azure OpenAI API key |
  | `FUNCTION_URL` | [MCP Server Setup](./docs/mcp-server-setup.md#get-the-function-url-and-key) | Function App base URL |
  | `FUNCTION_KEY` | [MCP Server Setup](./docs/mcp-server-setup.md#get-the-function-url-and-key) | MCP extension system key |
  | `DATAVERSE_ENV_DISPLAY_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Dataverse environment name |
  | `DATAVERSE_APP_DISPLAY_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Dataverse app registration name |
  | `DATAVERSE_TABLE_DISPLAY_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Dataverse table display name |
  | `DATAVERSE_TABLE_NAME` | [Resource Identification](./docs/resource-identification.md#define-resource-names) | Dataverse table schema name |
  | `DATAVERSE_URL` | [Dataverse Setup](./docs/dataverse-setup.md#get-dataverse-environment-url) | Dataverse environment URL |
  | `DATAVERSE_APP_ID` | [Dataverse Setup](./docs/dataverse-setup.md#create-app-registration) | Service Principal App ID |
  | `DATAVERSE_APP_SECRET` | [Dataverse Setup](./docs/dataverse-setup.md#create-app-registration) | Service Principal Secret |
  | `DATAVERSE_TABLE_SCHEMA` | [Dataverse Setup](./docs/dataverse-setup.md#verify-table-schema-name) | Full table name with prefix |
  | `DATAVERSE_TABLE_PREFIX` | [Dataverse Setup](./docs/dataverse-setup.md#verify-table-schema-name) | Auto-generated table prefix |

## 2. Azure Resources Setup

- [ ] Login to Azure CLI
- [ ] Create resource group: `rg-courtlistener-demo`
- [ ] Create Azure OpenAI resource
- [ ] Deploy GPT-4.1 model
- [ ] Save OpenAI endpoint (regional format, e.g., https://eastus.api.cognitive.microsoft.com/) and API key
- [ ] Create Bot Service app registration
- [ ] Save Bot App ID and secret
- [ ] Add Microsoft Graph API permissions to Bot App Registration
- [ ] Grant admin consent for API permissions
- [ ] Save Tenant ID
- [ ] Create Azure Function App for MCP server
- [ ] Save Function App URL
- [ ] Create Azure App Service for Teams bot
- [ ] Save App Service URL
- [ ] Update Bot messaging endpoint
- [ ] Enable Teams channel

**Documentation:** [docs/azure-setup.md](./docs/azure-setup.md)

## 3. Dataverse Setup

- [ ] Create Power Platform environment
- [ ] Save Dataverse environment URL
- [ ] Create app registration for Dataverse access
- [ ] Save Dataverse App ID, secret, and tenant ID
- [ ] Grant app registration as Application User in Dataverse
- [ ] Create custom table: `courtlistenercache`
- [ ] Add column: `cachekeyhash` (text, required)
- [ ] Add column: `cachekey` (multiline text, optional)
- [ ] Add column: `responsedata` (multiline text, required)
- [ ] Add column: `expirationdate` (datetime, required)
- [ ] Verify table schema name (e.g., `cr3f3_courtlistenercache`)
- [ ] Set `DATAVERSE_TABLE_SCHEMA` variable with full table name including prefix

**Documentation:** [docs/dataverse-setup.md](./docs/dataverse-setup.md)

## 4. MCP Server Configuration

- [ ] Navigate to `mcp-server` directory
- [ ] Update `local.settings.json` with:
  - [ ] Dataverse environment URL
  - [ ] Dataverse Client ID
  - [ ] Dataverse Client Secret
  - [ ] Dataverse Tenant ID
  - [ ] Dataverse Table Name (with prefix)
  - [ ] Court Listener API key (optional)
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] Test locally with `func start`
- [ ] Test health endpoint: `http://localhost:7071/api/health`
- [ ] Test MCP initialize call on `/runtime/webhooks/mcp` endpoint
- [ ] Test MCP tools/list call
- [ ] Test a tool call (e.g., search_opinions)

**Documentation:** [docs/mcp-server-setup.md](./docs/mcp-server-setup.md)

## 5. MCP Server Deployment

- [ ] Configure Azure Function App settings via Azure CLI
- [ ] Run `func azure functionapp publish func-courtlistener-mcp`
- [ ] Get the `mcp_extension` system key (NOT the default function key)
- [ ] Save Function URL and `mcp_extension` key
- [ ] Test deployed health endpoint
- [ ] Test deployed MCP endpoint (`/runtime/webhooks/mcp`) with `x-functions-key` header authentication
- [ ] Monitor logs for any errors
- [ ] (Optional) Configure Azure Key Vault for secrets

**Documentation:** [docs/mcp-server-setup.md](./docs/mcp-server-setup.md)

**Important:** The MCP endpoint uses `/runtime/webhooks/mcp` and requires the `mcp_extension` system key for authentication (passed via `x-functions-key` header).

## 6. Teams Bot Configuration

- [ ] Navigate to `teams-bot` directory
- [ ] Update `appsettings.json` with:
  - [ ] Microsoft App ID (Bot)
  - [ ] Microsoft App Password (Bot)
  - [ ] Microsoft App Tenant ID
  - [ ] Microsoft App Type (SingleTenant)
  - [ ] Azure OpenAI endpoint
  - [ ] Azure OpenAI API key
  - [ ] Azure OpenAI deployment name
  - [ ] MCP Server base URL
  - [ ] MCP Server `mcp_extension` system key (from step 5)
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] (Optional) Test locally with `dotnet run`
- [ ] (Optional) Test with Bot Framework Emulator

**Documentation:** [docs/teams-bot-setup.md](./docs/teams-bot-setup.md)

## 7. Teams Bot Deployment

- [ ] Configure Azure App Service settings via Azure CLI
- [ ] Build and publish: `dotnet publish -c Release`
- [ ] Deploy to Azure App Service
- [ ] Verify deployment successful
- [ ] Update Bot messaging endpoint in Azure Portal
- [ ] Test endpoint accessibility
- [ ] Monitor logs for any errors
- [ ] (Optional) Configure Azure Key Vault for secrets
- [ ] Enable HTTPS only

**Documentation:** [docs/teams-bot-setup.md](./docs/teams-bot-setup.md)

## 8. Teams App Package

- [ ] Navigate to `teams-bot/TeamsAppManifest`
- [ ] Generate new GUID for Teams App ID
- [ ] Update `manifest.json` with Teams App ID
- [ ] Update `manifest.json` with Bot App ID
- [ ] Create or obtain `color.png` (192x192)
- [ ] Create or obtain `outline.png` (32x32)
- [ ] Create zip package: `CourtListenerBot.zip`
- [ ] Verify zip contains files at root level

**Documentation:** [docs/teams-app-registration.md](./docs/teams-app-registration.md)

## 9. Teams Installation

- [ ] Open Microsoft Teams
- [ ] Navigate to Apps > Manage your apps
- [ ] Upload custom app
- [ ] Select `CourtListenerBot.zip`
- [ ] Install the app
- [ ] Open chat with the bot
- [ ] Send "Hello" and verify welcome message
- [ ] Test query: "Find Supreme Court opinions about privacy"
- [ ] Verify bot responds with formatted results
- [ ] (Optional) Add bot to a team channel

**Documentation:** [docs/teams-app-registration.md](./docs/teams-app-registration.md)

## 10. Verification

- [ ] MCP server health endpoint responds
- [ ] MCP server tools list returns 4 tools
- [ ] Teams bot responds to messages
- [ ] Azure OpenAI processes user queries
- [ ] MCP client successfully calls MCP server
- [ ] Court Listener API returns results
- [ ] Dataverse cache stores responses
- [ ] Cached responses are retrieved on repeat queries
- [ ] No errors in Application Insights
- [ ] Bot handles errors gracefully

## 11. Post-Deployment

- [ ] Share bot with team members
- [ ] Monitor Application Insights dashboards
- [ ] Review logs for optimization opportunities
- [ ] Gather user feedback
- [ ] Document any custom configurations
- [ ] Create backup of configuration values
- [ ] (Optional) Set up alerts in Azure Monitor
- [ ] (Optional) Configure auto-scaling for production

## Configuration Values Reference

Keep track of your configuration values:

**Azure OpenAI:**
- Endpoint (regional): `_______________________________`  
  (Format: https://{region}.api.cognitive.microsoft.com/)
- API Key: `_______________________________`
- Deployment: `_______________________________`

**Bot Service:**
- App ID: `_______________________________`
- App Secret: `_______________________________`
- Tenant ID: `_______________________________`

**MCP Server:**
- Function Base URL: `_______________________________`
- MCP Endpoint: `_______________________________/runtime/webhooks/mcp`
- MCP Extension Key (system key): `_______________________________`

**Dataverse:**
- Environment URL: `_______________________________`
- Client ID: `_______________________________`
- Client Secret: `_______________________________`
- Tenant ID: `_______________________________`
- Table Name: `_______________________________`

**Teams App:**
- Teams App ID: `_______________________________`

## Troubleshooting

If you encounter issues:
1. Review the specific documentation for that component
2. Check Azure Portal for resource status
3. Review Application Insights logs
4. Test components independently
5. Verify all configuration values are correct

## Success Criteria

You've successfully deployed when:
- ✅ Bot responds in Teams
- ✅ Queries return Court Listener results
- ✅ Responses are formatted clearly
- ✅ Cache reduces repeat query latency
- ✅ No errors in logs

## Next Steps

- Customize bot prompts and responses
- Add more MCP tools
- Integrate with Copilot Studio
- Train team on usage
- Monitor and optimize performance
