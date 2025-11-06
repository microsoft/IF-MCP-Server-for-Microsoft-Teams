using System.Text.Json;
using CourtListenerMcpServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CourtListenerMcpServer.Services;

public class CourtListenerClient : ICourtListenerClient
{
    private readonly ILogger<CourtListenerClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    public CourtListenerClient(HttpClient httpClient, IConfiguration configuration, ILogger<CourtListenerClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["CourtListener:BaseUrl"] ?? "https://www.courtlistener.com/api/rest/v4/";
        _apiKey = configuration["CourtListener:ApiKey"];
        _logger = logger;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_apiKey}");
        }
    }

    public async Task<OpinionSearchResult> SearchOpinionsAsync(Dictionary<string, object> parameters)
    {
        var queryString = BuildQueryString(parameters);
        var url = $"{_baseUrl}search/?type=o&{queryString}";
        _logger.LogInformation("Searching opinions with URL: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OpinionSearchResult>(content) ?? new OpinionSearchResult();
    }

    public async Task<Opinion> GetOpinionAsync(int opinionId)
    {
        var url = $"{_baseUrl}opinions/{opinionId}/";
        _logger.LogInformation("Getting opinion with URL: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Opinion>(content) ?? throw new Exception($"Opinion {opinionId} not found");
    }

    public async Task<DocketSearchResult> SearchDocketsAsync(Dictionary<string, object> parameters)
    {
        var queryString = BuildQueryString(parameters);
        var url = $"{_baseUrl}search/?type=r&{queryString}";
        _logger.LogInformation("Searching dockets with URL: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DocketSearchResult>(content) ?? new DocketSearchResult();
    }

    public async Task<Docket> GetDocketAsync(int docketId)
    {
        var url = $"{_baseUrl}dockets/{docketId}/";
        _logger.LogInformation("Getting docket with URL: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Docket>(content) ?? throw new Exception($"Docket {docketId} not found");
    }

    public async Task<CourtSearchResult> SearchCourtsAsync(Dictionary<string, object> parameters)
    {
        var queryString = BuildQueryString(parameters);
        var url = $"{_baseUrl}courts/?{queryString}";
        _logger.LogInformation("Searching courts with URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CourtSearchResult>(content) ?? new CourtSearchResult();
    }

    public async Task<Court> GetCourtAsync(string courtId)
    {
        var url = $"{_baseUrl}courts/{courtId}/";
        _logger.LogInformation("Getting court with URL: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Court>(content) ?? throw new Exception($"Court {courtId} not found");
    }

    private static string BuildQueryString(Dictionary<string, object> parameters)
    {
        var pairs = parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}");
        return string.Join("&", pairs);
    }
}
