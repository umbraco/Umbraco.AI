using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Provides a <em>simplified</em>, strict-representable LLM value contract for a specific Umbraco
/// property editor: the JSON Schema the LLM should generate the property value against, plus the
/// transform that expands the LLM's simplified value into the editor's real write value.
/// </summary>
/// <remarks>
/// <para>
/// Some editors expose a CMS write schema that cannot be expressed in a provider's <em>strict</em>
/// structured-output subset (e.g. the rich-text / block editors, whose nested block value is an
/// unconstrained <c>{}</c> node). For those, a consumer that requires strict output (such as the AI
/// Prompt schema-driven wand) cannot use the write schema directly. A transformer offers an
/// alternative: a simple schema the LLM <em>can</em> satisfy (e.g. rich-text as a plain markup
/// string), and a transform back to the editor's write value (e.g. <c>{ markup, blocks }</c>).
/// </para>
/// <para>
/// The transform is <strong>total and idempotent</strong>: it accepts either a value the LLM
/// produced against <see cref="GetSimplifiedSchemaAsync"/> <em>or</em> a value already in the
/// editor's write shape (which it returns unchanged). This lets any producer feed it safely, and is
/// what allows different consumers to present the simplified or the full schema by capability while
/// sharing one normalisation path.
/// </para>
/// <para>
/// Transformers are auto-discovered via <see cref="IDiscoverable"/>, registered through
/// <c>builder.AISimplifiedPropertyValueTransformers()</c>, and resolved by editor schema alias
/// (<see cref="ForPropertyEditorSchemaAlias"/>). They are pure — no database access.
/// </para>
/// </remarks>
public interface IAISimplifiedPropertyValueTransformer : IDiscoverable
{
    /// <summary>
    /// Gets the property editor schema alias this transformer applies to (e.g.
    /// <c>Umbraco.RichText</c>).
    /// </summary>
    string ForPropertyEditorSchemaAlias { get; }

    /// <summary>
    /// Gets the simplified, strict-representable JSON Schema (draft 2020-12) the LLM should generate
    /// the property value against.
    /// </summary>
    /// <param name="dataTypeKey">
    /// The data type key, allowing the transformer to tailor the schema to the data type's
    /// configuration.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The simplified schema, or <c>null</c> to opt out for this data type (the caller then falls
    /// back to the editor's write schema).
    /// </returns>
    Task<JsonNode?> GetSimplifiedSchemaAsync(Guid dataTypeKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms a value the LLM produced against <see cref="GetSimplifiedSchemaAsync"/> into the
    /// editor's real write value. Pure; runs server-side.
    /// </summary>
    /// <param name="simplifiedValue">
    /// The value the LLM generated against the simplified schema. Defensively, an already-write-shaped
    /// value should be returned unchanged.
    /// </param>
    /// <param name="currentValue">
    /// The property's existing write-shape value (may be <c>null</c>), so the transform can preserve
    /// parts of it — e.g. keep a rich-text property's existing inline blocks.
    /// </param>
    /// <param name="dataTypeKey">The data type key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The editor's write value.</returns>
    Task<JsonNode?> TransformToWriteValueAsync(
        JsonNode? simplifiedValue,
        JsonNode? currentValue,
        Guid dataTypeKey,
        CancellationToken cancellationToken = default);
}
