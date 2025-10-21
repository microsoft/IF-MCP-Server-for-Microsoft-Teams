using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CourtListenerMcpServer.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<ICourtListenerClient, CourtListenerClient>();
        services.AddSingleton<IDataverseCache, DataverseCache>();
        services.AddSingleton<IMcpServer, McpServerService>();
    })
    .Build();

host.Run();
