namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Specifies the target that a test will execute against.
/// The target can be identified by either ID (GUID) or alias (string).
/// Always tests the latest/current version - no version pinning in v1.
/// </summary>
public sealed class AiTestTarget
{
    /// <summary>
    /// The identifier of the target (either a GUID string or an alias).
    /// </summary>
    public required string TargetId { get; set; }

    /// <summary>
    /// Whether <see cref="TargetId"/> is an alias (true) or a GUID (false).
    /// </summary>
    public bool IsAlias { get; set; }
}
