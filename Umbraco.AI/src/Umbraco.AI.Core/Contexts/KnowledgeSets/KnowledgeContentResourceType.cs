using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// The single, Core-internal resource type that materialises knowledge-set content.
/// </summary>
/// <remarks>
/// <para>
/// This is the deferred-fetch seam for knowledge sets. The <see cref="KnowledgeSetContextResolver"/>
/// emits each item as an OnDemand resource carrying a <see cref="KnowledgeContentRef"/> (a reference,
/// never the content). At format time — the already-async <see cref="ResolveDataAsync"/>, only invoked
/// when the LLM actually retrieves the resource — this type re-locates the set + item and awaits the
/// item's <see cref="AIKnowledgeSetItem.GetContentAsync"/>, so content is materialised lazily.
/// </para>
/// <para>
/// It is deliberately <em>not</em> derived from <see cref="AIContextResourceTypeBase{TSettings}"/> and
/// <em>not</em> attribute-decorated: the base ctor requires an <c>[AIContextResourceType]</c> attribute
/// and routes settings through <see cref="IAIEditableModelResolver"/> (the <c>$</c>-config resolution
/// path knowledge content must avoid). Implementing <see cref="IAIContextResourceType"/> directly keeps
/// content literal and keeps the type out of attribute-based discovery — it is registered explicitly and
/// is invisible to package authors and the Context resource-type picker.
/// </para>
/// </remarks>
internal sealed class KnowledgeContentResourceType : IAIContextResourceType
{
    /// <summary>
    /// The immutable identifier of this resource type. Referenced by
    /// <see cref="KnowledgeSetContextResolver"/> when emitting resources.
    /// </summary>
    public const string TypeId = "knowledge-content";

    private readonly AIKnowledgeSetCollection _knowledgeSets;
    private readonly ILogger<KnowledgeContentResourceType> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContentResourceType"/> class.
    /// </summary>
    /// <param name="knowledgeSets">The collection of discovered knowledge sets.</param>
    /// <param name="logger">The logger, used to record graceful-degradation failures.</param>
    public KnowledgeContentResourceType(
        AIKnowledgeSetCollection knowledgeSets,
        ILogger<KnowledgeContentResourceType> logger)
    {
        _knowledgeSets = knowledgeSets;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Id => TypeId;

    /// <inheritdoc />
    public string Name => "Knowledge Content";

    /// <inheritdoc />
    public string? Description => "Internal resource type that materialises knowledge-set item content on demand.";

    /// <inheritdoc />
    public string? Icon => "icon-book";

    /// <inheritdoc />
    public Type? SettingsType => typeof(KnowledgeContentRef);

    /// <inheritdoc />
    // No settings UI — this type is never author-configured, so it exposes no schema.
    public AIEditableModelSchema? GetSettingsSchema() => null;

    /// <inheritdoc />
    public Type? DataType => typeof(string);

    /// <inheritdoc />
    public async Task<object?> ResolveDataAsync(object? settings, CancellationToken cancellationToken = default)
    {
        var reference = AsReference(settings);
        if (reference is null)
        {
            _logger.LogWarning("Knowledge content resource had no {Ref} settings; returning empty content.",
                nameof(KnowledgeContentRef));
            return string.Empty;
        }

        var knowledgeSet = _knowledgeSets.GetById(reference.KnowledgeSetId);
        if (knowledgeSet is null)
        {
            // Graceful degradation: a set removed since the resource was resolved should not fail the
            // whole request. Surface an empty/error block to the model instead.
            _logger.LogWarning("Knowledge set '{KnowledgeSetId}' was not found; returning empty content.",
                reference.KnowledgeSetId);
            return string.Empty;
        }

        try
        {
            var items = await knowledgeSet.GetItemsAsync(cancellationToken);
            var item = items.FirstOrDefault(i =>
                string.Equals(i.Key, reference.ItemKey, StringComparison.Ordinal));

            if (item is null)
            {
                _logger.LogWarning(
                    "Knowledge item '{ItemKey}' was not found in set '{KnowledgeSetId}'; returning empty content.",
                    reference.ItemKey, reference.KnowledgeSetId);
                return string.Empty;
            }

            return await item.GetContentAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a degradation case — let it propagate.
            throw;
        }
        catch (Exception ex)
        {
            // A throwing content producer degrades gracefully rather than failing the whole request.
            _logger.LogError(ex,
                "Failed to materialise knowledge content for item '{ItemKey}' in set '{KnowledgeSetId}'.",
                reference.ItemKey, reference.KnowledgeSetId);
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string FormatDataForLlm(object? data)
        => data as string ?? string.Empty;

    // Settings flow through the resolution pipeline by reference verbatim (see AIContextResolutionService),
    // so the common case is a direct cast. Fall back to JSON deserialization to stay robust if the resource
    // ever round-trips through serialization (e.g. telemetry).
    private static KnowledgeContentRef? AsReference(object? settings)
    {
        switch (settings)
        {
            case null:
                return null;
            case KnowledgeContentRef reference:
                return reference;
            case JsonElement jsonElement:
                return jsonElement.Deserialize<KnowledgeContentRef>(Constants.DefaultJsonSerializerOptions);
            default:
                var json = JsonSerializer.Serialize(settings, Constants.DefaultJsonSerializerOptions);
                return JsonSerializer.Deserialize<KnowledgeContentRef>(json, Constants.DefaultJsonSerializerOptions);
        }
    }
}
