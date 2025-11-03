# Resource Identification and Variables

Before beginning deployment, you'll define all resource names and variables that will be used throughout the deployment process. These variables
  will persist across your terminal session.

## Prerequisites

- Azure CLI installed and logged in
- Bash terminal (or Git Bash on Windows)

## Set Your Azure Context

```bash
# Login to Azure (if not already logged in)
az login

# List your subscriptions
az account list --output table

# Set your subscription (replace with your subscription ID)
SUBSCRIPTION_ID="<your-subscription-id>"
az account set --subscription $SUBSCRIPTION_ID

# Get your Tenant ID (save this)
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Tenant ID: $TENANT_ID"

# Get your User ID (for Key Vault permissions)
USER_ID=$(az ad signed-in-user show --query id -o tsv)
echo "User ID: $USER_ID"
```

## Define Resource Names

>**Important:** Some Azure resources require globally unique names (marked with 🌍). These include:  
>  - Storage accounts (lowercase, no hyphens, 3-24 characters)
> - Function apps
>  - App services
>  - Key vaults (3-24 characters)

Set all variables in your terminal:

```bash
# ===== LOCATION =====
LOCATION="eastus"

# ===== RESOURCE GROUP =====
RESOURCE_GROUP="rg-courtlistener-demo"

# ===== AZURE OPENAI =====
OPENAI_NAME="openai-courtlistener-demo"
OPENAI_DEPLOYMENT_NAME="gpt-4.1"
OPENAI_MODEL_NAME="gpt-4.1"
OPENAI_MODEL_VERSION="2025-04-14"

# ===== BOT SERVICE =====
BOT_NAME="bot-courtlistener-demo"
BOT_DISPLAY_NAME="CourtListenerBot"

# ===== STORAGE ACCOUNT (🌍 globally unique, lowercase, no hyphens) =====
STORAGE_ACCOUNT_NAME="stcourtlistenerdemo"

# ===== FUNCTION APP (🌍 globally unique) =====
FUNCTION_APP_NAME="func-courtlistener-demo"

# ===== APP SERVICE =====
APP_SERVICE_PLAN_NAME="plan-courtlistener-bot"
APP_SERVICE_NAME="app-courtlistener-demo"  # 🌍 globally unique

# ===== KEY VAULT (🌍 globally unique, 3-24 chars) =====
KEY_VAULT_NAME="kv-courtlistener"

# ===== DATAVERSE =====
DATAVERSE_ENV_DISPLAY_NAME="Court Listener Demo"
DATAVERSE_ENV_PURPOSE="Demo - Court Listener MCP Server Cache"
DATAVERSE_APP_DISPLAY_NAME="CourtListenerMcpServerDataverse"
DATAVERSE_TABLE_DISPLAY_NAME="Court Listener Cache"
DATAVERSE_TABLE_PLURAL_NAME="Court Listener Caches"
DATAVERSE_TABLE_NAME="courtlistenercache"
```

## Verify Variables

Print all variables to verify they're set correctly:

```bash
echo "=== Azure Context ==="
echo "Subscription ID: $SUBSCRIPTION_ID"
echo "Tenant ID: $TENANT_ID"
echo "User ID: $USER_ID"
echo "Location: $LOCATION"
echo ""
echo "=== Resource Names ==="
echo "Resource Group: $RESOURCE_GROUP"
echo "OpenAI: $OPENAI_NAME"
echo "Bot: $BOT_NAME"
echo "Storage Account: $STORAGE_ACCOUNT_NAME"
echo "Function App: $FUNCTION_APP_NAME"
echo "App Service Plan: $APP_SERVICE_PLAN_NAME"
echo "App Service: $APP_SERVICE_NAME"
echo "Key Vault: $KEY_VAULT_NAME"
echo "Dataverse Environment: $DATAVERSE_ENV_NAME"
echo "Dataverse Purpose: $DATAVERSE_ENV_PURPOSE"
echo "Dataverse App: $DATAVERSE_APP_DISPLAY_NAME"
echo "Dataverse Table Display Name: $DATAVERSE_TABLE_DISPLAY_NAME"
echo "Dataverse Table Display Name (Plural): $DATAVERSE_TABLE_PLURAL_NAME"
echo "Dataverse Table System Name: $DATAVERSE_TABLE_NAME"
```

