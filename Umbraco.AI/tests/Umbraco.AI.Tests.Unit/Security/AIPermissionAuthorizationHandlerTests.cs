// GP-2 (AC4, AC6)

using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Shouldly;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

using Xunit;

namespace Umbraco.AI.Tests.Unit.Security;

/// <summary>
/// Each right's policy needs both the AI section and the right's verb.
/// </summary>
public class AIPermissionAuthorizationHandlerTests
{
    #region Happy path

    [Fact]
    public async Task Policy_WithSectionAndRight_Succeeds()
    {
        // Given a user with the AI section and Uai.Connection.Manage
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ManageConnections]);

        // When  the Manage connections policy is evaluated
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), AIAuthorizationPolicies.ManageConnectionsAI);

        // Then  it succeeds
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(AIAuthorizationPolicies.ManageConnectionsAI, AIPermissions.ManageConnections)]
    [InlineData(AIAuthorizationPolicies.ManageProfilesAI, AIPermissions.ManageProfiles)]
    [InlineData(AIAuthorizationPolicies.ManageContextsAI, AIPermissions.ManageContexts)]
    [InlineData(AIAuthorizationPolicies.ManageKnowledgeSetsAI, AIPermissions.ManageKnowledgeSets)]
    [InlineData(AIAuthorizationPolicies.ManageGuardrailsAI, AIPermissions.ManageGuardrails)]
    [InlineData(AIAuthorizationPolicies.ManageSettingsAI, AIPermissions.ManageSettings)]
    [InlineData(AIAuthorizationPolicies.ViewAuditLogsAI, AIPermissions.ViewAuditLogs)]
    [InlineData(AIAuthorizationPolicies.ViewAnalyticsAI, AIPermissions.ViewAnalytics)]
    [InlineData(AIAuthorizationPolicies.ManageTestsAI, AIPermissions.ManageTests)]
    [InlineData(AIAuthorizationPolicies.ManageAgentsAI, AIPermissions.ManageAgents)]
    [InlineData(AIAuthorizationPolicies.ManagePromptsAI, AIPermissions.ManagePrompts)]
    public async Task Policy_WithSectionAndItsOwnVerb_Succeeds(string policyName, string verb)
    {
        // Given a user with the AI section and the policy's verb
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [verb]);

        // When  the policy is evaluated
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), policyName);

        // Then  it succeeds
        result.Succeeded.ShouldBeTrue();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Policy_WithSectionButNotRight_Fails()
    {
        // Given a user with the AI section but not Uai.Connection.Manage
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ManageProfiles]);

        // When  the Manage connections policy is evaluated
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), AIAuthorizationPolicies.ManageConnectionsAI);

        // Then  it fails (GP-2 AC4)
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Policy_WithRightButNotSection_Fails()
    {
        // Given a user with Uai.Connection.Manage but not the AI section
        var authorizationService = CreateAuthorizationService(hasSection: false, verbs: [AIPermissions.ManageConnections]);

        // When  the Manage connections policy is evaluated
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), AIAuthorizationPolicies.ManageConnectionsAI);

        // Then  it fails (GP-2 AC6)
        result.Succeeded.ShouldBeFalse();
    }

    [Theory]
    [InlineData(AIAuthorizationPolicies.ManageConnectionsAI, AIPermissions.ManageConnections)]
    [InlineData(AIAuthorizationPolicies.ManageProfilesAI, AIPermissions.ManageProfiles)]
    [InlineData(AIAuthorizationPolicies.ManageContextsAI, AIPermissions.ManageContexts)]
    [InlineData(AIAuthorizationPolicies.ManageKnowledgeSetsAI, AIPermissions.ManageKnowledgeSets)]
    [InlineData(AIAuthorizationPolicies.ManageGuardrailsAI, AIPermissions.ManageGuardrails)]
    [InlineData(AIAuthorizationPolicies.ManageSettingsAI, AIPermissions.ManageSettings)]
    [InlineData(AIAuthorizationPolicies.ViewAuditLogsAI, AIPermissions.ViewAuditLogs)]
    [InlineData(AIAuthorizationPolicies.ViewAnalyticsAI, AIPermissions.ViewAnalytics)]
    [InlineData(AIAuthorizationPolicies.ManageTestsAI, AIPermissions.ManageTests)]
    [InlineData(AIAuthorizationPolicies.ManageAgentsAI, AIPermissions.ManageAgents)]
    [InlineData(AIAuthorizationPolicies.ManagePromptsAI, AIPermissions.ManagePrompts)]
    public async Task Policy_WithSectionAndOnlyADifferentVerb_Fails(string policyName, string verb)
    {
        // Given a user with the AI section and a verb other than the policy's
        var otherVerb = verb == AIPermissions.ManageConnections ? AIPermissions.ManageProfiles : AIPermissions.ManageConnections;
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [otherVerb]);

        // When  the policy is evaluated
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), policyName);

        // Then  it fails
        result.Succeeded.ShouldBeFalse();
    }

    #endregion

    private static IAuthorizationService CreateAuthorizationService(bool hasSection, string[] verbs)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.AllowedSections).Returns(hasSection ? [Core.Constants.Sections.AI] : []);

        IUser? outUser = user.Object;
        var helper = new Mock<IAuthorizationHelper>();
        helper.Setup(h => h.TryGetUmbracoUser(It.IsAny<ClaimsPrincipal>(), out outUser)).Returns(true);

        var permissions = new Mock<IAIPermissionService>();
        permissions.Setup(p => p.HasPermission(It.IsAny<string>())).Returns<string>(verbs.Contains);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(helper.Object);
        services.AddSingleton(permissions.Object);
        services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AIPermissionAuthorizationHandler>();
        services.AddAuthorization(AIAuthorizationPolicies.Register);

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }
}
