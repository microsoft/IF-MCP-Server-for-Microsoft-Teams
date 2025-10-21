using System.Text.Json;
using CourtListenerMcpServer.Models;

namespace CourtListenerMcpServer.Services;

public class McpServerService : IMcpServer
{
    private readonly ICourtListenerClient _courtListenerClient;
    private readonly IDataverseCache _cache;

    public McpServerService(ICourtListenerClient courtListenerClient, IDataverseCache cache)
    {
        _courtListenerClient = courtListenerClient;
        _cache = cache;
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        try
        {
            object? result = request.Method switch
            {
                "initialize" => HandleInitialize(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCallAsync(request.Params),
                _ => throw new Exception($"Unknown method: {request.Method}")
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message
                }
            };
        }
    }

    private object HandleInitialize()
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "court-listener-mcp-server",
                version = "1.0.0"
            }
        };
    }

    private object HandleToolsList()
    {
        var tools = new List<McpTool>
        {
            new()
            {
                Name = "search_opinions",
                Description = "Search for court opinions by keywords, court, date range, and other criteria",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        q = new { type = "string", description = "Search query text" },
                        court = new { type = "string", description = "Court ID filter" },
                        filed_after = new { type = "string", description = "Filter by filed date (YYYY-MM-DD)" },
                        filed_before = new { type = "string", description = "Filter by filed before date (YYYY-MM-DD)" },
                        order_by = new { type = "string", description = "Field to order by (e.g., 'date_filed')" }
                    }
                }
            },
            new()
            {
                Name = "get_opinion_details",
                Description = "Get detailed information about a specific opinion by ID",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        opinion_id = new { type = "integer", description = "The opinion ID" }
                    },
                    required = new[] { "opinion_id" }
                }
            },
            new()
            {
                Name = "search_dockets",
                Description = "Search for dockets/cases by case name, docket number, court, and other criteria",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        q = new { type = "string", description = "Search query text" },
                        court = new { type = "string", description = "Court ID filter" },
                        docket_number = new { type = "string", description = "Docket number" },
                        case_name = new { type = "string", description = "Case name" },
                        filed_after = new { type = "string", description = "Filter by filed date (YYYY-MM-DD)" },
                        filed_before = new { type = "string", description = "Filter by filed before date (YYYY-MM-DD)" }
                    }
                }
            },
            new()
            {
                Name = "get_court_info",
                Description = "Get information about courts, including court names, jurisdictions, and metadata",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        court_id = new { type = "string", description = "The court ID (optional, if not provided returns all courts)" },
                        jurisdiction = new { type = "string", description = "Filter by jurisdiction" },
                        in_use = new { type = "boolean", description = "Filter by whether court is in use" }
                    }
                }
            }
        };

        return new { tools };
    }

    private async Task<object> HandleToolsCallAsync(object? paramsObj)
    {
        if (paramsObj == null)
        {
            throw new Exception("No parameters provided");
        }

        var json = JsonSerializer.Serialize(paramsObj);
        var toolCall = JsonSerializer.Deserialize<ToolCallParams>(json) ?? throw new Exception("Invalid tool call parameters");

        var cacheKey = $"{toolCall.Name}:{JsonSerializer.Serialize(toolCall.Arguments)}";

        // Try to get from cache first
        var cachedResult = await _cache.GetAsync(cacheKey);
        if (cachedResult != null)
        {
            return new ToolResult
            {
                Content = new List<ToolContent>
                {
                    new() { Type = "text", Text = cachedResult }
                }
            };
        }

        // Not in cache, execute the tool
        var result = toolCall.Name switch
        {
            "search_opinions" => await ExecuteSearchOpinions(toolCall.Arguments),
            "get_opinion_details" => await ExecuteGetOpinionDetails(toolCall.Arguments),
            "search_dockets" => await ExecuteSearchDockets(toolCall.Arguments),
            "get_court_info" => await ExecuteGetCourtInfo(toolCall.Arguments),
            _ => throw new Exception($"Unknown tool: {toolCall.Name}")
        };

        var resultText = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        // Cache the result
        await _cache.SetAsync(cacheKey, resultText);

        return new ToolResult
        {
            Content = new List<ToolContent>
            {
                new() { Type = "text", Text = resultText }
            }
        };
    }

    private async Task<object> ExecuteSearchOpinions(Dictionary<string, object>? arguments)
    {
        arguments ??= new Dictionary<string, object>();
        var result = await _courtListenerClient.SearchOpinionsAsync(arguments);

        return new
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
    }

    private async Task<object> ExecuteGetOpinionDetails(Dictionary<string, object>? arguments)
    {
        if (arguments == null || !arguments.ContainsKey("opinion_id"))
        {
            throw new Exception("opinion_id is required");
        }

        var opinionId = Convert.ToInt32(arguments["opinion_id"]);
        var opinion = await _courtListenerClient.GetOpinionAsync(opinionId);

        return new
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
    }

    private async Task<object> ExecuteSearchDockets(Dictionary<string, object>? arguments)
    {
        arguments ??= new Dictionary<string, object>();
        var result = await _courtListenerClient.SearchDocketsAsync(arguments);

        return new
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
    }

    private async Task<object> ExecuteGetCourtInfo(Dictionary<string, object>? arguments)
    {
        arguments ??= new Dictionary<string, object>();

        if (arguments.ContainsKey("court_id"))
        {
            var courtId = arguments["court_id"]?.ToString() ?? throw new Exception("Invalid court_id");
            var court = await _courtListenerClient.GetCourtAsync(courtId);

            return new
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
        }
        else
        {
            var result = await _courtListenerClient.SearchCourtsAsync(arguments);

            return new
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
        }
    }
}
