using Umbraco.AI.Web.Api.Management.Context.Controllers;
using Umbraco.AI.Web.Api.Management.ContextResourceTypes.Controllers;
using Umbraco.AI.Web.Api.Management.Profile.Controllers;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Web.Common.Authorization;

using static Umbraco.AI.Tests.Unit.Api.Management.AIAuthorizationTestHelper;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Load-bearing tests for the auth boundary (issue #509): read-only lookups behind shared pickers are
/// reused outside the AI section (Copilot Workspace, property editors, Automate action settings), so
/// they must NOT require the AI section. Full detail endpoints must stay behind it.
/// </summary>
public class SharedLookupAuthorizationTests
{
    public static TheoryData<Type> SharedLookupControllers => new()
    {
        typeof(AllContextController),
        typeof(AllContextResourceTypeController),
        typeof(ByIdContextResourceTypeController),
        typeof(AllProfileController),
    };

    public static TheoryData<Type> DetailControllers => new()
    {
        typeof(ByIdOrAliasContextController),
        typeof(ByIdOrAliasProfileController),
    };

    [Theory]
    [MemberData(nameof(SharedLookupControllers))]
    public void SharedLookup_DoesNotRequireAISectionAccess(Type controllerType)
    {
        // Arrange & Act
        var policies = GetPolicies(controllerType);

        // Assert
        policies.ShouldNotContain(AIAuthorizationPolicies.SectionAccessAI);
    }

    [Theory]
    [MemberData(nameof(SharedLookupControllers))]
    public void SharedLookup_StillRequiresBackOfficeAccess(Type controllerType)
    {
        // Arrange & Act
        var policies = GetPolicies(controllerType);

        // Assert
        policies.ShouldContain(AuthorizationPolicies.BackOfficeAccess);
    }

    [Theory]
    [MemberData(nameof(DetailControllers))]
    public void Detail_StillRequiresAISectionAccess(Type controllerType)
    {
        // Arrange & Act
        var policies = GetPolicies(controllerType);

        // Assert
        policies.ShouldContain(AIAuthorizationPolicies.SectionAccessAI);
    }
}
