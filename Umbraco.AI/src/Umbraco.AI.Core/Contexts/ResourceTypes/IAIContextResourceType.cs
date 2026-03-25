using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Defines a pluggable AI context resource type.
/// </summary>
/// <remarks>
/// Resource types define how specific kinds of context resources (e.g., brand voice, text)
/// are formatted for injection into AI operations.
/// Implementations should use the <see cref="AIContextResourceTypeAttribute"/> for auto-discovery.
/// </remarks>
public interface IAIContextResourceType : IDiscoverable
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
    /// Gets the type that represents the settings model for this resource type.
    /// </summary>
    Type? SettingsType { get; }

    /// <summary>
    /// Gets the settings schema that describes the fields for this resource type.
    /// Used by the UI to render resource settings forms.
    /// </summary>
    /// <returns>The settings schema, or null if the resource type has no data model.</returns>
    AIEditableModelSchema? GetSettingsSchema();

    /// <summary>
    /// Gets the type that represents the data model for this resource type.
    /// </summary>
    Type? DataType { get; }

    /// <summary>
    /// Asynchronously resolves the resource data based on the provided settings.
    /// </summary>
    /// <param name="settings">The settings object that may contain parameters for data resolution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The resolved resource data, or null if resolution fails.</returns>
    Task<object?> ResolveDataAsync(object? settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously formats the resource data for injection into the LLM system prompt.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    string FormatDataForLlm(object? data);
}
