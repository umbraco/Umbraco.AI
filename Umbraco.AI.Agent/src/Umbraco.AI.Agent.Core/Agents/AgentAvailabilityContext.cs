namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Current execution context for agent availability filtering.
/// </summary>
/// <remarks>
/// This model captures the runtime context where an agent is being queried.
/// Different agent scopes may check different context dimensions.
/// </remarks>
/// <example>
/// Copilot context in content section editing a document:
/// <code>
/// new AgentAvailabilityContext
/// {
///     Section = "content",
///     EntityTypeAlias = "document"
/// }
/// </code>
/// </example>
public sealed class AgentAvailabilityContext
{
    /// <summary>
    /// Current section pathname (e.g., "content", "media", "settings").
    /// Null if not in a section context.
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Current entity type alias (e.g., "document", "media", "documentType").
    /// Null if not in an entity context.
    /// </summary>
    public string? EntityTypeAlias { get; init; }
}
