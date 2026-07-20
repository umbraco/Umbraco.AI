using System.Text;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type that grounds the AI with the current values of a live CMS content node, fetched at
/// resolve time. Access is gated by the acting backoffice user's read permission on the node, so the
/// AI never surfaces content the user could not otherwise read (decision #6).
/// </summary>
[AIContextResourceType("cms-content", "CMS Content",
    Description = "Grounds the AI with the current values of a content node (respecting the user's read permissions)",
    Icon = "icon-document")]
public sealed class CmsContentResourceType : AIContextResourceTypeBase<CmsContentResourceSettings, CmsContentResourceData>
{
    private readonly IContentService _contentService;
    private readonly IContentPermissionAuthorizer _contentPermissionAuthorizer;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CmsContentResourceType"/> class.
    /// </summary>
    public CmsContentResourceType(
        IAIContextResourceTypeInfrastructure infrastructure,
        IContentService contentService,
        IContentPermissionAuthorizer contentPermissionAuthorizer,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        : base(infrastructure)
    {
        _contentService = contentService;
        _contentPermissionAuthorizer = contentPermissionAuthorizer;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public override async Task<CmsContentResourceData?> ResolveDataAsync(
        CmsContentResourceSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ContentId) || !Guid.TryParse(settings.ContentId, out var contentKey))
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

        var content = _contentService.GetById(contentKey);
        if (content is null)
        {
            return null;
        }

        var body = new StringBuilder();
        foreach (var property in content.Properties)
        {
            var value = property.GetValue()?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                body.Append(property.Alias).Append(": ").AppendLine(value);
            }
        }

        return new CmsContentResourceData
        {
            Name = content.Name,
            Content = body.ToString().Trim(),
        };
    }

    /// <inheritdoc />
    protected override string FormatDataForLlm(CmsContentResourceData data)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            sb.Append("# ").AppendLine(data.Name);
        }

        if (!string.IsNullOrWhiteSpace(data.Content))
        {
            sb.AppendLine(data.Content);
        }

        return sb.ToString().Trim();
    }
}
