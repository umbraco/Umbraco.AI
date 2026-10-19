namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// The built-in <see cref="AIAgentSelectionResult.SelectorId"/> values. Custom selectors pick their
/// own ID - short, stable, kebab-case, and stored word-for-word in audit metadata.
/// </summary>
public static class AIAgentSelectorIds
{
    /// <summary>The <see cref="DecisionAgentSelector"/>.</summary>
    public const string Decision = "decision";

    /// <summary>The <see cref="LLMAgentSelector"/>.</summary>
    public const string Llm = "llm";

    /// <summary>The <see cref="StickyAgentSelector"/>.</summary>
    public const string Sticky = "sticky";

    /// <summary>Recorded when exactly one candidate is available and no selector runs.</summary>
    public const string OnlyCandidate = "only-candidate";

    /// <summary>Recorded when every selector in the chain returned <c>null</c> or was skipped.</summary>
    public const string Fallback = "fallback";
}
