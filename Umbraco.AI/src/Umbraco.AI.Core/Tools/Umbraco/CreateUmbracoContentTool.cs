using System.ComponentModel;
using System.Text.Json;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the CreateUmbracoContent tool.
/// </summary>
public record CreateUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the parent content item, or omit/null to create at the root.")]
    Guid? ParentKey,

    [property: Description("The alias of the content type to create (e.g., 'blogPost'). Use get_content_type_schema to discover valid aliases and their property schemas first.")]
    string ContentTypeAlias,

    [property: Description("The name of the new content item.")]
    string Name,

    [property: Description("Optional simple/scalar property values, keyed by property alias (e.g., text, number, date, toggle, dropdown, single pickers). NOT suitable for Block List, Block Grid, or other structured properties — use add_umbraco_content_item after creation for those instead. Call get_content_type_schema first to see each property's expected value shape.")]
    Dictionary<string, JsonElement>? PropertyValues,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that creates a new Umbraco content item as a draft (unpublished). Use publish_umbraco_content
/// afterward to make it live.
/// </summary>
[AITool("create_umbraco_content", "Create Umbraco Content", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class CreateUmbracoContentTool(
    IContentEditingService contentEditingService,
    IContentTypeService contentTypeService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<CreateUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Creates a new Umbraco content item as a draft (not published). Returns the created item's key, " +
        "which you'll need for further edits, add_umbraco_content_item calls, or publish_umbraco_content. " +
        "Call get_content_type_schema first to discover the content type's valid property aliases and value " +
        "shapes. Only simple/scalar property values can be set here — for Block List, Block Grid, or other " +
        "structured properties, create the item first (with or without other property values) and then use " +
        "add_umbraco_content_item to populate them.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(CreateUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(args.ContentTypeAlias))
        {
            return new CreateUmbracoContentResult(false, null, "Content type alias cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(args.Name))
        {
            return new CreateUmbracoContentResult(false, null, "Name cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeContentAsync(ActionNew.ActionLetter, args.ParentKey);
        if (!authResult.IsAuthorized)
        {
            return new CreateUmbracoContentResult(false, null, authResult.Message);
        }

        var contentType = contentTypeService.Get(args.ContentTypeAlias);
        if (contentType is null)
        {
            return new CreateUmbracoContentResult(false, null, $"Content type '{args.ContentTypeAlias}' was not found.");
        }

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value, Culture = args.Culture })
            .ToList();

        var createModel = new ContentCreateModel
        {
            ContentTypeKey = contentType.Key,
            ParentKey = args.ParentKey,
            Properties = properties,
            Variants = [new VariantModel { Name = args.Name, Culture = args.Culture }],
        };

        var attempt = await contentEditingService.CreateAsync(createModel, authResult.UserKey!.Value);
        if (!attempt.Success || attempt.Result.Content is null)
        {
            return new CreateUmbracoContentResult(false, null, attempt.Status.ToMessage());
        }

        return new CreateUmbracoContentResult(
            true,
            ContentToolHelpers.BuildContentItem(attempt.Result.Content, args.Culture),
            null);
    }
}

/// <summary>
/// Result of the create Umbraco content tool.
/// </summary>
public record CreateUmbracoContentResult(
    bool Success,
    UmbracoContentItem? Content,
    string? Message);
