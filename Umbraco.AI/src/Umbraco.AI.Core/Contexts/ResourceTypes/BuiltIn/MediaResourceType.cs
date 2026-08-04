using System.Text;
using System.Text.Json;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type that grounds the AI with the current values of a live (published) CMS media item (its
/// name, file URL, and property values), fetched at resolve time. Access is gated by the acting
/// backoffice user's read permission on the item, so the AI never surfaces media the user could not
/// otherwise read (decision #6). The item is serialized via <see cref="ContentToolHelpers.BuildContentItem"/>
/// — the same path the content tools use — so we don't duplicate serialization. Core does not extract
/// text from the binary itself; that is a separate concern.
/// </summary>
[AIContextResourceType("media", "Media",
    Description = "Grounds the AI with a media item's details (respecting the user's read permissions)",
    Icon = "icon-picture")]
public sealed class MediaResourceType : AIContextResourceTypeBase<MediaResourceSettings, MediaResourceData>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IMediaPermissionAuthorizer _mediaPermissionAuthorizer;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaResourceType"/> class.
    /// </summary>
    public MediaResourceType(
        IAIContextResourceTypeInfrastructure infrastructure,
        IUmbracoContextAccessor umbracoContextAccessor,
        IMediaPermissionAuthorizer mediaPermissionAuthorizer,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        : base(infrastructure)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _mediaPermissionAuthorizer = mediaPermissionAuthorizer;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override async Task<MediaResourceData?> ResolveDataAsync(
        MediaResourceSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Media?.FirstOrDefault()?.MediaKey is not { } mediaKey)
        {
            return null;
        }

        // Require an authenticated acting user — never resolve live media without one, and never bypass
        // the user's own read permissions (indirect-exfiltration guard, decision #6 / F-SEC).
        var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return null;
        }

        if (await _mediaPermissionAuthorizer.IsDeniedAsync(user, mediaKey))
        {
            return null;
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return null;
        }

        var media = umbracoContext.Media?.GetById(mediaKey);
        if (media is null)
        {
            return null;
        }

        var item = ContentToolHelpers.BuildContentItem(media);

        return new MediaResourceData
        {
            Name = media.Name,
            Json = JsonSerializer.Serialize(item, Constants.DefaultJsonSerializerOptions),
        };
    }

    /// <inheritdoc />
    protected override string FormatDataForLlm(MediaResourceData data)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            sb.Append("# ").AppendLine(data.Name);
        }

        if (!string.IsNullOrWhiteSpace(data.Json))
        {
            sb.AppendLine(data.Json);
        }

        return sb.ToString().Trim();
    }
}
