using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Prompt details for an AI audit log entry.
/// Carries the messages reference and capability for deferred formatting.
/// </summary>
public sealed class AIAuditPrompt
{
    /// <summary>
    /// Gets or sets the prompt data (e.g., chat messages or embedding values).
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Gets or sets the AI capability type for formatting purposes.
    /// </summary>
    public AICapability Capability { get; init; }
}
