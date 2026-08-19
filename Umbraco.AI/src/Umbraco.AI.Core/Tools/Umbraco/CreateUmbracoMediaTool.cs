using System.ComponentModel;
using System.Text.Json;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the CreateUmbracoMedia tool.
/// </summary>
public record CreateUmbracoMediaArgs(
    [property: Description("The unique key (GUID) of the parent media folder/item, or omit/null to create at the root.")]
    Guid? ParentKey,

    [property: Description("The alias of the media type to create (e.g., 'image', 'file', 'folder'). Use get_content_type_schema to discover valid aliases and their property schemas first.")]
    string MediaTypeAlias,

    [property: Description("The name of the new media item.")]
    string Name,

    [property: Description("Optional simple/scalar property values, keyed by property alias. Call get_content_type_schema first to see each property's expected value shape.")]
    Dictionary<string, JsonElement>? PropertyValues);

/// <summary>
/// Tool that creates a new Umbraco media item (e.g. a folder, or a file/image record — note this does
/// not upload binary file content; it creates the media item's metadata).
/// </summary>
[AITool("create_umbraco_media", "Create Umbraco Media", ScopeId = MediaWriteScope.ScopeId, IsDestructive = true)]
public class CreateUmbracoMediaTool(
    IMediaEditingService mediaEditingService,
    IMediaTypeService mediaTypeService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<CreateUmbracoMediaArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Creates a new Umbraco media item (e.g. a folder). Returns the created item's key. Call " +
        "get_content_type_schema first to discover the media type's valid property aliases and value shapes.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(CreateUmbracoMediaArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(args.MediaTypeAlias))
        {
            return new CreateUmbracoMediaResult(false, null, "Media type alias cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(args.Name))
        {
            return new CreateUmbracoMediaResult(false, null, "Name cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeMediaAsync(args.ParentKey);
        if (!authResult.IsAuthorized)
        {
            return new CreateUmbracoMediaResult(false, null, authResult.Message);
        }

        var mediaType = mediaTypeService.Get(args.MediaTypeAlias);
        if (mediaType is null)
        {
            return new CreateUmbracoMediaResult(false, null, $"Media type '{args.MediaTypeAlias}' was not found.");
        }

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value })
            .ToList();

        var createModel = new MediaCreateModel
        {
            ContentTypeKey = mediaType.Key,
            ParentKey = args.ParentKey,
            Properties = properties,
            Variants = [new VariantModel { Name = args.Name }],
        };

        var attempt = await mediaEditingService.CreateAsync(createModel, authResult.UserKey!.Value);
        if (!attempt.Success || attempt.Result.Content is null)
        {
            return new CreateUmbracoMediaResult(false, null, attempt.Status.ToMessage());
        }

        return new CreateUmbracoMediaResult(true, ContentToolHelpers.BuildMediaItem(attempt.Result.Content), null);
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(CreateUmbracoMediaArgs args)
        => $"Create a new '{args.MediaTypeAlias}' media item named '{args.Name}'" +
           (args.ParentKey is { } parentKey ? $" under parent {parentKey}." : " at the root.");
}

/// <summary>
/// Result of the create Umbraco media tool.
/// </summary>
public record CreateUmbracoMediaResult(
    bool Success,
    UmbracoMediaItem? Media,
    string? Message);
