using System.Text.Json;
using CourtListenerMcpServer.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace CourtListenerMcpServer;

public class McpTools
{
    private readonly ILogger<McpTools> _logger;
    private readonly ICourtListenerClient _courtListenerClient;
    private readonly IDataverseCache _cache;

    public McpTools(
        ILoggerFactory loggerFactory,
        ICourtListenerClient courtListenerClient,
        IDataverseCache cache)
    {
        _logger = loggerFactory.CreateLogger<McpTools>();
        _courtListenerClient = courtListenerClient;
        _cache = cache;
    }

    [Function(nameof(SearchOpinions))]
    public async Task<string> SearchOpinions(
        [McpToolTrigger("search_opinions", "Search for court opinions by keywords, court, date range, and other criteria")]
            ToolInvocationContext context,
        [McpToolProperty("q", "Search query text")]
            string? q = null,
        [McpToolProperty("court", "Court ID filter (e.g., 'scotus', 'ca9')")]
            string? court = null,
        [McpToolProperty("filed_after", "Filter by filed date (YYYY-MM-DD)")]
            string? filed_after = null,
        [McpToolProperty("filed_before", "Filter by filed before date (YYYY-MM-DD)")]
            string? filed_before = null,
        [McpToolProperty("order_by", "Field to order by (e.g., 'date_filed')")]
            string? order_by = null)
    {
        _logger.LogInformation("SearchOpinions called with query: {Query}, court: {Court}", q, court);

        // Build cache key
        var cacheKey = $"search_opinions:{JsonSerializer.Serialize(new { q, court, filed_after, filed_before, order_by })}";

        // Try cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit for SearchOpinions");
            return cachedResult;
        }

