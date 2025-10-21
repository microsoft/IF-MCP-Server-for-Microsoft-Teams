using System.Text.Json.Serialization;

namespace CourtListenerMcpServer.Models;

public record OpinionSearchResult
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }

    [JsonPropertyName("previous")]
    public string? Previous { get; init; }

    [JsonPropertyName("results")]
    public List<Opinion> Results { get; init; } = new();
}

public record Opinion
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("absolute_url")]
    public string? AbsoluteUrl { get; init; }

    [JsonPropertyName("cluster")]
    public string? Cluster { get; init; }

    [JsonPropertyName("author_str")]
    public string? AuthorStr { get; init; }

    [JsonPropertyName("per_curiam")]
    public bool PerCuriam { get; init; }

    [JsonPropertyName("joined_by_str")]
    public string? JoinedByStr { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("page_count")]
    public int? PageCount { get; init; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; init; }

    [JsonPropertyName("local_path")]
    public string? LocalPath { get; init; }

    [JsonPropertyName("plain_text")]
    public string? PlainText { get; init; }

    [JsonPropertyName("html")]
    public string? Html { get; init; }

    [JsonPropertyName("html_lawbox")]
    public string? HtmlLawbox { get; init; }

    [JsonPropertyName("html_columbia")]
    public string? HtmlColumbia { get; init; }

    [JsonPropertyName("html_with_citations")]
    public string? HtmlWithCitations { get; init; }

    [JsonPropertyName("extracted_by_ocr")]
    public bool ExtractedByOcr { get; init; }
}

public record DocketSearchResult
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }

    [JsonPropertyName("previous")]
    public string? Previous { get; init; }

    [JsonPropertyName("results")]
    public List<Docket> Results { get; init; } = new();
}

public record Docket
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("absolute_url")]
    public string? AbsoluteUrl { get; init; }

    [JsonPropertyName("court")]
    public string? Court { get; init; }

    [JsonPropertyName("court_id")]
    public string? CourtId { get; init; }

    [JsonPropertyName("docket_number")]
    public string? DocketNumber { get; init; }

    [JsonPropertyName("case_name")]
    public string? CaseName { get; init; }

    [JsonPropertyName("case_name_short")]
    public string? CaseNameShort { get; init; }

    [JsonPropertyName("case_name_full")]
    public string? CaseNameFull { get; init; }

    [JsonPropertyName("date_filed")]
    public string? DateFiled { get; init; }

    [JsonPropertyName("date_terminated")]
    public string? DateTerminated { get; init; }

    [JsonPropertyName("date_last_filing")]
    public string? DateLastFiling { get; init; }

    [JsonPropertyName("assigned_to_str")]
    public string? AssignedToStr { get; init; }

    [JsonPropertyName("referred_to_str")]
    public string? ReferredToStr { get; init; }

    [JsonPropertyName("nature_of_suit")]
    public string? NatureOfSuit { get; init; }

    [JsonPropertyName("cause")]
    public string? Cause { get; init; }

    [JsonPropertyName("jury_demand")]
    public string? JuryDemand { get; init; }

    [JsonPropertyName("jurisdiction_type")]
    public string? JurisdictionType { get; init; }
}

public record CourtSearchResult
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }

    [JsonPropertyName("previous")]
    public string? Previous { get; init; }

    [JsonPropertyName("results")]
    public List<Court> Results { get; init; } = new();
}

public record Court
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("short_name")]
    public string? ShortName { get; init; }

    [JsonPropertyName("citation_string")]
    public string? CitationString { get; init; }

    [JsonPropertyName("in_use")]
    public bool InUse { get; init; }

    [JsonPropertyName("has_opinion_scraper")]
    public bool HasOpinionScraper { get; init; }

    [JsonPropertyName("has_oral_argument_scraper")]
    public bool HasOralArgumentScraper { get; init; }

    [JsonPropertyName("position")]
    public decimal? Position { get; init; }

    [JsonPropertyName("jurisdiction")]
    public string? Jurisdiction { get; init; }
}
