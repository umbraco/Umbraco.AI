using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Response details for an AI audit log entry.
/// </summary>
public sealed class AiAuditResponse
{
    /// <summary>
    /// Gets or sets the generated data.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Gets or sets the token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }
}