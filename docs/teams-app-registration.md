# Teams App Registration and Installation Guide

This guide walks you through creating and installing the Court Listener bot in Microsoft Teams.

## Prerequisites

- Completed [Teams Bot Deployment](./teams-bot-setup.md)
- Teams admin access (for organization-wide installation) OR
- Permission to upload custom apps (for personal installation)

## 1. Prepare Teams App Manifest

Navigate to the Teams app manifest directory:

```bash
cd mcp-teams/teams-bot/TeamsAppManifest
```

### Update manifest.json

Replace the placeholders in `manifest.json`:

```json
{
  "id": "{{TEAMS_APP_ID}}",  // Replace with a new GUID
  "...": "...",
  "bots": [
    {
      "botId": "{{MICROSOFT_APP_ID}}",  // Replace with your Bot App ID
      "...": "..."
    }
  ]
}
```

#### Generate a new Teams App ID (GUID)

**PowerShell:**
```powershell
[guid]::NewGuid().ToString()
```

**Bash/Linux:**
```bash
uuidgen
```

**Online:**
Go to https://www.uuidgenerator.net/

#### Update the manifest

Replace `{{TEAMS_APP_ID}}` with the new GUID.
Replace `{{MICROSOFT_APP_ID}}` with your Bot App ID from Azure setup.

Example:
```json
{
  "id": "12345678-1234-1234-1234-123456789abc",
  "bots": [
    {
      "botId": "87654321-4321-4321-4321-cba987654321",
      ...
    }
  ]
}
```

## 2. Create App Icons

Teams requires two icon files in the `TeamsAppManifest` folder:

### color.png (192x192 pixels)
- Full-color icon
- PNG format
- Exactly 192x192 pixels

### outline.png (32x32 pixels)
- Transparent background
- White or single-color outline
- PNG format
- Exactly 32x32 pixels

