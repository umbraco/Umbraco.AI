namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// Defines a pluggable rule that decides which agent should handle an <c>auto</c> request.
/// </summary>
/// <remarks>
/// <para>
/// Selectors run in order (controlled by <see cref="AIAgentSelectorCollectionBuilder"/>) until one of
/// them returns a result. Candidate filtering (active + scope) always runs before any selector, so a
/// selector can never be asked to consider an agent that scope rules already ruled out.
/// </para>
/// <para>
/// Register a selector with <c>builder.AIAgentSelectors().Append&lt;MySelector&gt;()</c> or
/// <c>InsertBefore&lt;LLMAgentSelector, MySelector&gt;()</c> to run before the built-in classifier.
/// The default chain is <see cref="DecisionAgentSelector"/> (when enabled), then
/// <see cref="LLMAgentSelector"/> - a selector that must run before both, e.g. a sticky/previous-agent
/// rule, uses <c>Insert&lt;MySelector&gt;()</c> instead (see <see cref="StickyAgentSelector"/>).
/// </para>
/// </remarks>
public interface IAIAgentSelector
{
    /// <summary>
    /// Decides which candidate agent should handle the request.
    /// </summary>
    /// <param name="request">The candidates and context available to every selector in the chain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The selected agent, or <c>null</c> for "no opinion" - the next selector in the collection decides.
    /// </returns>
    Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIAgentSelectionRequest request,
        CancellationToken cancellationToken = default);
}
