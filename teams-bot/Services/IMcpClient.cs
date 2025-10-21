namespace CourtListenerTeamsBot.Services;

public interface IMcpClient
{
    Task<string> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null);
}
