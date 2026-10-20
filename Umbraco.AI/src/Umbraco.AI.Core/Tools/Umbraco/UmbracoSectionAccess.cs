using Umbraco.Cms.Core.Models.Membership;
using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// The section a backoffice user needs before AI may read or write content or media on their behalf,
/// mirroring the section policies on the CMS's own Management API controllers.
/// </summary>
/// <remarks>
/// The CMS checks sections at the controller, before the per-item start-node and permission check
/// (<c>IContentPermissionAuthorizer</c> / <c>IMediaPermissionAuthorizer</c>), which doesn't look at
/// sections at all. AI tools reach the editing services without going through those controllers, so
/// they must apply both steps themselves. Start nodes and permissions are always checked on top.
/// </remarks>
internal static class UmbracoSectionAccess
{
    /// <summary>
    /// The sections any one of which opens the content and media trees (names, keys and structure),
    /// as the CMS's <c>SectionAccessForContentTree</c> / <c>SectionAccessForMediaTree</c> policies do.
    /// </summary>
    private static readonly string[] TreeSections =
    [
        CmsConstants.Applications.Content,
        CmsConstants.Applications.Media,
        CmsConstants.Applications.Users,
        CmsConstants.Applications.Settings,
        CmsConstants.Applications.Packages,
        CmsConstants.Applications.Members,
        CmsConstants.Applications.Library,
    ];

    /// <summary>
    /// Writing content, or reading a document's full property values: the Content section, like the
    /// CMS's document controllers.
    /// </summary>
    public static bool CanAccessFullContent(IUser user)
        => HasAnySection(user, CmsConstants.Applications.Content);

    /// <summary>
    /// Writing media: the Media section, like the CMS's media controllers.
    /// </summary>
    public static bool CanWriteMedia(IUser user)
        => HasAnySection(user, CmsConstants.Applications.Media);

    /// <summary>
    /// Listing content or media (names, keys, structure) and reading media items: any tree section.
    /// </summary>
    /// <remarks>
    /// Media reads follow the tree rule rather than the Media section because editors routinely work
    /// with images from the Content section (e.g. writing alt text), as the CMS's pickers allow.
    /// </remarks>
    public static bool CanBrowseTree(IUser user)
        => HasAnySection(user, TreeSections);

    private static bool HasAnySection(IUser user, params string[] sections)
        => user.AllowedSections.Any(allowed => sections.Contains(allowed, StringComparer.OrdinalIgnoreCase));
}
