using Microsoft.AspNetCore.Authorization;

using Umbraco.AI.Core.Security;
using Umbraco.Cms.Api.Management.Security.Authorization;

namespace Umbraco.AI.Web.Authorization;

internal sealed class AIPermissionAuthorizationHandler : MustSatisfyRequirementAuthorizationHandler<AIPermissionRequirement>
{
    private readonly IAIPermissionService _permissionService;

    public AIPermissionAuthorizationHandler(IAIPermissionService permissionService)
        => _permissionService = permissionService;

    protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, AIPermissionRequirement requirement)
        => Task.FromResult(_permissionService.HasPermission(requirement.Verb));
}
