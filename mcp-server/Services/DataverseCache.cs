using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CourtListenerMcpServer.Services;

public class DataverseCache : IDataverseCache
{
    private readonly ServiceClient? _serviceClient;
    private readonly int _expirationDays;
    private readonly string _tableName = "cr3f3_courtlistenercache";

    public DataverseCache(IConfiguration configuration)
    {
        var environment = configuration["Dataverse:Environment"];
        var clientId = configuration["Dataverse:ClientId"];
        var clientSecret = configuration["Dataverse:ClientSecret"];
        var tenantId = configuration["Dataverse:TenantId"];

        _expirationDays = int.TryParse(configuration["Cache:ExpirationDays"], out var days) ? days : 30;

        // Only initialize if all required configuration is present
        if (!string.IsNullOrEmpty(environment) &&
            !string.IsNullOrEmpty(clientId) &&
            !string.IsNullOrEmpty(clientSecret) &&
            !string.IsNullOrEmpty(tenantId))
        {
            var connectionString = $"AuthType=ClientSecret;Url={environment};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";
            _serviceClient = new ServiceClient(connectionString);
        }
    }

    public async Task<string?> GetAsync(string cacheKey)
    {
        if (_serviceClient == null || !_serviceClient.IsReady)
        {
            return null; // Cache not configured, return null
        }

        try
        {
            var hash = ComputeHash(cacheKey);

            var query = new QueryExpression(_tableName)
            {
                ColumnSet = new ColumnSet("cr3f3_responsedata", "cr3f3_expirationdate"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("cr3f3_cachekeyhash", ConditionOperator.Equal, hash)
                    }
                }
            };

            var results = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

            if (results.Entities.Count == 0)
            {
                return null; // Cache miss
            }

            var entity = results.Entities[0];
            var expirationDate = entity.GetAttributeValue<DateTime>("cr3f3_expirationdate");

            if (expirationDate < DateTime.UtcNow)
            {
                // Cache expired, delete it
                await Task.Run(() => _serviceClient.Delete(_tableName, entity.Id));
                return null;
            }

            return entity.GetAttributeValue<string>("cr3f3_responsedata");
        }
        catch (Exception ex)
        {
            // Log error but don't fail - just return null to indicate cache miss
            Console.WriteLine($"Cache retrieval error: {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string cacheKey, string value)
    {
        if (_serviceClient == null || !_serviceClient.IsReady)
        {
            return; // Cache not configured, skip caching
        }

        try
        {
            var hash = ComputeHash(cacheKey);
            var expirationDate = DateTime.UtcNow.AddDays(_expirationDays);

            var entity = new Entity(_tableName)
            {
                ["cr3f3_name"] = $"Cache_{hash.Substring(0, 10)}",
                ["cr3f3_cachekeyhash"] = hash,
                ["cr3f3_cachekey"] = cacheKey.Length > 1000 ? cacheKey.Substring(0, 1000) : cacheKey,
                ["cr3f3_responsedata"] = value,
                ["cr3f3_expirationdate"] = expirationDate
            };

            await Task.Run(() => _serviceClient.Create(entity));
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation
            Console.WriteLine($"Cache storage error: {ex.Message}");
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
