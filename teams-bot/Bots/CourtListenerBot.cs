using CourtListenerTeamsBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace CourtListenerTeamsBot.Bots;

public class CourtListenerBot : ActivityHandler
{
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<CourtListenerBot> _logger;

    public CourtListenerBot(
        IAzureOpenAIService azureOpenAIService,
        IMcpClient mcpClient,
        ILogger<CourtListenerBot> logger)
    {
        _azureOpenAIService = azureOpenAIService;
        _mcpClient = mcpClient;
        _logger = logger;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userMessage = turnContext.Activity.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userMessage))
        {
            return;
        }

        _logger.LogInformation($"Received message: {userMessage}");

        // Show typing indicator
        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

        try
        {
            // Step 1: Use Azure OpenAI to determine which tool to call
            var toolDecision = await _azureOpenAIService.DetermineToolAsync(userMessage);

            if (!toolDecision.ShouldCallTool)
            {
                // No tool needed, send direct response
                await turnContext.SendActivityAsync(MessageFactory.Text(toolDecision.DirectResponse), cancellationToken);
                return;
            }

            _logger.LogInformation($"Calling tool: {toolDecision.ToolName} with arguments: {string.Join(", ", toolDecision.Arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // Step 2: Call the MCP server with the determined tool and arguments
            var toolResult = await _mcpClient.CallToolAsync(toolDecision.ToolName, toolDecision.Arguments);

            _logger.LogInformation($"Tool result received: {toolResult.Substring(0, Math.Min(200, toolResult.Length))}...");

            // Step 3: Use Azure OpenAI to format the response for the user
            var formattedResponse = await _azureOpenAIService.FormatResponseAsync(toolResult, userMessage);

            // Step 4: Send the formatted response back to the user
            await turnContext.SendActivityAsync(MessageFactory.Text(formattedResponse), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await turnContext.SendActivityAsync(
                MessageFactory.Text("I apologize, but I encountered an issue while searching for legal information. Please try rephrasing your question or contact support if the problem persists."),
                cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = @"Welcome to the Court Listener Legal Research Bot!

I can help you find information about:
- Court opinions and case law
- Dockets and case information
- Court details and jurisdictions

Try asking me:
- ""Find Supreme Court opinions about copyright""
- ""Search for cases in the 9th Circuit about privacy""
- ""What courts are in the federal system?""

How can I assist with your legal research today?";

        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
            }
        }
    }
}
