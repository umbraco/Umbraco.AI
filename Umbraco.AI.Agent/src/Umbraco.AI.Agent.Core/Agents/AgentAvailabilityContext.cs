namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Current execution context for agent availability filtering.
/// </summary>
/// <remarks>
/// This model captures the runtime context where an agent is being queried.
/// Different agent scopes may check different scope dimensions.
/// </remarks>
/// <example>
/// Copilot context in content section editing a document:
/// <code>
/// new AgentAvailabilityContext
/// {
///     Surface = "copilot",
///     Section = "content",
///     EntityType = "document"
/// }
/// </code>
/// </example>
public sealed class AgentAvailabilityContext
{
    /// <summary>
    /// UI surface ID (e.g., "copilot", "workspace", "dashboard").
    /// Used to filter agents by which surface they're available in.
    /// </summary>
    public string? Surface { get; init; }

    /// <summary>
    /// Current section pathname (e.g., "content", "media", "settings").
    /// Null if not in a section context.
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Current entity type (e.g., "document", "media", "documentType").
    /// Null if not in an entity context.
    /// </summary>
    public string? EntityType { get; init; }
}