        // Build arguments dictionary
        var arguments = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(q)) arguments["q"] = q;
        if (!string.IsNullOrEmpty(court)) arguments["court"] = court;
        if (!string.IsNullOrEmpty(filed_after)) arguments["filed_after"] = filed_after;
        if (!string.IsNullOrEmpty(filed_before)) arguments["filed_before"] = filed_before;
        if (!string.IsNullOrEmpty(order_by)) arguments["order_by"] = order_by;

        // Call Court Listener API
        var result = await _courtListenerClient.SearchOpinionsAsync(arguments);

        var response = new
        {
            count = result.Count,
            results = result.Results.Take(10).Select(o => new
            {
                id = o.Id,
                cluster = o.Cluster,
                author = o.AuthorStr,
                type = o.Type,
                download_url = o.DownloadUrl,
                absolute_url = o.AbsoluteUrl
            })
        };

        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });

        // Cache the result
        await _cache.SetAsync(cacheKey, responseJson);

        return responseJson;
    }

    [Function(nameof(GetOpinionDetails))]
    public async Task<string> GetOpinionDetails(
        [McpToolTrigger("get_opinion_details", "Get detailed information about a specific opinion by ID")]
            ToolInvocationContext context,
        [McpToolProperty("opinion_id", "The opinion ID", isRequired: true)]
            int opinion_id)
    {
        _logger.LogInformation("GetOpinionDetails called with opinion_id: {OpinionId}", opinion_id);

        // Build cache key
        var cacheKey = $"get_opinion_details:{opinion_id}";

        // Try cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit for GetOpinionDetails");
            return cachedResult;
        }

        // Call Court Listener API
        var opinion = await _courtListenerClient.GetOpinionAsync(opinion_id);

        var response = new
        {
            id = opinion.Id,
            cluster = opinion.Cluster,
            author = opinion.AuthorStr,
            joined_by = opinion.JoinedByStr,
            type = opinion.Type,
            page_count = opinion.PageCount,
            download_url = opinion.DownloadUrl,
            absolute_url = opinion.AbsoluteUrl,
            plain_text = opinion.PlainText?.Length > 2000
                ? opinion.PlainText.Substring(0, 2000) + "..."
                : opinion.PlainText
        };

        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });

        // Cache the result
        await _cache.SetAsync(cacheKey, responseJson);

        return responseJson;
    }

    [Function(nameof(SearchDockets))]
    public async Task<string> SearchDockets(
        [McpToolTrigger("search_dockets", "Search for dockets/cases by case name, docket number, court, and other criteria")]
            ToolInvocationContext context,
        [McpToolProperty("q", "Search query text")]
            string? q = null,
        [McpToolProperty("court", "Court ID filter")]
            string? court = null,
        [McpToolProperty("docket_number", "Docket number")]
            string? docket_number = null,
        [McpToolProperty("case_name", "Case name")]
            string? case_name = null,
        [McpToolProperty("filed_after", "Filter by filed date (YYYY-MM-DD)")]
            string? filed_after = null,
        [McpToolProperty("filed_before", "Filter by filed before date (YYYY-MM-DD)")]
            string? filed_before = null)
    {
        _logger.LogInformation("SearchDockets called with query: {Query}, court: {Court}", q, court);

        // Build cache key
        var cacheKey = $"search_dockets:{JsonSerializer.Serialize(new { q, court, docket_number, case_name, filed_after, filed_before })}";

        // Try cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit for SearchDockets");
            return cachedResult;
        }

        // Build arguments dictionary
        var arguments = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(q)) arguments["q"] = q;
        if (!string.IsNullOrEmpty(court)) arguments["court"] = court;
        if (!string.IsNullOrEmpty(docket_number)) arguments["docket_number"] = docket_number;
        if (!string.IsNullOrEmpty(case_name)) arguments["case_name"] = case_name;
        if (!string.IsNullOrEmpty(filed_after)) arguments["filed_after"] = filed_after;
        if (!string.IsNullOrEmpty(filed_before)) arguments["filed_before"] = filed_before;

        // Call Court Listener API
        var result = await _courtListenerClient.SearchDocketsAsync(arguments);

        var response = new
        {
            count = result.Count,
            results = result.Results.Take(10).Select(d => new
            {
                id = d.Id,
                court = d.CourtId,
                docket_number = d.DocketNumber,
                case_name = d.CaseName,
                case_name_short = d.CaseNameShort,
                date_filed = d.DateFiled,
                assigned_to = d.AssignedToStr,
                nature_of_suit = d.NatureOfSuit,
                absolute_url = d.AbsoluteUrl
            })
        };

        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });

        // Cache the result
        await _cache.SetAsync(cacheKey, responseJson);

        return responseJson;
    }

    [Function(nameof(GetCourtInfo))]
    public async Task<string> GetCourtInfo(
        [McpToolTrigger("get_court_info", "Get information about courts, including court names, jurisdictions, and metadata")]
            ToolInvocationContext context,
        [McpToolProperty("court_id", "The court ID (optional, if not provided returns all courts)")]
            string? court_id = null,
        [McpToolProperty("jurisdiction", "Filter by jurisdiction")]
            string? jurisdiction = null,
        [McpToolProperty("in_use", "Filter by whether court is in use")]
            bool? in_use = null)
    {
        _logger.LogInformation("GetCourtInfo called with court_id: {CourtId}", court_id);

        // Build cache key
        var cacheKey = $"get_court_info:{JsonSerializer.Serialize(new { court_id, jurisdiction, in_use })}";

        // Try cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit for GetCourtInfo");
            return cachedResult;
        }

        string responseJson;

        if (!string.IsNullOrEmpty(court_id))
        {
            // Get specific court
            var court = await _courtListenerClient.GetCourtAsync(court_id);

            var response = new
            {
                id = court.Id,
                full_name = court.FullName,
                short_name = court.ShortName,
                citation_string = court.CitationString,
                jurisdiction = court.Jurisdiction,
                in_use = court.InUse,
                has_opinion_scraper = court.HasOpinionScraper,
                url = court.Url
            };

            responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            // Search courts
            var arguments = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(jurisdiction)) arguments["jurisdiction"] = jurisdiction;
            if (in_use.HasValue) arguments["in_use"] = in_use.Value;

            var result = await _courtListenerClient.SearchCourtsAsync(arguments);

            var response = new
            {
                count = result.Count,
                results = result.Results.Take(20).Select(c => new
                {
                    id = c.Id,
                    full_name = c.FullName,
                    short_name = c.ShortName,
                    citation_string = c.CitationString,
                    jurisdiction = c.Jurisdiction,
                    in_use = c.InUse
                })
            };

            responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }

        // Cache the result
        await _cache.SetAsync(cacheKey, responseJson);

        return responseJson;
    }
}
