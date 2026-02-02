using System.Text;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Contributes current backoffice user information to the runtime context.
/// This is an ambient contributor that adds user context to the system message
/// regardless of request context items.
/// </summary>
internal sealed class UserContextContributor : IAIRuntimeContextContributor
{
    private readonly IBackOfficeSecurityAccessor _securityAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserContextContributor"/> class.
    /// </summary>
    /// <param name="securityAccessor">The backoffice security accessor.</param>
    public UserContextContributor(IBackOfficeSecurityAccessor securityAccessor)
    {
        _securityAccessor = securityAccessor;
    }

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        var user = _securityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return;
        }

        context.SystemMessageParts.Add(FormatUserContext(user));
    }

    private static string FormatUserContext(Umbraco.Cms.Core.Models.Membership.IUser user)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Current User");
        sb.AppendLine($"- Key: {user.Key}");
        sb.AppendLine($"- Name: {user.Name}");

        // Only include username if it doesn't look like an email (privacy protection)
        if (!IsEmailLike(user.Username))
        {
            sb.AppendLine($"- Username: {user.Username}");
        }

        if (!string.IsNullOrEmpty(user.Language))
        {
            sb.AppendLine($"- Language: {user.Language}");
        }

        var groups = user.Groups
            .Select(g => g.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        if (groups.Count > 0)
        {
            sb.AppendLine($"- Groups: {string.Join(", ", groups)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsEmailLike(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return username.Contains('@');
    }
}
