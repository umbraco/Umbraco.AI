using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Response details for an AI audit log entry.
/// </summary>
public sealed class AiAuditResponse
{
    /// <summary>
    /// Gets or sets the generated response text.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }
}