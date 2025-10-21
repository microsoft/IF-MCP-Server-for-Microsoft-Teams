using System.Net;
using System.Text.Json;
using CourtListenerMcpServer.Models;
using CourtListenerMcpServer.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CourtListenerMcpServer;

public class McpFunctions
{
    private readonly ILogger _logger;
    private readonly IMcpServer _mcpServer;

    public McpFunctions(ILoggerFactory loggerFactory, IMcpServer mcpServer)
    {
        _logger = loggerFactory.CreateLogger<McpFunctions>();
        _mcpServer = mcpServer;
    }

    [Function("mcp")]
    public async Task<HttpResponseData> HandleMcpRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mcp")] HttpRequestData req)
    {
        _logger.LogInformation("Processing MCP request");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<McpRequest>(requestBody);

            if (mcpRequest == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Invalid request format" });
                return errorResponse;
            }

            var mcpResponse = await _mcpServer.HandleRequestAsync(mcpRequest);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(mcpResponse);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            });

            return errorResponse;
        }
    }

    [Function("health")]
    public HttpResponseData HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "court-listener-mcp-server"
        }));

        return response;
    }
}
