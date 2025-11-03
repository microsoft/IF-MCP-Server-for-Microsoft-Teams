# Court Listener MCP Teams Bot

A demonstration project showing how to build a Microsoft Teams bot using the Model Context Protocol (MCP) to provide legal research capabilities through the Court Listener API.

## Overview

This project demonstrates:

- Building a custom **MCP server** as an Azure Function
- Integrating the **Court Listener REST API** for case law research
- Using **Microsoft Dataverse** for response caching
- Creating a **Teams bot** with Bot Framework SDK
- Leveraging **Azure OpenAI** for natural language understanding
- Connecting MCP clients to standalone MCP servers

>**Important:** This demo uses the Microsoft Bot Framework which is scheduled for end of support on December 31, 2025. You can use this demo beyond that date, but understand that this demo is not intended to be used in production for a long period of time. The demo exists to merely show the concepts of architecture and functionality. For production usage, the dependencies of the _web application_ (Teams Bot) should be updated to use the Microsoft Agents SDK. Once the end-of-support date has been reached, we will update the repo with the necessary changes that leverage the Microsoft Agents SDK.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                      Microsoft Teams                              │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                   Teams Bot (Azure App Service)                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Bot Framework SDK                                         │  │
│  │  - Receives user messages                                  │  │
│  │  - Routes to Azure OpenAI for intent understanding         │  │
│  │  - Calls MCP server with determined tool & params          │  │
│  │  - Formats and returns results to Teams                    │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  ┌─────────────────┐          ┌────────────────────────────────┐ │
│  │ Azure OpenAI    │          │    MCP Client                  │ │
│  │ (GPT-4.1)       │          │    - HTTP/JSON-RPC protocol    │ │
│  │                 │          │    - Tool invocation           │ │
│  └─────────────────┘          └────────────────────────────────┘ │
└────────────────────────────────────────┬─────────────────────────┘
                                         │ HTTP/JSON-RPC
                                         ▼
┌──────────────────────────────────────────────────────────────────┐
│              MCP Server (Azure Functions)                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  MCP Protocol Implementation                               │  │
│  │  - initialize, tools/list, tools/call                      │  │
│  │                                                            │  │
│  │  MCP Tools:                                                │  │
│  │  • search_opinions - Search court opinions                │  │
│  │  • get_opinion_details - Get specific opinion             │  │
│  │  • search_dockets - Search dockets/cases                  │  │
│  │  • get_court_info - Get court information                 │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  ┌──────────────────────┐      ┌───────────────────────────────┐ │
│  │ Dataverse Cache      │      │ Court Listener Client         │ │
│  │ - 30-day TTL         │      │ - REST API integration        │ │
│  │ - SHA-256 keys       │      │                               │ │
│  └──────────────────────┘      └───────────────────────────────┘ │
└────────────────────────┬───────────────────────┬─────────────────┘
                         │                       │
                         ▼                       ▼
             ┌────────────────────┐   ┌──────────────────────────┐
             │ Microsoft Dataverse│   │  Court Listener API      │
             │ (Cache Storage)    │   │  courtlistener.com       │
             └────────────────────┘   └──────────────────────────┘
