using CourtListenerMcpServer.Models;

namespace CourtListenerMcpServer.Services;

public interface IMcpServer
{
    Task<McpResponse> HandleRequestAsync(McpRequest request);
}
