using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Result of an authorization check performed by <see cref="IUmbracoWriteAuthorizer"/>.
/// </summary>
public sealed record UmbracoWriteAuthorizationResult(bool IsAuthorized, Guid? UserKey, string? Message)
{
    public static UmbracoWriteAuthorizationResult Denied(string message) => new(false, null, message);

    public static UmbracoWriteAuthorizationResult Allowed(Guid userKey) => new(true, userKey, null);
}

/// <summary>
/// Authorizes a backend AI tool's write operation against the acting backoffice user's real CMS
/// permissions, mirroring the check the CMS's own Management API controllers perform.
/// </summary>
/// <remarks>
/// <c>IContentEditingService</c>, <c>IContentPublishingService</c>, <c>IMediaEditingService</c>, and
/// the property-value dispatcher behind the frontend's block-editing tools do NOT self-authorize —
/// they trust the caller. The CMS's own controllers check permissions themselves, via
/// <see cref="IContentPermissionAuthorizer"/>/<see cref="IMediaPermissionAuthorizer"/>, before ever
/// calling those services. Every backend write tool in this folder must call this authorizer first,
/// or it silently bypasses per-user start-node/permission restrictions.
/// </remarks>
public interface IUmbracoWriteAuthorizer
{
    /// <summary>
    /// Authorizes a content write action. Pass <paramref name="contentKey"/> as <c>null</c> to check
    /// root-level access (e.g. creating an item with no parent).
    /// </summary>
    Task<UmbracoWriteAuthorizationResult> AuthorizeContentAsync(
        string actionLetter, Guid? contentKey, IEnumerable<string>? cultures = null);

    /// <summary>
    /// Authorizes a media write action. Pass <paramref name="mediaKey"/> as <c>null</c> to check
    /// root-level access. Media has no action-letter dimension — Umbraco's media permissions only
    /// distinguish root/recycle-bin/specific-key access, not the kind of operation being performed.
    /// </summary>
    Task<UmbracoWriteAuthorizationResult> AuthorizeMediaAsync(Guid? mediaKey);
}

/// <inheritdoc cref="IUmbracoWriteAuthorizer"/>
public sealed class UmbracoWriteAuthorizer(
    IContentPermissionAuthorizer contentPermissionAuthorizer,
    IMediaPermissionAuthorizer mediaPermissionAuthorizer,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor) : IUmbracoWriteAuthorizer
{
    public async Task<UmbracoWriteAuthorizationResult> AuthorizeContentAsync(
        string actionLetter, Guid? contentKey, IEnumerable<string>? cultures = null)
    {
        var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return UmbracoWriteAuthorizationResult.Denied("No authenticated backoffice user context is available.");
        }

        var permissions = new HashSet<string> { actionLetter };
        var denied = contentKey is null
            ? await contentPermissionAuthorizer.IsDeniedAtRootLevelAsync(user, permissions)
            : await contentPermissionAuthorizer.IsDeniedAsync(user, contentKey.Value, actionLetter);
        if (denied)
        {
            return UmbracoWriteAuthorizationResult.Denied("You do not have permission to perform this action on this content item.");
        }

        var cultureSet = cultures as ISet<string> ?? cultures?.ToHashSet();
        if (cultureSet is { Count: > 0 })
        {
            var culturesDenied = await contentPermissionAuthorizer.IsDeniedForCultures(user, cultureSet);
            if (culturesDenied)
            {
                return UmbracoWriteAuthorizationResult.Denied("You do not have access to one or more of the requested cultures.");
            }
        }

        return UmbracoWriteAuthorizationResult.Allowed(user.Key);
    }

    public async Task<UmbracoWriteAuthorizationResult> AuthorizeMediaAsync(Guid? mediaKey)
    {
        var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return UmbracoWriteAuthorizationResult.Denied("No authenticated backoffice user context is available.");
        }

        var denied = mediaKey is null
            ? await mediaPermissionAuthorizer.IsDeniedAtRootLevelAsync(user)
            : await mediaPermissionAuthorizer.IsDeniedAsync(user, mediaKey.Value);

        return denied
            ? UmbracoWriteAuthorizationResult.Denied("You do not have permission to perform this action on this media item.")
            : UmbracoWriteAuthorizationResult.Allowed(user.Key);
    }
}
