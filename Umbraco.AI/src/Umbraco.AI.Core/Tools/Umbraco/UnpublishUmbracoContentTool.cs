using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the UnpublishUmbracoContent tool.
/// </summary>
public record UnpublishUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to unpublish.")]
    Guid Key,

    [property: Description("Optional culture code to unpublish (e.g., 'en-US', 'da-DK'). Omit to unpublish all cultures (or the whole item, for invariant content).")]
    string? Culture = null);

/// <summary>
/// Tool that unpublishes a content item, taking it offline.
/// </summary>
[AITool("unpublish_umbraco_content", "Unpublish Umbraco Content", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class UnpublishUmbracoContentTool(
    IContentPublishingService contentPublishingService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<UnpublishUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Unpublishes an Umbraco content item, taking it offline. Omit Culture to unpublish all cultures " +
        "(or the whole item, for invariant content), or specify a culture code to unpublish just that variant.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(UnpublishUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new UnpublishUmbracoContentResult(false, "Content key cannot be empty.");
        }

        var cultures = args.Culture is not null ? new[] { args.Culture } : null;
        var authResult = await authorizer.AuthorizeContentAsync(ActionUnpublish.ActionLetter, args.Key, cultures);
        if (!authResult.IsAuthorized)
        {
            return new UnpublishUmbracoContentResult(false, authResult.Message);
        }

        var cultureSet = args.Culture is not null ? new HashSet<string> { args.Culture } : null;
        var attempt = await contentPublishingService.UnpublishAsync(args.Key, cultureSet, authResult.UserKey!.Value);

        return attempt.Success
            ? new UnpublishUmbracoContentResult(true, null)
            : new UnpublishUmbracoContentResult(false, attempt.Result.ToMessage());
    }
}

/// <summary>
/// Result of the unpublish Umbraco content tool.
/// </summary>
public record UnpublishUmbracoContentResult(
    bool Success,
    string? Message);
