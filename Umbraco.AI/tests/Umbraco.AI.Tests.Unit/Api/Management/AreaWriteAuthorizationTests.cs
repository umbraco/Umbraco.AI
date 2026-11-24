// GP-2 (AC1-AC7)

using System.Reflection;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Api.Management.AuditLog.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Context.Controllers;
using Umbraco.AI.Web.Api.Management.Guardrail.Controllers;
using Umbraco.AI.Web.Api.Management.Profile.Controllers;
using Umbraco.AI.Web.Api.Management.Settings.Controllers;
using Umbraco.AI.Web.Api.Management.Test.Controllers;
using Umbraco.AI.Web.Api.Management.TestRun.Controllers;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Every AI write endpoint is guarded by its area's policy; reads stay as they are (#509). These tests read
/// the policy off the real controller and evaluate it with the real handlers, because ASP.NET Core runs that
/// check before the action, so a denied request never reaches the action's code. The live HTTP check is T7.
/// </summary>
public class AreaWriteAuthorizationTests
{
    // Each write endpoint with the policy it must carry. T5 adds its controllers here.
    public static TheoryData<Type, string> WriteControllers => new()
    {
        { typeof(CreateConnectionController), AIAuthorizationPolicies.ManageConnectionsAI },
        { typeof(UpdateConnectionController), AIAuthorizationPolicies.ManageConnectionsAI },
        { typeof(DeleteConnectionController), AIAuthorizationPolicies.ManageConnectionsAI },
        { typeof(TestConnectionController), AIAuthorizationPolicies.ManageConnectionsAI },
        { typeof(CreateProfileController), AIAuthorizationPolicies.ManageProfilesAI },
        { typeof(UpdateProfileController), AIAuthorizationPolicies.ManageProfilesAI },
        { typeof(DeleteProfileController), AIAuthorizationPolicies.ManageProfilesAI },
        { typeof(CreateContextController), AIAuthorizationPolicies.ManageContextsAI },
        { typeof(UpdateContextController), AIAuthorizationPolicies.ManageContextsAI },
        { typeof(DeleteContextController), AIAuthorizationPolicies.ManageContextsAI },
        { typeof(CreateGuardrailController), AIAuthorizationPolicies.ManageGuardrailsAI },
        { typeof(UpdateGuardrailController), AIAuthorizationPolicies.ManageGuardrailsAI },
        { typeof(DeleteGuardrailController), AIAuthorizationPolicies.ManageGuardrailsAI },
        { typeof(UpdateSettingsController), AIAuthorizationPolicies.ManageSettingsAI },
        { typeof(CreateTestController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(UpdateTestController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(DeleteTestController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(RunTestController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(RunTestBatchController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(RunTestsByTagsController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(DeleteTestRunController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(SetBaselineController), AIAuthorizationPolicies.ManageTestsAI },
        { typeof(DeleteAuditLogController), AIAuthorizationPolicies.ManageSettingsAI },
        { typeof(CleanupAuditLogsController), AIAuthorizationPolicies.ManageSettingsAI },
    };

    internal static readonly string[] AllVerbs =
    [
        AIPermissions.ManageConnections,
        AIPermissions.ManageProfiles,
        AIPermissions.ManageContexts,
        AIPermissions.ManageKnowledgeSets,
        AIPermissions.ManageGuardrails,
        AIPermissions.ManageSettings,
        AIPermissions.ViewAuditLogs,
        AIPermissions.ViewAnalytics,
        AIPermissions.ManageTests,
    ];

    #region Happy path

    [Fact]
    public async Task ConnectionCreate_WithManageConnections_Returns201()
    {
        // Given an AI section user with Uai.Connection.Manage
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ManageConnections]);

        // When  they POST connections (the create controller's AI policies are evaluated)
        var allowed = await IsAllowedAsync(authorizationService, typeof(CreateConnectionController));

        // Then  the request passes authorization, so the action runs and returns 201 (AC1)
        allowed.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(WriteControllers))]
    public void EachWriteEndpoint_RequiresItsAreaPolicy(Type controllerType, string policy)
    {
        // Given each write endpoint listed in SPEC.md
        // When  its authorization policies are read
        var policies = GetPolicies(controllerType);

        // Then  they contain that area's policy (AC2)
        policies.ShouldContain(policy);
    }

    // Controllers allowed to write while carrying only the section policy. Empty on purpose: add a name
    // here only with a reason.
    private static readonly string[] WriteControllersWithSectionPolicyOnly =
    [
        // POST only because they take a body; they compute a comparison and change nothing.
        nameof(CompareRunsController),
        nameof(CompareVariationsController),
    ];

    private static readonly string[] AreaNamespaces = ["Connection", "Profile", "Context", "Guardrail", "Settings", "Test", "TestRun", "AuditLog"];

    [Fact]
    public void EveryWriteController_InAnArea_CarriesAnAreaPolicy()
    {
        // Given every controller in the Connection, Profile, Context, Guardrail, Settings, Test, TestRun and AuditLog areas
        var writeControllers = typeof(AIAuthorizationPolicies).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t => AreaNamespaces.Any(a => t.Namespace == $"Umbraco.AI.Web.Api.Management.{a}.Controllers"))
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(m => m.GetCustomAttributes().Any(IsWriteVerb)))
            .ToList();

        // When  the ones that write are checked for a policy other than plain section access
        var sectionOnly = writeControllers
            .Where(t => !GetPolicies(t).Any(p => IsAIPolicy(p) && p != AIAuthorizationPolicies.SectionAccessAI))
            .Select(t => t.Name)
            .Except(WriteControllersWithSectionPolicyOnly)
            .ToList();

        // Then  none is left on the section policy alone, so a new write endpoint cannot skip its area right
        writeControllers.ShouldNotBeEmpty();
        sectionOnly.ShouldBeEmpty();

        static bool IsWriteVerb(Attribute a)
            => a is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute;
    }

    [Fact]
    public void ProfileList_WithNoAIRights_Returns200()
    {
        // Given an AI section user with no AI rights
        // When  they GET profiles (the list controller's policies are read)
        var policies = GetPolicies(typeof(AllProfileController));

        // Then  no AI policy is required, so the response is 200 (AC3)
        policies.Where(IsAIPolicy).ShouldBeEmpty();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task ConnectionCreate_WithoutManageConnections_Returns403()
    {
        // Given an AI section user with every AI right except Uai.Connection.Manage
        var authorizationService = CreateAuthorizationService(
            hasSection: true,
            verbs: AllVerbs.Where(v => v != AIPermissions.ManageConnections).ToArray());

        // When  they POST connections
        var allowed = await IsAllowedAsync(authorizationService, typeof(CreateConnectionController));

        // Then  authorization fails, so the response is 403 (AC4)
        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task ConnectionDelete_WithoutManageConnections_LeavesConnection()
    {
        // Given an AI section user without Uai.Connection.Manage and an existing connection
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: []);

        // When  they DELETE connections/{alias}
        var allowed = await IsAllowedAsync(authorizationService, typeof(DeleteConnectionController));

        // Then  authorization fails before the action runs, so the connection still exists (AC5)
        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task ConnectionCreate_WithRightButNoSection_Returns403()
    {
        // Given a user with Uai.Connection.Manage but not the AI section
        var authorizationService = CreateAuthorizationService(hasSection: false, verbs: [AIPermissions.ManageConnections]);

        // When  they POST connections
        var allowed = await IsAllowedAsync(authorizationService, typeof(CreateConnectionController));

        // Then  the response is 403 (AC6)
        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task AuditLogDelete_WithViewLogsOnly_Returns403()
    {
        // Given an AI section user with Uai.AuditLog.View but not Uai.Settings.Manage
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ViewAuditLogs]);

        // When  they call the audit log delete endpoint
        var allowed = await IsAllowedAsync(authorizationService, typeof(DeleteAuditLogController));

        // Then  authorization fails, so the response is 403 (AC7, spec guess 1)
        allowed.ShouldBeFalse();
    }

    #endregion

    // Every policy name declared on AIAuthorizationPolicies.
    private static readonly HashSet<string> AIPolicyNames = typeof(AIAuthorizationPolicies)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    internal static bool IsAIPolicy(string? policy)
        => policy is not null && AIPolicyNames.Contains(policy);

    // Every [Authorize(Policy)] on the controller and its bases must pass, as in ASP.NET Core.
    internal static List<string?> GetPolicies(Type controllerType)
        => controllerType
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(a => a.Policy)
            .ToList();

    internal static async Task<bool> IsAllowedAsync(IAuthorizationService authorizationService, Type controllerType)
    {
        // The CMS base policy needs a real backoffice login, so only the AI policies are evaluated here.
        foreach (var policy in GetPolicies(controllerType).Where(IsAIPolicy))
        {
            var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), policy!);
            if (!result.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    internal static IAuthorizationService CreateAuthorizationService(bool hasSection, string[] verbs)
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