You can create simple icons or download templates from [Teams App Design Guidelines](https://learn.microsoft.com/en-us/microsoftteams/platform/concepts/design/design-teams-app-overview).

### Quick icon creation (placeholder)

If you don't have icons ready, here's a quick way to create placeholder icons:

**Using ImageMagick (if installed):**
```bash
# Create a simple colored icon (192x192)
convert -size 192x192 xc:#0078D4 -gravity center -pointsize 72 -fill white -annotate +0+0 "CL" color.png

# Create a simple outline icon (32x32)
convert -size 32x32 xc:transparent -gravity center -pointsize 20 -fill white -annotate +0+0 "CL" outline.png
```

## 3. Create Teams App Package

### Verify files in TeamsAppManifest folder

You should have:
- `manifest.json` (with placeholders replaced)
- `color.png` (192x192)
- `outline.png` (32x32)

### Create the zip package

**Windows (PowerShell):**
```powershell
Compress-Archive -Path .\manifest.json, .\color.png, .\outline.png -DestinationPath .\CourtListenerBot.zip -Force
```

**Mac/Linux:**
```bash
zip CourtListenerBot.zip manifest.json color.png outline.png
```

> **Important:** The files must be at the root of the zip, not in a subfolder.

## 4. Install in Microsoft Teams

### Option A: Upload for Personal/Team Use

1. Open Microsoft Teams
2. Click **Apps** in the left sidebar
3. Click **Manage your apps** (bottom left)
4. Click **Upload an app**
5. Select **Upload a custom app**
6. Choose `CourtListenerBot.zip`
7. Click **Add** to install for yourself, or **Add to a team** to install in a specific team

### Option B: Upload to Organization App Catalog (Admin)

1. Go to [Teams Admin Center](https://admin.teams.microsoft.com/)
2. Navigate to **Teams apps** > **Manage apps**
3. Click **Upload**
4. Upload `CourtListenerBot.zip`
5. Review and approve the app
6. Configure app policies to make it available to users

## 5. Test the Bot in Teams

### Start a conversation

1. In Teams, go to **Chat** or **Apps**
2. Search for "Court Listener Bot"
3. Click on the bot to open a chat
4. Send a message: "Hello"

You should receive the welcome message.

### Test queries

Try these sample queries:

1. **Search Supreme Court opinions:**
   ```
   Find Supreme Court opinions about copyright
   ```

2. **Search circuit court cases:**
   ```
   Search for privacy cases in the 9th Circuit
   ```

3. **Get court information:**
   ```
   What courts are in the federal system?
   ```

4. **Search dockets:**
   ```
   Find dockets with "Apple" in the case name
   ```

### Expected behavior

1. Bot shows typing indicator
2. Bot responds with formatted results from Court Listener
3. Results include case names, courts, dates, and links

## 6. Add Bot to a Team Channel

1. Navigate to a Team
2. Click on a channel
3. Click **+** (Add a tab)
4. Search for "Court Listener Bot"
5. Click **Add**
6. Configure and save

Now team members can interact with the bot in the channel.

## 7. Troubleshooting

### Bot doesn't appear in Teams

**Solutions:**
- Wait a few minutes for propagation
- Verify the manifest.json is valid
- Check that the zip package is created correctly (files at root level)
- Ensure your organization allows custom app uploads

### Bot doesn't respond

**Solutions:**
- Verify the bot is deployed and running in Azure
- Check Azure App Service logs for errors
- Verify the messaging endpoint is correct in Azure Bot configuration
- Test with Bot Framework Emulator first

### "Something went wrong" error

**Solutions:**
- Check bot App ID and Password are correct
- Verify the bot endpoint is accessible (HTTPS)
- Review Azure Application Insights for errors
- Check that Teams channel is enabled in Azure Bot

### Manifest validation errors

**Solutions:**
- Validate your manifest at [Teams App Validator](https://dev.teams.microsoft.com/appvalidation.html)
- Ensure all required fields are filled
- Verify icon dimensions are correct
- Check that all URLs are HTTPS

## 8. Update the App

When you make changes to the bot code:

1. Redeploy the bot to Azure App Service
2. No need to update Teams package (unless changing manifest)

When you change the manifest:

1. Update `manifest.json`
2. Increment the version number in manifest
3. Recreate the zip package
4. In Teams, go to **Apps** > **Manage your apps**
5. Find your app and click the **...** menu
6. Select **Update**
7. Upload the new zip package

## 9. Distribute to Users

### For organization-wide deployment

1. Teams Admin uploads app to org catalog
2. Configure app permission policies
3. Users can find the app in their Teams app store

### For external distribution

1. Submit to [Teams App Store](https://aka.ms/publishteamsapp)
2. Follow Microsoft's app validation process
3. App becomes publicly available

## 10. Demo Scenarios

### Scenario 1: Legal Research
**User:** "Find recent Supreme Court opinions about privacy rights"
**Bot:** Returns relevant opinions with case names, dates, and links

### Scenario 2: Case Discovery
**User:** "Search for cases about copyright in the 2nd Circuit from 2023"
**Bot:** Returns filtered dockets and opinions

### Scenario 3: Court Information
**User:** "Tell me about the 9th Circuit Court"
**Bot:** Returns court details, jurisdiction, and metadata

### Scenario 4: Specific Case Details
**User:** "Get details for opinion ID 12345"
**Bot:** Returns full opinion details including text excerpts

## Summary

After completing this guide, you should have:

1. ✅ Valid Teams app manifest with correct IDs
2. ✅ App icons (color and outline)
3. ✅ Teams app package (zip file)
4. ✅ Bot installed in Teams
5. ✅ Successfully tested bot queries
6. ✅ Bot accessible to team members

## Next Steps

- Share the app with your team
- Customize the bot responses and prompts
- Monitor usage via Application Insights
- Gather feedback and iterate

## Additional Resources

- [Teams App Manifest Schema](https://learn.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema)
- [Design Guidelines](https://learn.microsoft.com/en-us/microsoftteams/platform/concepts/design/design-teams-app-overview)
- [Bot Framework Documentation](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Court Listener API Documentation](https://www.courtlistener.com/help/api/)
