#pragma warning disable UMBRACOAI_DECISION // Consumes the experimental Decision capability for agent routing

using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// Routes an <c>auto</c> request through the experimental Decision capability: asks a single
/// <see cref="AIChoiceDecisionQuestion"/> whose options are the candidate agents, and picks whichever
/// one it answers with. Registered by default, before <see cref="LLMAgentSelector"/>, so the default
/// chain is Decision -&gt; LLM.
/// </summary>
/// <remarks>
/// Returns <c>null</c> ("no opinion", the next selector in the chain decides) whenever Decision can't
/// answer cleanly: the <see cref="AICapability.Decision"/> feature flag is off, no default Decision
/// profile is configured, there are more than 255 candidates (<see cref="AIChoiceDecisionQuestion.Options"/>
/// only supports 2-255 entries - see <c>ValidatingDecisionClient</c>), the attempt threw anything but an
/// <see cref="OperationCanceledException"/> (logged as a warning - a site that doesn't use Decision at
/// all must never have auto mode break because of it), or it answered with a key that isn't one of the
/// candidates. <see cref="OperationCanceledException"/> is never swallowed here; it propagates to the
/// caller.
/// </remarks>
public sealed class DecisionAgentSelector : IAIAgentSelector
{
    private readonly IAIDecisionService _decisionService;
    private readonly IAIProfileService _profileService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly ILogger<DecisionAgentSelector> _logger;

    public DecisionAgentSelector(
        IAIDecisionService decisionService,
        IAIProfileService profileService,
        IAIExperimentalFeatures experimentalFeatures,
        ILogger<DecisionAgentSelector> logger)
    {
        _decisionService = decisionService;
        _profileService = profileService;
        _experimentalFeatures = experimentalFeatures;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        // AIChoiceDecisionQuestion.Options only supports 2-255 entries (ValidatingDecisionClient).
        if (request.CandidateAgents.Count is < 2 or > 255)
        {
            return null;
        }

        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return null;
        }

        try
        {
            if (!await _profileService.HasDefaultProfileAsync(AICapability.Decision, cancellationToken))
            {
                return null;
            }

            var question = new AIChoiceDecisionQuestion
            {
                Instructions = "Select the id of the agent best suited to handle the user's message.",
                Options = request.CandidateAgents
                    .Select(a => new AIDecisionOption(a.Id.ToString("D"), BuildAgentOptionDescription(a)))
                    .ToList(),
            };

            var userMessage = AgentSelectionMessages.GetLastUserMessageText(request.Messages);

            var response = await _decisionService.AskAsync(
                configure: b => b.WithAlias("agent-routing"),
                question: question,
                state: userMessage,
                cancellationToken: cancellationToken);

            if (!Guid.TryParse(response.Answer.Choice, out var selectedAgentId))
            {
                return null;
            }

            var selectedAgent = request.CandidateAgents.FirstOrDefault(a => a.Id == selectedAgentId);
            return selectedAgent is null
                ? null
                : new AIAgentSelectionResult(selectedAgent, AIAgentSelectorIds.Decision, Reason: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Decision-based agent routing failed; falling back to the next selector.");
            return null;
        }
    }

    /// <summary>
    /// Builds the option description Decision sees for one agent: its name plus description, matching
    /// how <see cref="LLMAgentSelector"/> phrases the same information for the chat classifier.
    /// </summary>
    private static string BuildAgentOptionDescription(AIAgent agent)
    {
        var description = string.IsNullOrWhiteSpace(agent.Description)
            ? "No description"
            : agent.Description;

        return $"{agent.Name}: {description}";
    }
}
