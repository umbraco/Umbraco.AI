using System.Text.Json.Nodes;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// The resolved schema a prompt should constrain LLM output to for a target property, plus the
/// identity needed to transform the LLM result back into the editor's write value.
/// </summary>
/// <param name="Schema">
/// The JSON Schema (draft 2020-12) to constrain generation to — either a simplified schema from an
/// <c>IAISimplifiedPropertyValueTransformer</c> (<paramref name="IsSimplified"/> is <c>true</c>) or the
/// editor's own write schema. <c>null</c> when no schema could be resolved.
/// </param>
/// <param name="IsSimplified">
/// <c>true</c> when <paramref name="Schema"/> came from a simplified transformer, meaning the LLM value
/// must be transformed back to the editor's write value before use.
/// </param>
/// <param name="DataTypeKey">The target property's data type key.</param>
/// <param name="EditorAlias">The target property's property editor alias.</param>
public sealed record AIPromptPropertyValueSchemaResolution(
    JsonObject? Schema,
    bool IsSimplified,
    Guid DataTypeKey,
    string EditorAlias);

/// <summary>
/// Resolves the JSON Schema describing the value a property editor accepts, so prompt execution
/// can constrain LLM output to the exact shape the target property expects instead of assuming
/// a plain string.
/// </summary>
public interface IAIPromptPropertyValueSchemaResolver
{
    /// <summary>
    /// Resolves the value schema for the property identified by the given content type alias,
    /// entity type, and property alias.
    /// </summary>
    /// <param name="contentTypeAlias">The content/media/member/element type alias.</param>
    /// <param name="entityType">The entity type (e.g. "document", "media", "member", "block").</param>
    /// <param name="propertyAlias">The property alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The JSON Schema (draft 2020-12) describing the property's value, or <c>null</c> when the
    /// property can't be resolved or its editor doesn't expose a value schema.
    /// </returns>
    [Obsolete("Use ResolvePropertyValueSchemaAsync, which also reports whether the schema is a simplified " +
        "transformer schema and returns the property's data type key and editor alias. This method returns " +
        "only the editor's write schema. Will be removed in v20.")]
    Task<JsonObject?> ResolveValueSchemaAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the schema a prompt should constrain generation to for the target property. When the
    /// property's editor has a registered <c>IAISimplifiedPropertyValueTransformer</c>, the simplified
    /// schema is returned (with <see cref="AIPromptPropertyValueSchemaResolution.IsSimplified"/> set);
    /// otherwise the editor's own write schema is returned.
    /// </summary>
    /// <param name="contentTypeAlias">The content/media/member/element type alias.</param>
    /// <param name="entityType">The entity type (e.g. "document", "media", "member", "block").</param>
    /// <param name="propertyAlias">The property alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The resolution, or <c>null</c> when the property can't be resolved.
    /// </returns>
    Task<AIPromptPropertyValueSchemaResolution?> ResolvePropertyValueSchemaAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken = default);
}
