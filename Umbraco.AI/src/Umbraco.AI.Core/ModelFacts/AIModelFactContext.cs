using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Describes the connection and capability that model facts are being requested for.
/// </summary>
/// <remarks>
/// This is a class with <c>init</c> properties, not a positional record, so new context can be added later
/// without breaking providers compiled against an earlier version.
/// </remarks>
public sealed class AIModelFactContext
{
    /// <summary>
    /// Gets the id of the connection the models were listed from.
    /// </summary>
    public required Guid ConnectionId { get; init; }

    /// <summary>
    /// Gets the id of the AI provider that owns the connection.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the capability the models were listed for.
    /// </summary>
    public required AICapability Capability { get; init; }
}
