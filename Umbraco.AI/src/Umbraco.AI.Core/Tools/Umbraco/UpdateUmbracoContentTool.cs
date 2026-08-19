using System.ComponentModel;
using System.Text.Json;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the UpdateUmbracoContent tool.
/// </summary>
public record UpdateUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Optional new name for the content item. Omit to leave the name unchanged.")]
    string? Name,

    [property: Description("Optional simple/scalar property values to set, keyed by property alias. NOT suitable for Block List, Block Grid, or other structured properties — use set_umbraco_content_value/add_umbraco_content_item for those instead. Call get_content_type_schema first to see each property's expected value shape.")]
    Dictionary<string, JsonElement>? PropertyValues,

    [property: Description("Optional culture code for variant content (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that updates an existing Umbraco content item's name and/or simple property values. Changes
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
        "Updates an existing Umbraco content item's name and/or simple property values, saving as a draft. " +
        "Call publish_umbraco_content afterward to make the changes live. Call get_content_type_schema first " +
        "to discover valid property aliases and value shapes. Only simple/scalar property values can be set " +
        "here — for Block List, Block Grid, or other structured properties, use set_umbraco_content_value or " +
        "add_umbraco_content_item instead.";

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

        var properties = (args.PropertyValues ?? [])
            .Select(kvp => new PropertyValueModel { Alias = kvp.Key, Value = kvp.Value, Culture = args.Culture })
            .ToList();

        // ContentEditingServiceBase.TryGetAndValidateContentType requires at least one Variants entry
        // matching the content type's own variance regardless of what Properties contains — an invariant
        // type demands one with Culture/Segment both null, otherwise the whole update fails with
        // ContentTypeCultureVarianceMismatch even when Name isn't changing. When no new name was given,
        // load the current one so the Variants entry is still present without altering it.
        string variantName;
        if (args.Name is not null)
        {
            variantName = args.Name;
        }
        else
        {
            var existing = await contentEditingService.GetAsync(args.Key);
            if (existing is null)
            {
                return new UpdateUmbracoContentResult(false, null, $"Content with key '{args.Key}' was not found.");
            }

            variantName = existing.Name ?? string.Empty;
        }

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
}

/// <summary>
/// Result of the update Umbraco content tool.
/// </summary>
public record UpdateUmbracoContentResult(
    bool Success,
    UmbracoContentItem? Content,
    string? Message);
