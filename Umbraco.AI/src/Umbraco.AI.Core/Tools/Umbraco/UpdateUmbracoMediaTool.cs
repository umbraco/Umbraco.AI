using System.ComponentModel;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
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
[AITool("update_umbraco_media", "Update Umbraco Media", ScopeId = MediaWriteScope.ScopeId, IsDestructive = true, RequiresApproval = false)]
public class UpdateUmbracoMediaTool(
    IMediaEditingService mediaEditingService,
    IUmbracoWriteAuthorizer authorizer,
    PropertyEditorCollection propertyEditors,
    IJsonSerializer jsonSerializer)
    : AIToolBase<UpdateUmbracoMediaArgs>
{
    private readonly ContentEditorValueReader _valueReader = new(propertyEditors, jsonSerializer);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUmbracoMediaTool"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts a PropertyEditorCollection and an IJsonSerializer. Will be removed in v20")]
    public UpdateUmbracoMediaTool(
        IMediaEditingService mediaEditingService,
        IUmbracoWriteAuthorizer authorizer)
        : this(
            mediaEditingService,
            authorizer,
            StaticServiceProvider.Instance.GetRequiredService<PropertyEditorCollection>(),
            StaticServiceProvider.Instance.GetRequiredService<IJsonSerializer>())
    {
    }

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

        var existing = await mediaEditingService.GetAsync(args.Key);
        if (existing is null)
        {
            return new UpdateUmbracoMediaResult(false, null, $"Media with key '{args.Key}' was not found.");
        }

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value })
            .ToList();

        // Send back every property this update isn't changing, or the CMS clears them, including the
        // media file itself (see ContentEditorValueReader.GetUnchangedValues).
        properties.AddRange(_valueReader.GetUnchangedValues(existing, (args.PropertyValues ?? []).Keys.ToHashSet(), culture: null));

        // ContentEditingServiceBase.TryGetAndValidateContentType (shared with media) requires at least
        // one Variants entry matching the media type's own variance regardless of what Properties
        // contains — an invariant type demands one with Culture/Segment both null, otherwise the whole
        // update fails with ContentTypeCultureVarianceMismatch even when Name isn't changing. When no new
        // name was given, reuse the current one so the Variants entry is present without altering it.
        var variantName = args.Name ?? existing.Name ?? string.Empty;

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
