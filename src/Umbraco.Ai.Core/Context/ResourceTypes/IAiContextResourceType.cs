using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Defines a pluggable AI context resource type.
/// </summary>
/// <remarks>
/// Resource types define how specific kinds of context resources (e.g., brand voice, text)
/// are formatted for injection into AI operations.
/// Implementations should use the <see cref="AiContextResourceTypeAttribute"/> for auto-discovery.
/// </remarks>
public interface IAiContextResourceType : IDiscoverable
{
    /// <summary>
    /// The immutable unique identifier of the resource type (e.g., "brand-voice", "text").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name for the UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description for the UI.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// The Umbraco icon alias for the UI.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Formats the resource data for injection into the system prompt.
    /// </summary>
    /// <param name="jsonData">The JSON-encoded resource data.</param>
    /// <returns>Formatted text suitable for AI consumption.</returns>
    string FormatForInjection(string jsonData);
}
