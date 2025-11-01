# Dataverse Setup Guide

This guide walks you through setting up Microsoft Dataverse for caching Court Listener API responses.

## Prerequisites

- Azure subscription
- Power Platform admin access
- Completed [Azure Setup](./azure-setup.md)

## 1. Create Power Platform Environment

### Option A: Using Power Platform Admin Center (Recommended)

1. Go to [Power Platform Admin Center](https://admin.powerplatform.microsoft.com/)
2. Click **Environments** > **+ New**
3. Configure the environment:
   - **Name:** `Court Listener Demo`
   - **Type:** `Sandbox` (or `Production` for production use)
   - **Region:** Choose the same region as your Azure resources (e.g., United States)
   - **Purpose:** `Demo - Court Listener MCP Server Cache`
   - **Create a database:** `Yes`
   - **Currency:** USD
   - **Language:** English
   - **Add a Dataverse data store?** Enable
   - Choose Billing
4. Click **Next**
5. **Security Group**: Open access - None (or choose a specific user)
6. Click **Save**
7. Wait for the environment to be created (may take a few minutes)

### Option B: Using PowerShell

```powershell
# Install the Power Platform admin module
Install-Module -Name Microsoft.PowerApps.Administration.PowerShell

# Connect to Power Platform
Add-PowerAppsAccount

# Create environment with Dataverse
New-AdminPowerAppEnvironment `
  -DisplayName "Court Listener Demo" `
  -LocationName "unitedstates" `
  -EnvironmentSku Trial `
  -ProvisionDatabase
```

## 2. Get Dataverse Environment URL

1. In Power Platform Admin Center, click on your environment
2. Copy the **Environment URL** (e.g., `https://yourorg.crm.dynamics.com`)
3. Save this URL - you'll need it for configuration

## 3. Create App Registration for Service Principal Access

This allows the MCP server to authenticate to Dataverse without user credentials.

### Create the App Registration

```bash
# Create app registration
az ad app create \
  --display-name "CourtListenerMcpServerDataverse" \
  --sign-in-audience AzureADMultipleOrgs

# Get the App ID
DATAVERSE_APP_ID=$(az ad app list --display-name "CourtListenerMcpServerDataverse" --query "[0].appId" -o tsv)
echo "Dataverse App ID: $DATAVERSE_APP_ID"

# Get Tenant ID
TENANT_ID=$(az account show --query "tenantId" -o tsv)
echo "Tenant ID: $TENANT_ID"

# Create client secret
DATAVERSE_SECRET=$(az ad app credential reset --id $DATAVERSE_APP_ID --append --query "password" -o tsv)
echo "Dataverse Secret: $DATAVERSE_SECRET"

# Ensure Service Principal is created
az ad sp create --id $DATAVERSE_APP_ID
```

**Save these values:**
- Client ID: `<DATAVERSE_APP_ID>`
- Client Secret: `<DATAVERSE_SECRET>`
- Tenant ID: `<TENANT_ID>`

## 4. Grant Dataverse Permissions to App Registration

1. Go to [Power Platform Admin Center](https://admin.powerplatform.microsoft.com/)
2. Navigate to **Environments** and select your environment
3. Go to **Settings** > **Users + permissions** > **Application users**
4. Click **+ New app user**
5. Click **+ Add an app**
6. Search for your app registration (`CourtListenerMcpServerDataverse`) and select it
7. Select your **Business unit**
8. Under **Security roles**, assign **System Administrator** (for demo) or create a custom role
9. Click **Create**

## 5. Create Custom Table for Caching

### Using Power Apps Portal

1. Go to [Power Apps](https://make.powerapps.com/)
2. Select your environment from the top-right dropdown
3. Navigate to **Tables** > **+ New table** > **Set advanced properties**
4. Configure the table:
   - **Display name:** `Court Listener Cache`
   - **Plural name:** `Court Listener Caches`
   - **Name:** `courtlistenercache`
   - **Primary column:**
     - **Display name:** `Name`
     - **Name:** `name`
5. Click **Save**

### Add Custom Columns

After creating the table, add these columns:

1. **Cache Key Hash** (Single line of text)
   - Display name: `Cache Key Hash`
   - Name: `cachekeyhash`
   - Max length: 100
   - Required

2. **Cache Key** (Multiple lines of text)
   - Display name: `Cache Key`
   - Name: `cachekey`
   - Max length: 5000
   - Optional (for debugging)

3. **Response Data** (Multiple lines of text)
   - Display name: `Response Data`
   - Name: `responsedata`
   - Max length: 1048576 (max)
   - Required

4. **Expiration Date** (Date and Time)
   - Display name: `Expiration Date`
   - Name: `expirationdate`
   - Format: Date and time
   - Required

### Using PowerShell (Alternative)

```powershell
# Connect to your Dataverse environment
$conn = Get-CrmConnection -InteractiveMode

# Create the table (entity)
$entity = New-Object -TypeName Microsoft.Xrm.Sdk.Messages.CreateEntityRequest
$entity.Entity = New-Object -TypeName Microsoft.Xrm.Sdk.Metadata.EntityMetadata
$entity.Entity.SchemaName = "cr3f3_courtlistenercache"
$entity.Entity.DisplayName = New-Object -TypeName Microsoft.Xrm.Sdk.Label("Court Listener Cache", 1033)
$entity.Entity.DisplayCollectionName = New-Object -TypeName Microsoft.Xrm.Sdk.Label("Court Listener Caches", 1033)
$entity.Entity.OwnershipType = [Microsoft.Xrm.Sdk.Metadata.OwnershipTypes]::UserOwned

$entity.PrimaryAttribute = New-Object -TypeName Microsoft.Xrm.Sdk.Metadata.StringAttributeMetadata
$entity.PrimaryAttribute.SchemaName = "cr3f3_name"
$entity.PrimaryAttribute.DisplayName = New-Object -TypeName Microsoft.Xrm.Sdk.Label("Name", 1033)
$entity.PrimaryAttribute.MaxLength = 100

# Execute the request
$conn.Execute($entity)
```

## 6. Verify Table Schema Name

The table schema name should be: `cr3f3_courtlistenercache`

Your column schema names should be:
- `cr3f3_name` (auto-created primary column)
- `cr3f3_cachekeyhash`
- `cr3f3_cachekey`
- `cr3f3_responsedata`
- `cr3f3_expirationdate`

> **Note:** The prefix `cr3f3_` is auto-generated by Dataverse. Your prefix may be different. If so, update the table name in `/mcp-server/Services/DataverseCache.cs` line 15.

## 7. Test Connection (Optional)

You can test the connection using a simple C# console app or PowerShell:

```powershell
Install-Module Microsoft.PowerApps.PowerShell -AllowClobber

# Connect
Add-PowerAppsAccount

# Test connection
Get-PowerApp
```

## Configuration Summary

After completing this setup, you should have:

1. **Power Platform Environment:**
   - Name: `Court Listener Demo`
   - URL: `https://yourorg.crm.dynamics.com`

2. **Service Principal (App Registration):**
   - Display Name: `CourtListenerMcpServerDataverse`
   - Client ID: `<saved>`
   - Client Secret: `<saved>`
   - Tenant ID: `<saved>`
   - Granted as Application User in Dataverse

3. **Custom Table:**
   - Table Name: `cr3f3_courtlistenercache` (or your prefix)
   - Columns: name, cachekeyhash, cachekey, responsedata, expirationdate

## Next Steps

Update your MCP Server configuration with these Dataverse settings:
- See [MCP Server Deployment](./mcp-server-setup.md)
