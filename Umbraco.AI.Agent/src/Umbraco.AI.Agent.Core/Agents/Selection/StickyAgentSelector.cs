namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// An opt-in selector that keeps the previous turn's agent for the rest of the conversation.
/// </summary>
/// <remarks>
/// <para>
/// Ships with the package but is **not** registered by default - turning it on keeps today's
/// re-pick-every-turn behaviour unchanged for everyone who doesn't ask for sticky selection.
/// Register it first in the chain so it gets first refusal, ahead of every other selector
/// (<see cref="DecisionAgentSelector"/>, <see cref="LLMAgentSelector"/>, or any other selector that
/// would switch agents):
/// </para>
/// <code>
/// builder.AIAgentSelectors().Insert&lt;StickyAgentSelector&gt;();
/// </code>
/// <para>
/// Use <c>Insert</c>, not <c>InsertBefore&lt;LLMAgentSelector, …&gt;</c> - <see cref="DecisionAgentSelector"/>
/// is registered by default before <see cref="LLMAgentSelector"/>, so inserting only before LLM would
/// land Sticky after Decision and let Decision override the previous turn's agent.
/// </para>
/// </remarks>
public sealed class StickyAgentSelector : IAIAgentSelector
{
    /// <inheritdoc />
    public Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = request.PreviousAgent is { } previousAgent
            ? new AIAgentSelectionResult(previousAgent, AIAgentSelectorIds.Sticky, Reason: null)
            : null;

        return Task.FromResult(result);
    }
}
