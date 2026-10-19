using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// The built-in selector: asks the classifier profile's chat model to pick a candidate agent from
/// the last user message. Registered by default.
/// </summary>
/// <remarks>
/// Same prompt and input today's <c>SelectAgentForPromptAsync</c> used - only the last user message
/// text, not the full conversation. Returns <c>null</c> (never the first candidate) when there is no
/// classifier/default chat profile, the reply has no parseable GUID, or the GUID isn't a candidate -
/// the agent selection service owns the "pick the first candidate" fallback.
/// </remarks>
public sealed class LLMAgentSelector : IAIAgentSelector
{
    private readonly IAIProfileService _profileService;
    private readonly IAIChatClientFactory _chatClientFactory;

    public LLMAgentSelector(IAIProfileService profileService, IAIChatClientFactory chatClientFactory)
    {
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
    }

    /// <inheritdoc />
    public async Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        AIProfile profile;
        try
        {
            profile = await _profileService.GetClassifierProfileAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // No classifier or default chat profile configured.
            return null;
        }

        var userPrompt = AgentSelectionMessages.GetLastUserMessageText(request.Messages);
        var classificationPrompt = BuildClassificationPrompt(request.CandidateAgents, userPrompt);

        var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);
        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, classificationPrompt)], options: null, cancellationToken);
        var responseText = response.Text ?? string.Empty;

        var selectedAgentId = ParseAgentIdFromResponse(responseText);
        if (selectedAgentId is null)
        {
            return null;
        }

        var selectedAgent = request.CandidateAgents.FirstOrDefault(a => a.Id == selectedAgentId.Value);
        return selectedAgent is null ? null : new AIAgentSelectionResult(selectedAgent, AIAgentSelectorIds.Llm, Reason: null);
    }

    /// <summary>
    /// Builds a classification prompt for agent selection.
    /// </summary>
    private static string BuildClassificationPrompt(IReadOnlyList<AIAgent> agents, string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an agent router. Given the user's message, select the most appropriate agent.");
        sb.AppendLine("Return ONLY the agent ID (the GUID) on a single line, nothing else.");
        sb.AppendLine();
        sb.AppendLine("Available agents:");

        foreach (var agent in agents)
        {
            var description = string.IsNullOrWhiteSpace(agent.Description)
                ? "No description"
                : agent.Description;

            sb.AppendLine($"[{agent.Id}] {agent.Name}: {description}");
        }

        sb.AppendLine();
        sb.AppendLine($"User message: {userPrompt}");

        return sb.ToString();
    }

    /// <summary>
    /// Parses an agent ID (GUID) from the LLM response.
    /// </summary>
    private static Guid? ParseAgentIdFromResponse(string response)
    {
        // Try to find a GUID in the response using regex
        var guidPattern = @"[{(]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[)}]?";
        var match = Regex.Match(response, guidPattern);

        if (match.Success && Guid.TryParse(match.Value, out var agentId))
        {
            return agentId;
        }

        return null;
    }
}