## Check Name Availability

Before proceeding, verify that globally unique names are available:

```bash
# Check storage account name availability
az storage account check-name --name $STORAGE_ACCOUNT_NAME

# Check app service name availability
az webapp check-name --name $APP_SERVICE_NAME

# Check function app name availability
az functionapp check-name --name $FUNCTION_APP_NAME

# Check key vault name availability (returns true if available)
az keyvault list --query "[?name=='$KEY_VAULT_NAME'].name" -o tsv

# If any of these return empty, the name is available for the resource.
# If any names are unavailable (they return a value), modify the variable and re-run the echo commands above to verify.
```

Save Variables for Later Sessions (Optional)

If you need to close your terminal and resume later, save these variables to a file:

```bash
# Save to a file
cat > ~/courtlistener-vars.sh <<'EOF'
export SUBSCRIPTION_ID="<paste-your-value>"
export TENANT_ID="<paste-your-value>"
export USER_ID="<paste-your-value>"
export LOCATION="eastus"
export RESOURCE_GROUP="rg-courtlistener-demo"
export OPENAI_NAME="openai-courtlistener-demo"
export OPENAI_DEPLOYMENT_NAME="gpt-4.1"
export OPENAI_MODEL_NAME="gpt-4.1"
export OPENAI_MODEL_VERSION="2025-04-14"
export BOT_NAME="bot-courtlistener-demo"
export BOT_DISPLAY_NAME="CourtListenerBot"
export STORAGE_ACCOUNT_NAME="stcourtlistenerdemo"
export FUNCTION_APP_NAME="func-courtlistener-demo"
export APP_SERVICE_PLAN_NAME="plan-courtlistener-bot"
export APP_SERVICE_NAME="app-courtlistener-demo"
export KEY_VAULT_NAME="kv-courtlistener"
export DATAVERSE_ENV_NAME="courtlistener-demo"
export DATAVERSE_ENV_PURPOSE="Demo - Court Listener MCP Server Cache"
export DATAVERSE_APP_DISPLAY_NAME="CourtListenerMcpServerDataverse"
export DATAVERSE_TABLE_DISPLAY_NAME="Court Listener Cache"
export DATAVERSE_TABLE_PLURAL_NAME="Court Listener Caches"
export DATAVERSE_TABLE_NAME="courtlistenercache"
EOF
```

To reload in a new session:
```bash
source ~/courtlistener-vars.sh
```

## Variables Set During Deployment

The following variables will be set automatically as you progress through the deployment:

- `APP_ID` - Bot App Registration ID (set in azure-setup.md)
- `APP_SECRET` - Bot App Secret (set in azure-setup.md)
- `OPENAI_ENDPOINT` - Azure OpenAI endpoint URL (set in azure-setup.md)
- `OPENAI_API_KEY` - Azure OpenAI API key (set in azure-setup.md)
- `FUNCTION_URL` - Function App URL (set in mcp-server-setup.md)
- `FUNCTION_KEY` - Function App key (set in mcp-server-setup.md)
- `APP_SERVICE_URL` - App Service URL (set in teams-bot-setup.md)
- `DATAVERSE_URL` - Dataverse environment URL (set in dataverse-setup.md)
- `DATAVERSE_APP_ID` - Dataverse Service Principal App ID (set in dataverse-setup.md)
- `DATAVERSE_APP_SECRET` - Dataverse Service Principal Secret (set in dataverse-setup.md)
- `DATAVERSE_TABLE_SCHEMA` - Full Dataverse table name with prefix (set in dataverse-setup.md)
- `DATAVERSE_TABLE_SCHEMA` - Full Dataverse table name with auto-generated prefix (e.g., `cr3f3_courtlistenercache`) - set in dataverse-setup.md
- `IDENTITY` - Managed identity principal ID (set when creating managed identities)

## Next Steps

Proceed to [./azure-setup.md](./azure-setup.md) to begin creating Azure resources.

All subsequent commands will use these variables instead of hardcoded values.