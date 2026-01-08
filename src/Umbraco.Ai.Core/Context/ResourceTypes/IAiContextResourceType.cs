using Umbraco.Ai.Core.EditableModels;
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
    /// Gets the type that represents the data model for this resource type.
    /// </summary>
    Type? DataType { get; }

    /// <summary>
    /// Gets the data schema that describes the fields for this resource type.
    /// Used by the UI to render resource data forms.
    /// </summary>
    /// <returns>The data schema, or null if the resource type has no data model.</returns>
    AiEditableModelSchema? GetDataSchema();

    /// <summary>
    /// Formats the resource data for injection into the system prompt.
    /// </summary>
    /// <param name="data">The resource data object.</param>
    /// <returns>Formatted text suitable for AI consumption.</returns>
    string FormatForInjection(object? data);
}
