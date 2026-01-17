namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Request model for resolving AI context from multiple sources.
/// </summary>
public sealed class AiContextResolutionRequest
{
    /// <summary>
    /// The content node ID for content-level context resolution.
    /// Context is resolved by walking up the content tree.
    /// </summary>
    public Guid? ContentId { get; set; }

    /// <summary>
    /// The content path for debugging/display purposes.
    /// </summary>
    public string? ContentPath { get; set; }

    /// <summary>
    /// The profile ID to resolve context from.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs assigned to the prompt (from Umbraco.Ai.Prompt).
    /// </summary>
    public IEnumerable<Guid>? PromptContextIds { get; set; }

    /// <summary>
    /// The prompt name for debugging/display purposes.
    /// </summary>
    public string? PromptName { get; set; }

    /// <summary>
    /// Context IDs assigned to the agent (from Umbraco.Ai.Agent).
    /// </summary>
    public IEnumerable<Guid>? AgentContextIds { get; set; }

    /// <summary>
    /// The agent name for debugging/display purposes.
    /// </summary>
    public string? AgentName { get; set; }
}
