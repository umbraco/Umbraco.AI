using System.ComponentModel;
using System.Text.Json;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the UpdateUmbracoMedia tool.
/// </summary>
public record UpdateUmbracoMediaArgs(
    [property: Description("The unique key (GUID) of the media item to update.")]
    Guid Key,

    [property: Description("Optional new name for the media item. Omit to leave the name unchanged.")]
    string? Name,

    [property: Description("Optional simple/scalar property values to set, keyed by property alias. Call get_content_type_schema first to see each property's expected value shape.")]
    Dictionary<string, JsonElement>? PropertyValues);

/// <summary>
/// Tool that updates an existing Umbraco media item's name and/or property values.
/// </summary>
[AITool("update_umbraco_media", "Update Umbraco Media", ScopeId = MediaWriteScope.ScopeId, IsDestructive = true)]
public class UpdateUmbracoMediaTool(
    IMediaEditingService mediaEditingService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<UpdateUmbracoMediaArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Updates an existing Umbraco media item's name and/or simple property values. Call " +
        "get_content_type_schema first to discover valid property aliases and value shapes.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(UpdateUmbracoMediaArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new UpdateUmbracoMediaResult(false, null, "Media key cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeMediaAsync(args.Key);
        if (!authResult.IsAuthorized)
        {
            return new UpdateUmbracoMediaResult(false, null, authResult.Message);
        }

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value })
            .ToList();

        // ContentEditingServiceBase.TryGetAndValidateContentType (shared with media) requires at least
        // one Variants entry matching the media type's own variance regardless of what Properties
        // contains — an invariant type demands one with Culture/Segment both null, otherwise the whole
        // update fails with ContentTypeCultureVarianceMismatch even when Name isn't changing. When no new
        // name was given, load the current one so the Variants entry is still present without altering it.
        string variantName;
        if (args.Name is not null)
        {
            variantName = args.Name;
        }
        else
        {
            var existing = await mediaEditingService.GetAsync(args.Key);
            if (existing is null)
            {
                return new UpdateUmbracoMediaResult(false, null, $"Media with key '{args.Key}' was not found.");
            }

            variantName = existing.Name ?? string.Empty;
        }

        var updateModel = new MediaUpdateModel
        {
            Properties = properties,
            Variants = [new VariantModel { Name = variantName }],
        };

        var attempt = await mediaEditingService.UpdateAsync(args.Key, updateModel, authResult.UserKey!.Value);
        if (!attempt.Success || attempt.Result.Content is null)
        {
            return new UpdateUmbracoMediaResult(false, null, attempt.Status.ToMessage());
        }

        return new UpdateUmbracoMediaResult(true, ContentToolHelpers.BuildMediaItem(attempt.Result.Content), null);
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(UpdateUmbracoMediaArgs args)
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

        return parts.Count == 0 ? null : $"Update this media item: {string.Join(" and ", parts)}.";
    }
}

/// <summary>
/// Result of the update Umbraco media tool.
/// </summary>
public record UpdateUmbracoMediaResult(
    bool Success,
    UmbracoMediaItem? Media,
    string? Message);
