namespace Umbraco.Ai.Agent.Persistence.Agents;

/// <summary>
/// EF Core entity for agent storage.
/// </summary>
public class AiAgentEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias (URL-safe identifier).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional linked profile ID (soft FK).
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// JSON-serialized array of context IDs.
    /// </summary>
    public string? ContextIds { get; set; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
