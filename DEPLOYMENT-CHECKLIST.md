# Deployment Checklist

Use this checklist to track your deployment progress.

## Prerequisites
- [ ] Azure subscription with appropriate permissions
- [ ] Azure CLI installed and configured
- [ ] .NET 8 SDK installed
- [ ] Azure Functions Core Tools installed
- [ ] Power Platform admin access
- [ ] Teams admin access (or custom app upload permission)

## 1. Azure Resources Setup

- [ ] Login to Azure CLI
- [ ] Create resource group: `rg-courtlistener-demo`
- [ ] Create Azure OpenAI resource
- [ ] Deploy GPT-4 model
- [ ] Save OpenAI endpoint and API key
- [ ] Create Bot Service app registration
- [ ] Save Bot App ID and secret
- [ ] Create Azure Function App for MCP server
- [ ] Save Function App URL
- [ ] Create Azure App Service for Teams bot
- [ ] Save App Service URL
- [ ] Update Bot messaging endpoint
- [ ] Enable Teams channel

**Documentation:** [docs/azure-setup.md](./docs/azure-setup.md)

## 2. Dataverse Setup

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
- [ ] Update code if prefix differs from `cr3f3_`

**Documentation:** [docs/dataverse-setup.md](./docs/dataverse-setup.md)

## 3. MCP Server Configuration

- [ ] Navigate to `mcp-server` directory
- [ ] Update `local.settings.json` with:
  - [ ] Dataverse environment URL
  - [ ] Dataverse Client ID
  - [ ] Dataverse Client Secret
  - [ ] Dataverse Tenant ID
  - [ ] Court Listener API key (optional)
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] Test locally with `func start`
- [ ] Test health endpoint: `http://localhost:7071/api/health`
- [ ] Test MCP initialize call
- [ ] Test MCP tools/list call
- [ ] Test a tool call (e.g., search_opinions)

**Documentation:** [docs/mcp-server-setup.md](./docs/mcp-server-setup.md)

## 4. MCP Server Deployment

- [ ] Configure Azure Function App settings via Azure CLI
- [ ] Run `func azure functionapp publish func-courtlistener-mcp`
- [ ] Get Function App default key
- [ ] Save Function URL and key
- [ ] Test deployed health endpoint
- [ ] Test deployed MCP endpoint with authentication
- [ ] Monitor logs for any errors
- [ ] (Optional) Configure Azure Key Vault for secrets

**Documentation:** [docs/mcp-server-setup.md](./docs/mcp-server-setup.md)

## 5. Teams Bot Configuration

- [ ] Navigate to `teams-bot` directory
- [ ] Update `appsettings.json` with:
  - [ ] Microsoft App ID (Bot)
  - [ ] Microsoft App Password (Bot)
  - [ ] Azure OpenAI endpoint
  - [ ] Azure OpenAI API key
  - [ ] Azure OpenAI deployment name
  - [ ] MCP Server base URL
  - [ ] MCP Server function key
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] (Optional) Test locally with `dotnet run`
- [ ] (Optional) Test with Bot Framework Emulator

**Documentation:** [docs/teams-bot-setup.md](./docs/teams-bot-setup.md)

## 6. Teams Bot Deployment

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

## 7. Teams App Package

- [ ] Navigate to `teams-bot/TeamsAppManifest`
- [ ] Generate new GUID for Teams App ID
- [ ] Update `manifest.json` with Teams App ID
- [ ] Update `manifest.json` with Bot App ID
- [ ] Create or obtain `color.png` (192x192)
- [ ] Create or obtain `outline.png` (32x32)
- [ ] Create zip package: `CourtListenerBot.zip`
- [ ] Verify zip contains files at root level

**Documentation:** [docs/teams-app-registration.md](./docs/teams-app-registration.md)

## 8. Teams Installation

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

## 9. Verification

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

## 10. Post-Deployment

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
- Endpoint: `_______________________________`
- API Key: `_______________________________`
- Deployment: `_______________________________`

**Bot Service:**
- App ID: `_______________________________`
- App Secret: `_______________________________`

**MCP Server:**
- Function URL: `_______________________________`
- Function Key: `_______________________________`

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
