using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Extensions;

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
        ContentEditorValueReader valueReader,
        ITemplateService templateService,
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

        if (!TryResolveCulture(content, culture, out culture, out var cultureError))
        {
            return ContentPropertyValueOperationOutcome.Fail(cultureError);
        }

        var documentMetadata = new AIDocumentMetadata(
            content.ContentType.Key,
            [culture is not null || segment is not null ? new AIVariantId(culture, segment) : AIVariantId.Invariant],
            content.ContentType.Variations.HasFlag(ContentVariation.Culture),
            content.ContentType.Variations.HasFlag(ContentVariation.Segment),
            content.Name);

        // The root property is read and written for the edited variant narrowed to its own variance:
        // Property.GetValue returns null for a culture on an invariant property, which would make the
        // dispatcher start from an empty value. Nested block values are narrowed by the dispatcher.
        // The dispatcher works on the editor format — the same shape the backoffice and the LLM's own
        // values use — so the new root value can be handed straight back to IContentEditingService.
        var rootProperty = content.Properties.FirstOrDefault(p => p.Alias == rootAlias);
        var rootVariant = rootProperty is null
            ? new AIVariantId(culture, segment)
            : AIVariantId.ForVariations(new AIVariantId(culture, segment), rootProperty.PropertyType.Variations);

        var rootValue = rootProperty is null
            ? null
            : valueReader.GetEditorValueNode(rootProperty, rootVariant.Culture, rootVariant.Segment);

        var request = new AIPropertyValueDispatchRequest(segments, operation, args, rootValue, documentMetadata);
        var dispatchResult = await dispatcher.DispatchAsync(request, cancellationToken);
        if (!dispatchResult.Success)
        {
            return ContentPropertyValueOperationOutcome.Fail(dispatchResult.Error!.Message);
        }

        var properties = new List<PropertyValueModel>
        {
            new()
            {
                Alias = rootAlias,
                Value = dispatchResult.NewRootValue is { } newRootValue
                    ? NormalizeIncomingValue(newRootValue.Deserialize<JsonElement>())
                    : null,
                Culture = rootVariant.Culture,
                Segment = rootVariant.Segment,
            },
        };

        // Like UpdateUmbracoContentTool, send back every other property's current value, or the CMS
        // clears them (see ContentEditorValueReader.GetUnchangedValues).
        properties.AddRange(valueReader.GetUnchangedValues(content, new HashSet<string> { rootAlias }, culture));

        var updateModel = new ContentUpdateModel
        {
            Properties = properties,
            // ContentEditingServiceBase.TryGetAndValidateContentType requires at least one Variants entry
            // matching the content type's own variance — an invariant type demands one with
            // Culture/Segment both null, otherwise it fails with ContentTypeCultureVarianceMismatch even
            // though nothing here is actually changing the name. Reuse the current name unchanged.
            Variants = [new VariantModel { Name = ContentToolHelpers.GetCurrentName(content, culture), Culture = culture, Segment = segment }],
            TemplateKey = await ContentToolHelpers.GetTemplateKeyAsync(templateService, content),
        };

        var updateAttempt = await contentEditingService.UpdateAsync(contentKey, updateModel, authResult.UserKey!.Value);
        if (!updateAttempt.Success)
        {
            return ContentPropertyValueOperationOutcome.Fail(updateAttempt.Status.ToMessage());
        }

        // A value in the wrong shape (e.g. rich text sent as something other than { markup, blocks })
        // passes validation but is turned into nothing by the property editor, so the save "succeeds"
        // while the property is emptied. Check the saved value and put the old one back if that happened.
        if (operation == AIPropertyOperation.SetValue
            && HasContent(args?["value"])
            && dispatcher is IAIPropertyValueReader reader
            && updateAttempt.Result.Content is { } savedContent
            && savedContent.Properties.FirstOrDefault(p => p.Alias == rootAlias) is { } savedRootProperty)
        {
            var savedRequest = request with
            {
                RootValue = valueReader.GetEditorValueNode(savedRootProperty, rootVariant.Culture, rootVariant.Segment),
            };

            if (reader.TryReadValue(savedRequest, out var savedValue) && !HasContent(savedValue))
            {
                properties[0].Value = rootValue is null ? null : NormalizeIncomingValue(rootValue.Deserialize<JsonElement>());
                var restoreAttempt = await contentEditingService.UpdateAsync(contentKey, updateModel, authResult.UserKey!.Value);

                return ContentPropertyValueOperationOutcome.Fail(
                    $"The value for '{path[^1].Alias}' was not saved because the property editor could not read it" +
                    (restoreAttempt.Success ? ", so the previous value has been kept. " : ", and the previous value could not be restored. ") +
                    "Call get_property_value_schema to check the expected shape and try again.");
            }
        }

        return ContentPropertyValueOperationOutcome.Ok(dispatchResult.BlockKey);
    }

    /// <summary>
    /// Fills in the culture when the caller left it out. On a culture-variant item, a missing culture
    /// reads every culture-variant property as empty, which surfaces as misleading errors further down
    /// (e.g. "Block was not found") — so use the item's only culture when there is one, and otherwise
    /// ask for it, listing the options.
    /// </summary>
    internal static bool TryResolveCulture(IContent content, string? culture, out string? resolvedCulture, [NotNullWhen(false)] out string? error)
    {
        resolvedCulture = string.IsNullOrWhiteSpace(culture) ? null : culture;
        error = null;

        if (resolvedCulture is not null || !content.ContentType.Variations.HasFlag(ContentVariation.Culture))
        {
            return true;
        }

        var cultures = content.AvailableCultures.ToArray();
        if (cultures.Length == 1)
        {
            resolvedCulture = cultures[0];
            return true;
        }

        error = cultures.Length == 0
            ? "This content item varies by culture, so a Culture is required."
            : $"This content item varies by culture, so a Culture is required. Available cultures: {string.Join(", ", cultures)}.";
        return false;
    }

    /// <summary>
    /// Whether a value carries something to store: not null, not an empty string, array or object.
    /// </summary>
    private static bool HasContent(JsonNode? value) => value switch
    {
        null => false,
        JsonValue v when v.GetValueKind() == JsonValueKind.Null => false,
        JsonValue v when v.GetValueKind() == JsonValueKind.String => !string.IsNullOrWhiteSpace(v.GetValue<string>()),
        JsonArray a => a.Count > 0,
        JsonObject o => o.Count > 0,
        _ => true,
    };

    private static AIPropertyPathSegment ToSegment(UmbracoPropertyPathSegmentArg segment) => segment switch
    {
        { Alias: { } alias, BlockKey: null } => AIPropertyPathSegment.ForProperty(alias),
        { Alias: null, BlockKey: { } blockKey } => AIPropertyPathSegment.ForBlock(blockKey),
        _ => throw new ArgumentException("Each path segment must set exactly one of Alias or BlockKey."),
    };

    /// <summary>
    /// Converts a raw stored property value into a <see cref="JsonNode"/> — used both by the dispatcher
    /// here and by <see cref="UpdateUmbracoContentTool"/> to round-trip untouched properties' current
    /// values back into a <see cref="PropertyValueModel"/> so they survive a patch update. A block
    /// editor's stored value is a JSON string (the envelope) and parses directly; a scalar editor's
    /// stored value is often a plain, non-JSON string (e.g. "Hello World" from a text box) that would
    /// throw if parsed as JSON, so it falls back to wrapping it as a JSON string value instead.
    /// </summary>
    internal static JsonNode? ToJsonNode(object? value)
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

    /// <summary>
    /// Converts a <see cref="JsonElement"/> — the shape a tool's JSON arguments and a round-tripped
    /// current value both arrive as — into the same CLR shape the real backoffice save path produces,
    /// by reusing <see cref="JsonObjectConverter"/> (the converter the Management API registers for its
    /// <c>object</c>-typed property-value model). Left as a raw <see cref="JsonElement"/>, a JSON
    /// <c>true</c>/number/array doesn't match the concrete CLR types or <see cref="JsonNode"/> subtypes
    /// that some property editors' <c>FromEditor</c> override pattern-matches on (e.g.
    /// <c>Umbraco.TrueFalse</c> checks for <c>bool</c>/<c>int</c>/<c>string</c>,
    /// <c>Umbraco.MultiNodeTreePicker</c> checks for <see cref="JsonArray"/>) — those checks silently
    /// fall through to a default/empty value instead of erroring, so the save reports success while the
    /// value doesn't persist. See umbraco/Umbraco.AI#408.
    /// </summary>
    internal static object? NormalizeIncomingValue(JsonElement value)
        => JsonSerializer.Deserialize<object?>(value.GetRawText(), NormalizationSerializerOptions);

    private static readonly JsonSerializerOptions NormalizationSerializerOptions = new()
    {
        Converters = { new JsonObjectConverter() },
    };
}
