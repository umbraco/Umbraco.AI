using System.Text.Json.Nodes;

namespace Umbraco.AI.Prompt.Core.Prompts;

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
    Task<JsonObject?> ResolveValueSchemaAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken = default);
}
