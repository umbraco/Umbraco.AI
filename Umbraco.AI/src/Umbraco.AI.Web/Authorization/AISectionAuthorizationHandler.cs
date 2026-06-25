using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Management.Security.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Web.Authorization;

internal sealed class AISectionAuthorizationHandler : MustSatisfyRequirementAuthorizationHandler<AISectionRequirement>
{
    private readonly IAuthorizationHelper _authorizationHelper;

    public AISectionAuthorizationHandler(IAuthorizationHelper authorizationHelper)
        => _authorizationHelper = authorizationHelper;

    protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, AISectionRequirement requirement)
    {
        var allowed = _authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
                      && user.AllowedSections.Contains(Core.Constants.Sections.AI);
        return Task.FromResult(allowed);
    }
}
