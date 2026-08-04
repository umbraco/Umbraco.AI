using System.Text;
using System.Text.Json;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type that grounds the AI with the current values of a live (published) CMS content node,
/// fetched at resolve time. Access is gated by the acting backoffice user's read permission on the
/// node, so the AI never surfaces content the user could not otherwise read (decision #6). The node is
/// serialized via <see cref="ContentToolHelpers.BuildContentItem"/> — the same path the
/// <c>get_content</c> tool uses — so property values are formatted consistently and we don't duplicate
/// serialization.
/// </summary>
[AIContextResourceType("content", "Content",
    Description = "Grounds the AI with the current values of a content node (respecting the user's read permissions)",
    Icon = "icon-documents")]
public sealed class ContentResourceType : AIContextResourceTypeBase<ContentResourceSettings, ContentResourceData>
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IContentPermissionAuthorizer _contentPermissionAuthorizer;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentResourceType"/> class.
    /// </summary>
    public ContentResourceType(
        IAIContextResourceTypeInfrastructure infrastructure,
        IUmbracoContextAccessor umbracoContextAccessor,
        IContentPermissionAuthorizer contentPermissionAuthorizer,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        : base(infrastructure)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _contentPermissionAuthorizer = contentPermissionAuthorizer;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override async Task<ContentResourceData?> ResolveDataAsync(
        ContentResourceSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.ContentId is not { } contentKey)
        {
            return null;
        }

        // Require an authenticated acting user — never resolve live content without one, and never
        // bypass the user's own read permissions (indirect-exfiltration guard, decision #6 / F-SEC).
        var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return null;
        }

        if (await _contentPermissionAuthorizer.IsDeniedAsync(user, contentKey, ActionBrowse.ActionLetter))
        {
            return null;
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            return null;
        }

        var content = umbracoContext.Content?.GetById(contentKey);
        if (content is null)
        {
            return null;
        }

        var item = ContentToolHelpers.BuildContentItem(content);

        return new ContentResourceData
        {
            Name = content.Name,
            Json = JsonSerializer.Serialize(item, Constants.DefaultJsonSerializerOptions),
        };
    }

    /// <inheritdoc />
    protected override string FormatDataForLlm(ContentResourceData data)
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
