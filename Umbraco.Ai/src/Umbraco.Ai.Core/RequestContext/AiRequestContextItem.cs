using System.Text.Json;

namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// A flexible context item that can contain any data.
/// Matches the AG-UI protocol's simple structure.
/// </summary>
public class AiRequestContextItem
{
    /// <summary>
    /// Human-readable description (e.g., "Currently editing document: My Page").
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The context data.
    /// </summary>
    public string? Value { get; init; }
}
