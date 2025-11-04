using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CourtListenerMcpServer;
using CourtListenerMcpServer.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<ICourtListenerClient, CourtListenerClient>();
        services.AddSingleton<IDataverseCache, DataverseCache>();
        services.AddScoped<McpTools>();
    })
    .Build();

host.Run();
