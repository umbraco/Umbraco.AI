using System.ComponentModel;
using System.Text.Json;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the UpdateUmbracoContent tool.
/// </summary>
public record UpdateUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Optional new name for the content item. Omit to leave the name unchanged.")]
    string? Name,

    [property: Description("Optional simple/scalar property values to patch, keyed by property alias. Only the aliases listed here are changed — every other property on the content item keeps its current value. NOT suitable for Block List, Block Grid, or other structured properties — use set_umbraco_content_value/add_umbraco_content_item for those instead. Call get_content_type_schema first to see each property's expected value shape.")]
    Dictionary<string, JsonElement>? PropertyValues,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that patches an existing Umbraco content item's name and/or simple property values — properties
/// not named in <see cref="UpdateUmbracoContentArgs.PropertyValues"/> keep their current value. Changes
/// are saved as a draft — call publish_umbraco_content afterward to make them live.
/// </summary>
[AITool("update_umbraco_content", "Update Umbraco Content", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class UpdateUmbracoContentTool(
    IContentEditingService contentEditingService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<UpdateUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Patches an existing Umbraco content item's name and/or simple property values, saving as a draft. " +
        "Only the fields you pass are changed — Name and PropertyValues are both optional, and any property " +
        "alias not included in PropertyValues keeps its current value. Call publish_umbraco_content afterward " +
        "to make the changes live. Call get_content_type_schema first to discover valid property aliases and " +
        "value shapes. Only simple/scalar property values can be set here — for Block List, Block Grid, or " +
        "other structured properties, use set_umbraco_content_value or add_umbraco_content_item instead.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(UpdateUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new UpdateUmbracoContentResult(false, null, "Content key cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeContentAsync(ActionUpdate.ActionLetter, args.Key);
        if (!authResult.IsAuthorized)
        {
            return new UpdateUmbracoContentResult(false, null, authResult.Message);
        }

        var existing = await contentEditingService.GetAsync(args.Key);
        if (existing is null)
        {
            return new UpdateUmbracoContentResult(false, null, $"Content with key '{args.Key}' was not found.");
        }

        var explicitAliases = (args.PropertyValues ?? []).Keys.ToHashSet();

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value, Culture = args.Culture })
            .ToList();

        // ContentEditingServiceBase.RemoveMissingProperties clears every property alias NOT present in
        // Properties on every save, so — unlike a create — this tool must resubmit every other property's
        // current value or an update that only names one field would silently wipe the rest of the content
        // item. Segment-varying properties are skipped here: this tool has no Segment argument to read/write
        // them correctly, so they're left with the pre-existing (removed-if-omitted) behavior rather than
        // risk a NotSupportedException from guessing a segment.
        foreach (var property in existing.Properties)
        {
            if (explicitAliases.Contains(property.Alias) || property.PropertyType.VariesBySegment())
            {
                continue;
            }

            var propertyCulture = property.PropertyType.VariesByCulture() ? args.Culture : null;
            var currentValue = ContentPropertyValueOperationHelper.ToJsonNode(property.GetValue(propertyCulture))?.Deserialize<JsonElement>();
            properties.Add(new PropertyValueModel { Alias = property.Alias, Value = currentValue, Culture = propertyCulture });
        }

        // ContentEditingServiceBase.TryGetAndValidateContentType requires at least one Variants entry
        // matching the content type's own variance regardless of what Properties contains — an invariant
        // type demands one with Culture/Segment both null, otherwise the whole update fails with
        // ContentTypeCultureVarianceMismatch even when Name isn't changing.
        var variantName = args.Name ?? existing.Name ?? string.Empty;

        var updateModel = new ContentUpdateModel
        {
            Properties = properties,
            Variants = [new VariantModel { Name = variantName, Culture = args.Culture }],
        };

        var attempt = await contentEditingService.UpdateAsync(args.Key, updateModel, authResult.UserKey!.Value);
        if (!attempt.Success || attempt.Result.Content is null)
        {
            return new UpdateUmbracoContentResult(false, null, attempt.Status.ToMessage());
        }

        return new UpdateUmbracoContentResult(
            true,
            ContentToolHelpers.BuildContentItem(attempt.Result.Content, args.Culture),
            null);
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(UpdateUmbracoContentArgs args)
    {
        var parts = new List<string>();
        if (args.Name is not null)
        {
            parts.Add($"rename it to '{args.Name}'");
        }

        if (args.PropertyValues is { Count: > 0 })
        {
            parts.Add($"update {string.Join(", ", args.PropertyValues.Keys.Select(a => $"'{a}'"))}");
        }

        return parts.Count == 0 ? null : $"Update this content item: {string.Join(" and ", parts)}.";
    }
}

/// <summary>
/// Result of the update Umbraco content tool.
/// </summary>
public record UpdateUmbracoContentResult(
    bool Success,
    UmbracoContentItem? Content,
    string? Message);
