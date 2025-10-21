namespace CourtListenerTeamsBot.Services;

public interface IAzureOpenAIService
{
    Task<ToolDecision> DetermineToolAsync(string userMessage);
    Task<string> FormatResponseAsync(string toolResult, string userMessage);
}

public record ToolDecision
{
    public string ToolName { get; init; } = string.Empty;
    public Dictionary<string, object> Arguments { get; init; } = new();
    public bool ShouldCallTool { get; init; }
    public string? DirectResponse { get; init; }
}