```

## Key Components

### 1. MCP Server (`/mcp-server`)
- **Technology:** C# Azure Functions (.NET 8)
- **Purpose:** Standalone MCP server exposing Court Listener API as MCP tools
- **Features:**
  - MCP protocol implementation (JSON-RPC 2.0)
  - 4 MCP tools for legal research
  - Dataverse integration for caching (30-day TTL)
  - Service Principal authentication
  - Health monitoring endpoint

### 2. Teams Bot (`/teams-bot`)
- **Technology:** C# ASP.NET Core with Bot Framework SDK (.NET 8)
- **Purpose:** Teams interface for legal research
- **Features:**
  - Bot Framework integration
  - Azure OpenAI (GPT-4.1) for intent recognition
  - MCP client for calling the MCP server
  - Natural language query processing
  - Formatted response generation

### 3. Microsoft Dataverse
- **Purpose:** Response caching layer
- **Features:**
  - Custom table for cache entries
  - SHA-256 hashed cache keys
  - Automatic expiration (30 days)
  - Service Principal access

### 4. Azure OpenAI
- **Purpose:** Natural language understanding
- **Features:**
  - Intent detection from user queries
  - Tool and parameter extraction
  - Response formatting for Teams
  - GPT-4.1 deployment

## Project Structure

```
mcp-teams/
├── mcp-server/                 # MCP Server (Azure Functions)
│   ├── Models/
│   │   ├── McpModels.cs       # MCP protocol models
│   │   └── CourtListenerModels.cs
│   ├── Services/
│   │   ├── CourtListenerClient.cs  # Court Listener API client
│   │   ├── DataverseCache.cs       # Dataverse caching service
│   │   └── McpServerService.cs     # MCP protocol implementation
│   ├── McpFunctions.cs             # Azure Functions HTTP endpoints
│   ├── Program.cs
│   ├── host.json
│   └── local.settings.json
│
├── teams-bot/                  # Teams Bot
│   ├── Bots/
│   │   └── CourtListenerBot.cs     # Main bot logic
│   ├── Services/
│   │   ├── McpClient.cs            # MCP client implementation
│   │   └── AzureOpenAIService.cs   # OpenAI integration
│   ├── Controllers/
│   │   └── BotController.cs
│   ├── TeamsAppManifest/
│   │   └── manifest.json           # Teams app package
│   ├── Program.cs
│   └── appsettings.json
│
├── docs/                       # Documentation
│   ├── azure-setup.md          # Azure resources setup
│   ├── dataverse-setup.md      # Dataverse configuration
│   ├── mcp-server-setup.md     # MCP server deployment
│   ├── teams-bot-setup.md      # Teams bot deployment
│   └── teams-app-registration.md # Teams app installation
│
├── README.md                   # This file
└── LICENSE                     # MIT License
```

## Prerequisites

- Azure subscription
- .NET 8 SDK
- Azure Functions Core Tools
- Azure CLI
- Power Platform environment (with associated Azure subscription)
- Microsoft Teams admin access (or custom app upload permission)

## Getting Started

Deploying the demo requires approximately **90 minutes**.

You can view the [Quick Start Guide](./QUICKSTART.md) for faster reference. However, for your first deployment, we encourage you to follow the setup guides in order:

1. **[Resource Identification](./docs/resource-identification.md)** (10 Minutes)
    - Set all resource name variables
    - Verify name availability
    - Save variables for later use

2. **[Azure Setup](./docs/azure-setup.md)** (30 Minutes)
   - Create Azure OpenAI resource
   - Create Bot Service registration
   - Create Function App for MCP server
   - Create App Service for Teams bot

3. **[Dataverse Setup](./docs/dataverse-setup.md)** (15 Minutes)
   - Create Power Platform environment
   - Configure Service Principal access
   - Create custom cache table

4. **[MCP Server Deployment](./docs/mcp-server-setup.md)** (15 Minutes)
   - Configure and test locally
   - Deploy to Azure Functions
   - Verify endpoints

5. **[Teams Bot Deployment](./docs/teams-bot-setup.md)** (15 Minutes)
   - Configure and test locally
   - Deploy to Azure App Service
   - Configure bot endpoint

6. **[Teams App Registration](./docs/teams-app-registration.md)** (10 Minutes)
   - Create Teams app package
   - Install in Microsoft Teams
   - Test the bot

## MCP Tools

The MCP server exposes four tools:

### 1. `search_opinions`
Search for court opinions by keywords, court, date range.

**Parameters:**
- `q` (string): Search query
- `court` (string): Court ID (e.g., "scotus", "ca9")
- `filed_after` (string): Date filter (YYYY-MM-DD)
- `filed_before` (string): Date filter (YYYY-MM-DD)
- `order_by` (string): Sort field

**Example:**
```json
{
  "name": "search_opinions",
  "arguments": {
    "q": "copyright",
    "court": "scotus"
  }
}
```

### 2. `get_opinion_details`
Get detailed information about a specific opinion.

**Parameters:**
- `opinion_id` (integer): The opinion ID

### 3. `search_dockets`
Search for dockets/cases.

**Parameters:**
- `q` (string): Search query
- `court` (string): Court ID
- `docket_number` (string): Docket number
- `case_name` (string): Case name
- `filed_after` (string): Date filter
- `filed_before` (string): Date filter

### 4. `get_court_info`
Get information about courts.

**Parameters:**
- `court_id` (string, optional): Specific court ID
- `jurisdiction` (string, optional): Filter by jurisdiction
- `in_use` (boolean, optional): Filter by active status

## Usage Examples

### In Microsoft Teams

**User:** "Find Supreme Court opinions about privacy from 2023"

**Bot Response:**
```
I found 5 Supreme Court opinions about privacy from 2023:

1. Smith v. United States (2023)
   Court: Supreme Court of the United States
   Filed: March 15, 2023
   View: [Link to opinion]

2. Johnson v. California (2023)
   Court: Supreme Court of the United States
   Filed: June 22, 2023
   View: [Link to opinion]

