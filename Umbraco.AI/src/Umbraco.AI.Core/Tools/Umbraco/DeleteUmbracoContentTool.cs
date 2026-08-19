using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the DeleteUmbracoContent tool.
/// </summary>
public record DeleteUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to delete.")]
    Guid Key);

/// <summary>
/// Tool that moves a content item to the recycle bin (soft delete, reversible) — the same behavior as
/// a human editor's Delete action. Does not permanently remove the item.
/// </summary>
[AITool("delete_umbraco_content", "Delete Umbraco Content", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class DeleteUmbracoContentTool(
    IContentEditingService contentEditingService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<DeleteUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Moves an Umbraco content item to the recycle bin. This is a soft delete (reversible), the same " +
        "as a human editor's Delete action — it does not permanently remove the item.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(DeleteUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new DeleteUmbracoContentResult(false, "Content key cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeContentAsync(ActionDelete.ActionLetter, args.Key);
        if (!authResult.IsAuthorized)
        {
            return new DeleteUmbracoContentResult(false, authResult.Message);
        }

        var attempt = await contentEditingService.MoveToRecycleBinAsync(args.Key, authResult.UserKey!.Value);

        return attempt.Success
            ? new DeleteUmbracoContentResult(true, null)
            : new DeleteUmbracoContentResult(false, attempt.Status.ToMessage());
    }
}

/// <summary>
/// Result of the delete Umbraco content tool.
/// </summary>
public record DeleteUmbracoContentResult(
    bool Success,
    string? Message);
