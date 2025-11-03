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

**Commands:**
```bash
cd mcp-server
# Update local.settings.json with your values
dotnet restore
func start  # Test locally
func azure functionapp publish func-courtlistener-mcp  # Deploy
```

## Step 5: Deploy Teams Bot (15 minutes)

Follow [docs/teams-bot-setup.md](./docs/teams-bot-setup.md) to:
- Configure app settings
- Test locally (optional)
- Deploy to Azure App Service

**Commands:**
```bash
cd teams-bot
# Update appsettings.json with your values
dotnet restore
dotnet run  # Test locally (optional)
dotnet publish -c Release -o ./publish
# Deploy to Azure
```

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
- Test MCP server health endpoint
- Verify function key is correct
- Check Application Insights for errors

**Dataverse errors:**
- Verify Service Principal has Application User access
- Check table name matches code (`cr3f3_courtlistenercache`)
- Test Dataverse connection independently

## Next Steps

- Customize bot prompts in `AzureOpenAIService.cs`
- Add more MCP tools in `McpServerService.cs`
- Monitor usage in Application Insights
- Share with your team

## Documentation

- **[README.md](./README.md)** - Full project overview
- **[docs/](./docs/)** - Detailed setup guides
- **[LICENSE](./LICENSE)** - MIT License

## Support

Review the detailed documentation in `/docs` for step-by-step instructions and troubleshooting.
