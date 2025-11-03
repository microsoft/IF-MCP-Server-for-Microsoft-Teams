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
    private readonly string _tableName;
    private readonly string _tablePrefix;

    public DataverseCache(IConfiguration configuration)
    {
        var environment = configuration["Dataverse:Environment"];
        var clientId = configuration["Dataverse:ClientId"];
        var clientSecret = configuration["Dataverse:ClientSecret"];
        var tenantId = configuration["Dataverse:TenantId"];

        // Get table name from configuration (includes prefix, e.g., "cr3f3_courtlistenercache")
        _tableName = configuration["Dataverse:TableName"] ?? throw new ArgumentException("Dataverse:TableName is required");

        // Extract the prefix from the table name (everything before the first underscore)
        var underscoreIndex = _tableName.IndexOf('_');
        _tablePrefix = underscoreIndex > 0 ? _tableName.Substring(0, underscoreIndex) : string.Empty;
  
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
                ColumnSet = new ColumnSet($"{_tablePrefix}_responsedata", $"{_tablePrefix}_expirationdate"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression($"{_tablePrefix}_cachekeyhash", ConditionOperator.Equal, hash)
                    }
                }
            };

            var results = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

            if (results.Entities.Count == 0)
            {
                return null; // Cache miss
            }

            var entity = results.Entities[0];
            var expirationDate = entity.GetAttributeValue<DateTime>($"{_tablePrefix}_expirationdate");

            if (expirationDate < DateTime.UtcNow)
            {
                // Cache expired, delete it
                await Task.Run(() => _serviceClient.Delete(_tableName, entity.Id));
                return null;
            }

            return entity.GetAttributeValue<string>($"{_tablePrefix}_responsedata");
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
                [$"{_tablePrefix}_name"] = $"Cache_{hash.Substring(0, 10)}",
                [$"{_tablePrefix}_cachekeyhash"] = hash,
                [$"{_tablePrefix}_cachekey"] = cacheKey.Length > 1000 ? cacheKey.Substring(0, 1000) : cacheKey,
                [$"{_tablePrefix}_responsedata"] = value,
                [$"{_tablePrefix}_expirationdate"] = expirationDate
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
