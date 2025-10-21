namespace CourtListenerMcpServer.Services;

public interface IDataverseCache
{
    Task<string?> GetAsync(string cacheKey);
    Task SetAsync(string cacheKey, string value);
}