...
```

### Common Court IDs

- `scotus` - Supreme Court of the United States
- `ca1` through `ca11` - Federal Circuit Courts (1st-11th)
- `cadc` - D.C. Circuit
- `cafc` - Federal Circuit
- `dcd` - D.C. District Court
- `nysd` - Southern District of New York
- `cand` - Northern District of California

## Development

### Local Development - MCP Server

```bash
cd mcp-server
dotnet restore
dotnet build
func start
```

Test with:
```bash
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
```

### Local Development - Teams Bot

```bash
cd teams-bot
dotnet restore
dotnet build
dotnet run
```

Test with Bot Framework Emulator at `http://localhost:5000/api/messages`

## Deployment

### Deploy MCP Server
```bash
cd mcp-server
func azure functionapp publish func-courtlistener-mcp
```

### Deploy Teams Bot
```bash
cd teams-bot
dotnet publish -c Release
# Then deploy via Azure CLI or Visual Studio
```

## Configuration

### MCP Server (Azure Functions)
```json
{
  "CourtListener__BaseUrl": "https://www.courtlistener.com/api/rest/v3/",
  "CourtListener__ApiKey": "",
  "Dataverse__Environment": "https://yourorg.crm.dynamics.com",
  "Dataverse__ClientId": "<app-id>",
  "Dataverse__ClientSecret": "<secret>",
  "Dataverse__TenantId": "<tenant-id>",
  "Dataverse__TableName": "<table-name-including-prefix>"
  "Cache__ExpirationDays": "30"
}
```

### Teams Bot (App Service)
```json
{
  "MicrosoftAppId": "<bot-app-id>",
  "MicrosoftAppPassword": "<bot-secret>",
  "MicrosoftAppTenantId": "<tenant-id>",
  "MicrosoftAppType": "SingleTenant",
  "AzureOpenAI__Endpoint": "https://yourservice.openai.azure.com/",
  "AzureOpenAI__ApiKey": "<openai-key>",
  "AzureOpenAI__DeploymentName": "gpt-4.1",
  "McpServer__BaseUrl": "https://func-courtlistener-mcp.azurewebsites.net",
  "McpServer__FunctionKey": "<function-key>"
}
```

## Monitoring

### Application Insights
- MCP server logs and metrics
- Teams bot logs and metrics
- Performance tracking
- Error monitoring

### Logs
```bash
# MCP Server
az webapp log tail --name func-courtlistener-mcp --resource-group rg-courtlistener-demo

# Teams Bot
az webapp log tail --name app-courtlistener-bot --resource-group rg-courtlistener-demo
```

## Security

- **Service Principal Authentication** for Dataverse
- **Azure Key Vault** for secrets (recommended for production)
- **HTTPS only** enforcement
- **Function keys** for MCP server access
- **Bot Framework authentication** for Teams

## Why MCP?

This demo showcases MCP as a **standalone service architecture**:

1. **Reusability**: The MCP server can be consumed by:
   - Teams bots
   - Copilot Studio
   - Desktop MCP clients
   - Other applications

2. **Separation of Concerns**:
   - MCP server: API integration + caching
   - Teams bot: User interface + AI orchestration

3. **Scalability**: Independent scaling of components

4. **Maintainability**: Update API integration without touching the bot

## Extending the Demo

### Add More Tools

1. Add new methods to `McpServerService.cs`
2. Register tools in `HandleToolsList()`
3. Implement tool logic in `HandleToolsCallAsync()`

### Add More Clients

1. **Copilot Studio**: Connect to MCP server via custom connector
2. **Power Apps**: Call MCP server via Power Automate
3. **Desktop App**: Use MCP client library

### Enhance Caching

- Add cache invalidation API
- Implement cache warming
- Add cache analytics

### Improve AI

- Fine-tune prompts for better intent detection
- Add multi-turn conversations
- Implement clarifying questions

## Troubleshooting

See individual setup guides for detailed troubleshooting:
- [Resource Identification](./docs/resource-identification.md)
- [Azure Setup Issues](./docs/azure-setup.md)
- [MCP Server Issues](./docs/mcp-server-setup.md)
- [Teams Bot Issues](./docs/teams-bot-setup.md)
- [Teams App Issues](./docs/teams-app-registration.md)

## Resources

- [Court Listener API Documentation](https://www.courtlistener.com/help/api/)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Bot Framework Documentation](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Microsoft Dataverse Documentation](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

This is a demonstration project. Feel free to fork and adapt for your own use cases.

## Support

For issues or questions:

- Review the documentation in `/docs`
- Check Azure resource configurations
- Review Application Insights logs
- Test components independently

## Acknowledgments

- [Court Listener](https://www.courtlistener.com/) for providing the free legal research API
- [Model Context Protocol](https://modelcontextprotocol.io/) for the protocol specification
- Microsoft for Azure, Teams, and Bot Framework platforms
