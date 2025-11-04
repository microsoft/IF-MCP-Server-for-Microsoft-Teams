using System.Linq;
using System.Text;
using System.Text.Json;
using CourtListenerTeamsBot.Models;
using Microsoft.Extensions.Logging;

namespace CourtListenerTeamsBot.Services;

public class McpClient : IMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _functionKey;
    private readonly ILogger<McpClient> _logger;

    public McpClient(HttpClient httpClient, IConfiguration configuration, ILogger<McpClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["McpServer:BaseUrl"] ?? throw new ArgumentException("McpServer:BaseUrl is required");
        _functionKey = configuration["McpServer:FunctionKey"];
        _logger = logger;

        _logger.LogInformation("McpClient initialized. BaseUrl: {BaseUrl}, Has FunctionKey: {HasKey}, Key Length: {KeyLength}",
            _baseUrl,
            !string.IsNullOrEmpty(_functionKey),
            _functionKey?.Length ?? 0);
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

        var url = $"{_baseUrl}/runtime/webhooks/mcp";

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        // MCP protocol headers - must accept both application/json and text/event-stream
        // Prioritize JSON (q=1.0) over SSE (q=0.9)
        httpRequest.Headers.Accept.Clear();
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json", 1.0));
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream", 0.9));

        if (!string.IsNullOrEmpty(_functionKey))
        {
            httpRequest.Headers.Add("x-functions-key", _functionKey);
            _logger.LogInformation("Added x-functions-key header. Key starts with: {KeyPrefix}", _functionKey.Substring(0, Math.Min(10, _functionKey.Length)));
        }
        else
        {
            _logger.LogWarning("No function key configured!");
        }

        _logger.LogInformation("Sending MCP request. Method: {Method}, Body: {Body}",
            request.Method,
            requestJson);

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(httpRequest);
            _logger.LogInformation("MCP response received. Status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("MCP request failed. URL: {Url}, Status: {Status}, Response: {Response}",
                    url, (int)response.StatusCode, errorBody);
            }

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"MCP request failed. URL: {url}, Status: {ex.StatusCode}, Message: {ex.Message}", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType;

        _logger.LogInformation("MCP response content type: {ContentType}, First 200 chars: {Preview}",
            contentType,
            responseBody.Length > 200 ? responseBody.Substring(0, 200) : responseBody);

        // Parse SSE format if needed
        string responseJson;
        if (contentType == "text/event-stream")
        {
            // SSE format: "event: message\ndata: <json>\n\n"
            // Extract the JSON from the data: line
            var lines = responseBody.Split('\n');
            var dataLine = lines.FirstOrDefault(l => l.StartsWith("data: "));
            if (dataLine == null)
            {
                throw new Exception($"Invalid SSE response: no data line found. Response: {responseBody}");
            }
            responseJson = dataLine.Substring(6); // Remove "data: " prefix
            _logger.LogInformation("Extracted JSON from SSE data line: {Json}",
                responseJson.Length > 200 ? responseJson.Substring(0, 200) : responseJson);
        }
        else
        {
            responseJson = responseBody;
        }

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
