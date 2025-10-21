using System.Text;
using System.Text.Json;
using CourtListenerTeamsBot.Models;

namespace CourtListenerTeamsBot.Services;

public class McpClient : IMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _functionKey;

    public McpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["McpServer:BaseUrl"] ?? throw new ArgumentException("McpServer:BaseUrl is required");
        _functionKey = configuration["McpServer:FunctionKey"];
    }

    public async Task<string> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null)
    {
        var request = new McpRequest
        {
            Id = Guid.NewGuid().ToString(),
            Method = "tools/call",
            Params = new ToolCallParams
            {
                Name = toolName,
                Arguments = arguments
            }
        };

        var url = $"{_baseUrl}/api/mcp";
        if (!string.IsNullOrEmpty(_functionKey))
        {
            url += $"?code={_functionKey}";
        }

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseJson);

        if (mcpResponse?.Error != null)
        {
            throw new Exception($"MCP Error: {mcpResponse.Error.Message}");
        }

        if (mcpResponse?.Result == null)
        {
            throw new Exception("No result returned from MCP server");
        }

        // Extract the text content from the tool result
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result);
        var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);

        if (resultElement.TryGetProperty("content", out var contentArray) &&
            contentArray.GetArrayLength() > 0)
        {
            var firstContent = contentArray[0];
            if (firstContent.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
        }

        return resultJson;
    }
}
