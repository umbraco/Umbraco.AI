using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Authorizes a backend AI tool's read of content or media against the acting backoffice user's real
/// CMS access, the read-side counterpart of <see cref="IUmbracoWriteAuthorizer"/>.
/// </summary>
/// <remarks>
/// The CMS's content and media caches and editing services don't filter by user, so every backend tool
/// that returns content or media must ask this authorizer first. It applies the same two steps as the
/// CMS's own Management API: the section policy for the kind of access, then the per-item start-node and
/// permission check. With no acting backoffice user, every check fails.
/// </remarks>
public interface IUmbracoReadAuthorizer
{
    /// <summary>
    /// Whether the acting user may read a document's full property values: the Content section, and
    /// Browse permission on the item (which includes it being under one of their start nodes).
    /// </summary>
    Task<bool> CanReadContentAsync(Guid contentKey);

    /// <summary>
    /// Whether the acting user's content tree shows the item (its key, name and place in the tree): one
    /// of the tree sections, and either Browse permission on the item or the item being an ancestor of
    /// one of their start nodes (which the tree shows so they can navigate down to it).
    /// </summary>
    /// <param name="contentKey">The item's key.</param>
    /// <param name="path">The item's comma-separated id path, as on <c>IPublishedContent.Path</c>.</param>
    Task<bool> CanSeeContentInTreeAsync(Guid contentKey, string path);

    /// <summary>
    /// Whether the acting user may read a media item, including its file: one of the tree sections, and
    /// access to the item (which includes it being under one of their media start nodes).
    /// </summary>
    Task<bool> CanReadMediaAsync(Guid mediaKey);
}

/// <inheritdoc cref="IUmbracoReadAuthorizer"/>
public sealed class UmbracoReadAuthorizer(
    IContentPermissionAuthorizer contentPermissionAuthorizer,
    IMediaPermissionAuthorizer mediaPermissionAuthorizer,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
    IEntityService entityService,
    AppCaches appCaches) : IUmbracoReadAuthorizer
{
    /// <inheritdoc />
    public async Task<bool> CanReadContentAsync(Guid contentKey)
    {
        IUser? user = CurrentUser;
        return user is not null
            && UmbracoSectionAccess.CanAccessFullContent(user)
            && !await contentPermissionAuthorizer.IsDeniedAsync(user, contentKey, ActionBrowse.ActionLetter);
    }

    /// <inheritdoc />
    public async Task<bool> CanSeeContentInTreeAsync(Guid contentKey, string path)
    {
        IUser? user = CurrentUser;
        if (user is null || !UmbracoSectionAccess.CanBrowseTree(user))
        {
            return false;
        }

        var inBranch = ContentPermissions.IsInBranchOfStartNode(
            path,
            user.CalculateContentStartNodeIds(entityService, appCaches),
            user.GetContentStartNodePaths(entityService, appCaches),
            out var hasPathAccess);

        if (!inBranch)
        {
            return false;
        }

        // An ancestor of a start node is shown (as the CMS tree does) without needing Browse on it.
        return !hasPathAccess
            || !await contentPermissionAuthorizer.IsDeniedAsync(user, contentKey, ActionBrowse.ActionLetter);
    }

    /// <inheritdoc />
    public async Task<bool> CanReadMediaAsync(Guid mediaKey)
    {
        IUser? user = CurrentUser;
        return user is not null
            && UmbracoSectionAccess.CanBrowseTree(user)
            && !await mediaPermissionAuthorizer.IsDeniedAsync(user, mediaKey);
    }

    private IUser? CurrentUser => backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
}
