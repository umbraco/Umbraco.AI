using System.ComponentModel;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the PublishUmbracoContent tool.
/// </summary>
public record PublishUmbracoContentArgs(
    [property: Description("The unique key (GUID) of the content item to publish.")]
    Guid Key,

    [property: Description("Optional culture code to publish (e.g., 'en-US', 'da-DK'). Omit for invariant content.")]
    string? Culture = null);

/// <summary>
/// Tool that publishes a content item, making it live immediately.
/// </summary>
[AITool("publish_umbraco_content", "Publish Umbraco Content", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class PublishUmbracoContentTool(
    IContentPublishingService contentPublishingService,
    IContentService contentService,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<PublishUmbracoContentArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Publishes a draft Umbraco content item, making it live immediately. " +
        "Omit Culture for invariant content, or specify a culture code to publish that variant.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(PublishUmbracoContentArgs args, CancellationToken cancellationToken = default)
    {
        if (args.Key == Guid.Empty)
        {
            return new PublishUmbracoContentResult(false, "Content key cannot be empty.");
        }

        var cultures = args.Culture is not null ? new[] { args.Culture } : null;
        var authResult = await authorizer.AuthorizeContentAsync(ActionPublish.ActionLetter, args.Key, cultures);
        if (!authResult.IsAuthorized)
        {
            return new PublishUmbracoContentResult(false, authResult.Message);
        }

        var schedule = new CulturePublishScheduleModel { Culture = args.Culture, Schedule = null };
        var attempt = await contentPublishingService.PublishAsync(args.Key, [schedule], authResult.UserKey!.Value);

        return attempt.Success
            ? new PublishUmbracoContentResult(true, null)
            : new PublishUmbracoContentResult(false, attempt.Status.ToMessage());
    }

    /// <inheritdoc />
    protected override string? DescribeInvocation(PublishUmbracoContentArgs args)
        => args.Culture is null
            ? "Publish this content item, making it live."
            : $"Publish the '{args.Culture}' culture of this content item, making it live.";

    /// <inheritdoc />
    protected override string? ConfirmationPhrase(PublishUmbracoContentArgs args)
        => contentService.GetById(args.Key)?.Name;
}

/// <summary>
/// Result of the publish Umbraco content tool.
/// </summary>
public record PublishUmbracoContentResult(
    bool Success,
    string? Message);
