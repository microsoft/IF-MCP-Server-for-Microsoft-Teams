using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;

namespace CourtListenerTeamsBot.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public AzureOpenAIService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentException("AzureOpenAI:Endpoint is required");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentException("AzureOpenAI:ApiKey is required");
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? throw new ArgumentException("AzureOpenAI:DeploymentName is required");

        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<ToolDecision> DetermineToolAsync(string userMessage)
    {
        var systemPrompt = @"You are an AI assistant helping legal teams find case law information using the Court Listener API.
Analyze the user's question and determine which tool to call:

Tools available:
1. search_opinions - Search for court opinions by keywords, court, date
   Arguments: q (query), court (court ID), filed_after (YYYY-MM-DD), filed_before (YYYY-MM-DD), order_by

2. get_opinion_details - Get detailed information about a specific opinion
   Arguments: opinion_id (integer)

3. search_dockets - Search for dockets/cases by case name, docket number, court
   Arguments: q (query), court (court ID), docket_number, case_name, filed_after (YYYY-MM-DD), filed_before (YYYY-MM-DD)

4. get_court_info - Get information about courts
   Arguments: court_id (string, optional), jurisdiction (string, optional), in_use (boolean, optional), short_name (string, optional), short_name_lookup (string, optional), full_name (string, optional), full_name_lookup (string, optional)

Respond with JSON in this format:
{
  ""should_call_tool"": true/false,
  ""tool_name"": ""tool_name"",
  ""arguments"": {
    ""arg1"": ""value1"",
    ""arg2"": ""value2""
  },
  ""direct_response"": ""optional message if no tool needed""
}

Common court IDs:
- scotus: Supreme Court of the United States
- ca1, ca2, ... ca11, cadc, cafc: Federal Circuit Courts
- dcd, nysd, cand, etc.: District Courts

Jurisdictions for get_court_info only allows the following values (with meaning). Leave blank to search all jurisdictions.
- F (Federal Appellate)
- FD (Federal District)
- FB (Federal Bankruptcy)
- FBP (Federal Bankruptcy Panel)
- FS (Federal Special)
- S (State Supreme)
- SA (State Appellate)
- ST (State Trial)
- SS (State Special)
- TRS (Tribal Supreme)
- TRA (Tribal Appellate)
- TRT (Tribal Trial)
- TRX (Tribal Special)
- TS (Territory Supreme)
- TA (Territory Appellate)
- TT (Territory Trial)
- TSP (Territory Special)
- SAG (State Attorney General)
- MA (Military Appellate)
- MT (Military Trial)    
- C (Committee)
- I (International)
- T (Testing)

For short_name and full_name, you can use the following lookup types to control how the value is matched:
- exact: exact match
- iexact: case-insensitive exact match
- startswith: starts with
- istartswith: case-insensitive starts with
- endswith: ends with
- iendswith: case-insensitive ends with
- contains: contains substring
- icontains: case-insensitive contains substring

Example:
To find courts whose short name contains 'supreme' (case-insensitive), use:
  'short_name': 'supreme',
  'short_name_lookup': 'icontains'

If the user's query is unclear or not related to case law, set should_call_tool to false and provide a direct_response.";

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userMessage)
            },
            Temperature = 0.3f,
            MaxTokens = 500
        };

        var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
        var content = response.Value.Choices[0].Message.Content;

        // Parse the JSON response
        var decision = JsonSerializer.Deserialize<JsonElement>(content);

        var shouldCallTool = decision.GetProperty("should_call_tool").GetBoolean();

        if (!shouldCallTool)
        {
            return new ToolDecision
            {
                ShouldCallTool = false,
                DirectResponse = decision.TryGetProperty("direct_response", out var directResponse)
                    ? directResponse.GetString()
                    : "I'm not sure how to help with that. Please ask about court opinions, dockets, or court information."
            };
        }

        var toolName = decision.GetProperty("tool_name").GetString() ?? string.Empty;
        var arguments = new Dictionary<string, object>();

        if (decision.TryGetProperty("arguments", out var argsElement))
        {
            foreach (var prop in argsElement.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetInt32(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }
        }

        return new ToolDecision
        {
            ShouldCallTool = true,
            ToolName = toolName,
            Arguments = arguments
        };
    }

    public async Task<string> FormatResponseAsync(string toolResult, string userMessage)
    {
        var systemPrompt = @"You are an AI assistant helping legal teams with case law research.
The user asked a question, and we've retrieved information from the Court Listener database.
Format the results in a clear, professional manner suitable for a legal team.

Guidelines:
- Be concise but informative
- Highlight key information like case names, courts, dates, docket numbers
- Use bullet points for multiple results
- Include relevant URLs when available
- If there are no results, explain that clearly
- Keep responses under 2000 characters for Teams display";

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage($"User question: {userMessage}\n\nAPI Results:\n{toolResult}")
            },
            Temperature = 0.5f,
            MaxTokens = 1000
        };

        var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }
}
