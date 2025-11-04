# Quick Start Guide

Get up and running with the Court Listener MCP Teams Bot in 5 steps.

## Step 1: Resource Identification (10 minutes)

Follow [docs/resource-identification.md](./docs/resource-identification.md) to:
- Set all resource name variables
- Verify name availability
- Save variables for later use

## Step 2: Azure Resources (30 minutes)

Follow [docs/azure-setup.md](./docs/azure-setup.md) to create:
- Azure OpenAI (GPT-4.1)
- Bot Service registration
- Function App (MCP server)
- App Service (Teams bot)

**What you'll need:**
- Azure subscription
- Azure CLI installed

## Step 3: Dataverse Setup (15 minutes)

Follow [docs/dataverse-setup.md](./docs/dataverse-setup.md) to:
- Create Power Platform environment
- Create Service Principal
- Create custom cache table

**What you'll need:**
- Power Platform access
- PowerShell or Power Platform Admin Center

## Step 4: Deploy MCP Server (15 minutes)

Follow [docs/mcp-server-setup.md](./docs/mcp-server-setup.md) to:
- Configure local settings
- Test locally
- Deploy to Azure Functions
- Get the `mcp_extension` system key

**Commands:**
```bash
cd mcp-server
# Update local.settings.json with your values
dotnet restore
func start  # Test locally at /runtime/webhooks/mcp
func azure functionapp publish func-courtlistener-mcp  # Deploy

# Get the mcp_extension system key (NOT the default function key)
func keys list --system mcp_extension
```

**Note:** The MCP server uses:
- Microsoft MCP Extension (NuGet package)
- Extension bundle version `[4.0.0, 5.0.0)` in `host.json`
- Single endpoint `/runtime/webhooks/mcp` for all tools
- `mcp_extension` system key for authentication

## Step 5: Deploy Teams Bot (15 minutes)

Follow [docs/teams-bot-setup.md](./docs/teams-bot-setup.md) to:
- Configure app settings
- Test locally (optional)
- Deploy to Azure App Service

**Commands:**
```bash
cd teams-bot
# Update appsettings.json with your values
# IMPORTANT: Use the mcp_extension system key from Step 4
dotnet restore
dotnet run  # Test locally (optional)
dotnet publish -c Release -o ./publish
# Deploy to Azure
```

**Note:** The Teams bot configuration requires:
- `McpServer__BaseUrl`: Your Function App URL (e.g., `https://func-courtlistener-mcp.azurewebsites.net`)
- `McpServer__FunctionKey`: The `mcp_extension` system key from Step 4

## Step 6: Install in Teams (10 minutes)

Follow [docs/teams-app-registration.md](./docs/teams-app-registration.md) to:
- Create Teams app package
- Install in Microsoft Teams
- Test the bot

**Commands:**
```bash
cd teams-bot/TeamsAppManifest
# Update manifest.json with your App IDs
# Add color.png and outline.png icons
zip CourtListenerBot.zip manifest.json color.png outline.png
# Upload to Teams
```

## Total Time: ~90 minutes

## Quick Test

Once deployed, test in Teams:

1. Find "Court Listener Bot" in Teams
2. Send: **"Find Supreme Court opinions about privacy"**
3. Bot should return formatted results from Court Listener

## Troubleshooting

**Bot doesn't respond:**
- Check Azure App Service logs
- Verify messaging endpoint in Bot Service
- Test with Bot Framework Emulator

**MCP calls fail:**
- Test MCP server health endpoint: `curl https://func-courtlistener-mcp.azurewebsites.net/api/health`
- Verify you're using the `mcp_extension` system key, NOT the default function key
- Test MCP endpoint: `curl -X POST https://func-courtlistener-mcp.azurewebsites.net/runtime/webhooks/mcp -H "x-functions-key: YOUR_KEY" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'`
- Check Application Insights for errors
- Verify Accept headers include both `application/json` and `text/event-stream`

**Dataverse errors:**
- Verify Service Principal has Application User access
- Check table name matches code (`cr3f3_courtlistenercache`)
- Test Dataverse connection independently

## Next Steps

- Customize bot prompts in `AzureOpenAIService.cs`
- Add more MCP tools in `McpTools.cs` using `[McpToolTrigger]` attributes
- Monitor usage in Application Insights
- Share with your team
- Configure VS Code MCP integration for development

## Documentation

- **[README.md](./README.md)** - Full project overview
- **[docs/](./docs/)** - Detailed setup guides
- **[LICENSE](./LICENSE)** - MIT License

## Support

Review the detailed documentation in `/docs` for step-by-step instructions and troubleshooting.
