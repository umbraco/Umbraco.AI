namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Identifies the target being tested (prompt, agent, or custom feature).
/// Always tests the latest/current version - no version pinning in v1.
/// </summary>
public sealed class AITestTarget
{
    /// <summary>
    /// The identifier of the target - either a Guid or an alias.
    /// </summary>
    public required string TargetId { get; set; }

    /// <summary>
    /// Whether TargetId represents an alias (true) or Guid (false).
    /// </summary>
    public bool IsAlias { get; set; }
}
