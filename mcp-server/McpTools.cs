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

    /// <summary>
    /// Workaround for MCP extension parameter binding bug - manually extracts arguments from context.
    /// Converts DateTime strings to YYYY-MM-DD format for date parameters.
    /// See GH Issue: https://github.com/Azure/azure-functions-mcp-extensions/issues/124
    /// </summary>
    private static string? GetArgumentValue(ToolInvocationContext context, string key, bool isDate = false)
    {
        if (context.Arguments == null || !context.Arguments.TryGetValue(key, out var value))
            return null;

        var stringValue = value?.ToString();

        if (isDate && !string.IsNullOrEmpty(stringValue))
        {
            // Try to parse as DateTime and convert to YYYY-MM-DD format
            if (DateTime.TryParse(stringValue, out var dateValue))
                return dateValue.ToString("yyyy-MM-dd");
        }

        return stringValue;
    }

    [Function(nameof(SearchOpinions))]
    public async Task<string> SearchOpinions(
        [McpToolTrigger("search_opinions", "Search for court opinions by keywords, court, date range, and other criteria")]
            ToolInvocationContext context,
        [McpToolProperty("q", "Search query text")]
            string? q = null,
        [McpToolProperty("court", "Court ID filter (e.g., 'scotus', 'ca9')")]
            string? court = null,
        [McpToolProperty("filedAfter", "Filter by filed date (YYYY-MM-DD)")]
            string? filedAfter = null,
        [McpToolProperty("filedBefore", "Filter by filed before date (YYYY-MM-DD)")]
            string? filedBefore = null,
        [McpToolProperty("orderBy", "Field to order by (e.g., 'dateFiled')")]
            string? orderBy = null)
    {
        // Workaround for MCP extension parameter binding bug
        filedAfter = GetArgumentValue(context, "filedAfter", isDate: true) ?? filedAfter;
        filedBefore = GetArgumentValue(context, "filedBefore", isDate: true) ?? filedBefore;

        _logger.LogInformation("SearchOpinions called with query: {Query}, court: {Court}, filedAfter: {FiledAfter}, filedBefore: {FiledBefore}, orderBy: {OrderBy}",
            q, court, filedAfter, filedBefore, orderBy);

        // Build cache key
        var cacheKey = $"search_opinions:{JsonSerializer.Serialize(new { q, court, filedAfter, filedBefore, orderBy })}";
        _logger.LogInformation("Cache key for SearchOpinions: {CacheKey}", cacheKey);

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
        if (!string.IsNullOrEmpty(filedAfter)) arguments["filedAfter"] = filedAfter;
        if (!string.IsNullOrEmpty(filedBefore)) arguments["filedBefore"] = filedBefore;
        if (!string.IsNullOrEmpty(orderBy)) arguments["orderBy"] = orderBy;

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
        [McpToolProperty("docketNumber", "Docket number")]
            string? docketNumber = null,
        [McpToolProperty("caseName", "Case name")]
            string? caseName = null,
        [McpToolProperty("filedAfter", "Filter by filed date (YYYY-MM-DD)")]
            string? filedAfter = null,
        [McpToolProperty("filedBefore", "Filter by filed before date (YYYY-MM-DD)")]
            string? filedBefore = null)
    {
        // Workaround for MCP extension parameter binding bug
        filedAfter = GetArgumentValue(context, "filedAfter", isDate: true) ?? filedAfter;
        filedBefore = GetArgumentValue(context, "filedBefore", isDate: true) ?? filedBefore;

        _logger.LogInformation("SearchDockets called with query: {Query}, court: {Court}, docketNumber: {DocketNumber}, caseName: {CaseName}, filedAfter: {FiledAfter}, filedBefore: {FiledBefore}",
            q, court, docketNumber, caseName, filedAfter, filedBefore);

        // Build cache key
        var cacheKey = $"search_dockets:{JsonSerializer.Serialize(new { q, court, docketNumber, caseName, filedAfter, filedBefore })}";

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
        if (!string.IsNullOrEmpty(docketNumber)) arguments["docketNumber"] = docketNumber;
        if (!string.IsNullOrEmpty(caseName)) arguments["caseName"] = caseName;
        if (!string.IsNullOrEmpty(filedAfter)) arguments["filedAfter"] = filedAfter;
        if (!string.IsNullOrEmpty(filedBefore)) arguments["filedBefore"] = filedBefore;

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
        [McpToolProperty("courtId", "The court ID (optional, if not provided returns all courts)")]
            string? courtId = null,
        [McpToolProperty(
                "jurisdiction",
                "Filter by jurisdiction. Allowed values: F (Federal Appellate), FD (Federal District), FB (Federal Bankruptcy), FBP (Federal Bankruptcy Panel), FS (Federal Special), S (State Supreme), SA (State Appellate), ST (State Trial), SS (State Special), TRS (Tribal Supreme), TRA (Tribal Appellate), TRT (Tribal Trial), TRX (Tribal Special), TS (Territory Supreme), TA (Territory Appellate), TT (Territory Trial), TSP (Territory Special), SAG (State Attorney General), MA (Military Appellate), MT (Military Trial), C (Committee), I (International), T (Testing)"
            )]
            string? jurisdiction = null,
        [McpToolProperty("shortName", "Filter by short name (partial match allowed)")]
            string? shortName = null,
        [McpToolProperty("shortNameLookup", "Lookup type for short name. Allowed: exact, iexact, startswith, istartswith, endswith, iendswith, contains, icontains")]
            string? shortNameLookup = null,
        [McpToolProperty("fullName", "Filter by full name (partial match allowed)")]
            string? fullName = null,
        [McpToolProperty("fullNameLookup", "Lookup type for full name. Allowed: exact, iexact, startswith, istartswith, endswith, iendswith, contains, icontains")]
            string? fullNameLookup = null,
        [McpToolProperty("inUse", "Filter by whether court is in use")]
            bool? inUse = null)
    {
        // Handle boolean parameter
        if (context.Arguments != null && context.Arguments.TryGetValue("inUse", out var inUseVal))
        {
            if (bool.TryParse(inUseVal?.ToString(), out var inUseBool))
                inUse = inUseBool;
        }

        _logger.LogInformation("GetCourtInfo called with courtId: {CourtId}, jurisdiction: {Jurisdiction}, shortName: {ShortName}, fullName: {FullName}, inUse: {InUse}",
            courtId, jurisdiction, shortName, fullName, inUse);

        // Build cache key
        var cacheKey = $"get_court_info:{JsonSerializer.Serialize(new { courtId, jurisdiction, shortName, shortNameLookup, fullName, fullNameLookup, inUse })}";

        // Try cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogInformation("Cache hit for GetCourtInfo");
            return cachedResult;
        }

        string responseJson;

        if (!string.IsNullOrEmpty(courtId))
        {
            // Get specific court
            var court = await _courtListenerClient.GetCourtAsync(courtId);

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
            if (!string.IsNullOrEmpty(shortName))
            {
                var key = !string.IsNullOrEmpty(shortNameLookup) ? $"shortName__{shortNameLookup}" : "shortName";
                arguments[key] = shortName;
            }
            if (!string.IsNullOrEmpty(fullName))
            {
                var key = !string.IsNullOrEmpty(fullNameLookup) ? $"fullName__{fullNameLookup}" : "fullName";
                arguments[key] = fullName;
            }
            if (!string.IsNullOrEmpty(jurisdiction)) arguments["jurisdiction"] = jurisdiction;
            if (inUse.HasValue) arguments["inUse"] = inUse.Value;

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
