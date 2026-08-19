using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// One segment of a property value path, in the schema-friendly shape exposed to the LLM. Exactly one
/// of <see cref="Alias"/> or <see cref="BlockKey"/> must be set.
/// </summary>
/// <remarks>
/// The dispatcher's own <c>AIPropertyPathSegment</c> is an abstract record with a custom
/// <see cref="System.Text.Json.Serialization.JsonConverter"/> — reusing it directly as a tool argument
/// type risks the same unconstrained-schema problem that broke OpenAI's strict structured-output mode
/// for the AI Prompt wand on rich-text/block properties. This flat, two-field shape has a clean,
/// unambiguous JSON schema and is converted internally before calling the dispatcher.
/// </remarks>
public record UmbracoPropertyPathSegmentArg(
    [property: Description("Set this when the segment identifies a property by alias (e.g. 'contentBlocks'). Leave null when BlockKey is set instead.")]
    string? Alias,

    [property: Description("Set this when the segment identifies a block within a collection property, by its key (from a prior add_umbraco_content_item call). Leave null when Alias is set instead.")]
    Guid? BlockKey);

/// <summary>
/// Outcome of a property value operation, before being mapped into a tool-specific result record.
/// </summary>
internal sealed record ContentPropertyValueOperationOutcome(bool Success, Guid? BlockKey, string? Message)
{
    public static ContentPropertyValueOperationOutcome Fail(string message) => new(false, null, message);

    public static ContentPropertyValueOperationOutcome Ok(Guid? blockKey = null) => new(true, blockKey, null);
}

/// <summary>
/// Shared orchestration for the five content property-value tools (set/add/remove/move/clear). Each
/// tool authorizes, loads the persisted content item, reads the target property's current value,
/// dispatches the requested operation through <see cref="IAIPropertyValueDispatcher"/> — the same
/// engine the frontend's block-editing tools use, reused in-process here rather than reimplemented —
/// and persists the result. Consolidated into one helper so the five tools can't drift out of sync
/// with each other on this multi-step recipe.
/// </summary>
internal static class ContentPropertyValueOperationHelper
{
    public static async Task<ContentPropertyValueOperationOutcome> ExecuteAsync(
        IUmbracoWriteAuthorizer authorizer,
        IContentEditingService contentEditingService,
        IAIPropertyValueDispatcher dispatcher,
        Guid contentKey,
        IReadOnlyList<UmbracoPropertyPathSegmentArg>? path,
        AIPropertyOperation operation,
        JsonNode? args,
        string? culture,
        string? segment,
        CancellationToken cancellationToken)
    {
        if (contentKey == Guid.Empty)
        {
            return ContentPropertyValueOperationOutcome.Fail("Content key cannot be empty.");
        }

        if (path is null || path.Count == 0)
        {
            return ContentPropertyValueOperationOutcome.Fail("Path must contain at least one segment.");
        }

        if (path[0].Alias is not { } rootAlias || path[0].BlockKey is not null)
        {
            return ContentPropertyValueOperationOutcome.Fail("Path must begin with a property alias segment (Alias set, BlockKey null).");
        }

        AIPropertyPathSegment[] segments;
        try
        {
            segments = path.Select(ToSegment).ToArray();
        }
        catch (ArgumentException ex)
        {
            return ContentPropertyValueOperationOutcome.Fail(ex.Message);
        }

        var authResult = await authorizer.AuthorizeContentAsync(ActionUpdate.ActionLetter, contentKey);
        if (!authResult.IsAuthorized)
        {
            return ContentPropertyValueOperationOutcome.Fail(authResult.Message!);
        }

        var content = await contentEditingService.GetAsync(contentKey);
        if (content is null)
        {
            return ContentPropertyValueOperationOutcome.Fail($"Content with key '{contentKey}' was not found.");
        }

        var documentMetadata = new AIDocumentMetadata(
            content.ContentType.Key,
            [culture is not null || segment is not null ? new AIVariantId(culture, segment) : AIVariantId.Invariant],
            content.ContentType.Variations.HasFlag(ContentVariation.Culture),
            content.ContentType.Variations.HasFlag(ContentVariation.Segment),
            content.Name);

        var rootValue = ToJsonNode(content.GetValue(rootAlias, culture, segment));

        var request = new AIPropertyValueDispatchRequest(segments, operation, args, rootValue, documentMetadata);
        var dispatchResult = await dispatcher.DispatchAsync(request, cancellationToken);
        if (!dispatchResult.Success)
        {
            return ContentPropertyValueOperationOutcome.Fail(dispatchResult.Error!.Message);
        }

        var updateModel = new ContentUpdateModel
        {
            Properties =
            [
                new PropertyValueModel
                {
                    Alias = rootAlias,
                    Value = dispatchResult.NewRootValue?.Deserialize<JsonElement>(),
                    Culture = culture,
                    Segment = segment,
                },
            ],
            // ContentEditingServiceBase.TryGetAndValidateContentType requires at least one Variants entry
            // matching the content type's own variance — an invariant type demands one with
            // Culture/Segment both null, otherwise it fails with ContentTypeCultureVarianceMismatch even
            // though nothing here is actually changing the name. Reuse the current name unchanged.
            Variants = [new VariantModel { Name = content.Name ?? string.Empty, Culture = culture, Segment = segment }],
        };

        var updateAttempt = await contentEditingService.UpdateAsync(contentKey, updateModel, authResult.UserKey!.Value);
        if (!updateAttempt.Success)
        {
            return ContentPropertyValueOperationOutcome.Fail(updateAttempt.Status.ToMessage());
        }

        return ContentPropertyValueOperationOutcome.Ok(dispatchResult.BlockKey);
    }

    private static AIPropertyPathSegment ToSegment(UmbracoPropertyPathSegmentArg segment) => segment switch
    {
        { Alias: { } alias, BlockKey: null } => AIPropertyPathSegment.ForProperty(alias),
        { Alias: null, BlockKey: { } blockKey } => AIPropertyPathSegment.ForBlock(blockKey),
        _ => throw new ArgumentException("Each path segment must set exactly one of Alias or BlockKey."),
    };

    /// <summary>
    /// Converts a raw stored property value into a <see cref="JsonNode"/> for the dispatcher. A block
    /// editor's stored value is a JSON string (the envelope) and parses directly; a scalar editor's
    /// stored value is often a plain, non-JSON string (e.g. "Hello World" from a text box) that would
    /// throw if parsed as JSON, so it falls back to wrapping it as a JSON string value instead.
    /// </summary>
    private static JsonNode? ToJsonNode(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonNode node:
                return node;
            case string s:
                try
                {
                    return JsonNode.Parse(s);
                }
                catch (JsonException)
                {
                    return JsonValue.Create(s);
                }
            default:
                return JsonSerializer.SerializeToNode(value);
        }
    }
}
