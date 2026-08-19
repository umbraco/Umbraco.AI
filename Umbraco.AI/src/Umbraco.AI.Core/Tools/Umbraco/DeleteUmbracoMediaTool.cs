using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the DeleteUmbracoMedia tool.
/// </summary>
public record DeleteUmbracoMediaArgs(
    [property: Description("The unique key (GUID) of the media item to delete.")]
    Guid Key);

/// <summary>
/// Tool that moves a media item to the recycle bin (soft delete, reversible) — the same behavior as
/// a human editor's Delete action. Does not permanently remove the item.
/// </summary>
[AITool("delete_umbraco_media", "Delete Umbraco Media", ScopeId = MediaWriteScope.ScopeId, IsDestructive = true)]
public class DeleteUmbracoMediaTool(
    IMediaEditingService mediaEditingService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<DeleteUmbracoMediaArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Moves an Umbraco media item to the recycle bin. This is a soft delete (reversible), the same " +
        "as a human editor's Delete action — it does not permanently remove the item.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(DeleteUmbracoMediaArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new DeleteUmbracoMediaResult(false, "Media key cannot be empty.");
        }

        var authResult = await authorizer.AuthorizeMediaAsync(args.Key);
        if (!authResult.IsAuthorized)
        {
            return new DeleteUmbracoMediaResult(false, authResult.Message);
        }

        var attempt = await mediaEditingService.MoveToRecycleBinAsync(args.Key, authResult.UserKey!.Value);

        return attempt.Success
            ? new DeleteUmbracoMediaResult(true, null)
            : new DeleteUmbracoMediaResult(false, attempt.Status.ToMessage());
    }
}

/// <summary>
/// Result of the delete Umbraco media tool.
/// </summary>
public record DeleteUmbracoMediaResult(
    bool Success,
    string? Message);
