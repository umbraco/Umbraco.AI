// GP-2 (AC1-AC7)

using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Api.Management.AuditLog.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Profile.Controllers;
using Umbraco.AI.Web.Authorization;

using static Umbraco.AI.Tests.Unit.Api.Management.AIAuthorizationTestHelper;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Every AI write endpoint is guarded by its area's policy; reads stay as they are (#509). These tests read
/// the policy off the real controller and evaluate it with the real handlers, because ASP.NET Core runs that
/// check before the action, so a denied request never reaches the action's code. The live HTTP check is T7.
/// </summary>
public class AreaWriteAuthorizationTests
{
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

    private const string ManagementNamespace = "Umbraco.AI.Web.Api.Management.";

    // Controllers with a POST/PUT/PATCH/DELETE action that may carry only the section policy, each with why.
    private static readonly Dictionary<string, string> WriteControllersWithSectionPolicyOnly = new()
    {
        ["CompareRunsController"] = "POST only because it takes a body; it computes a comparison and changes nothing.",
        ["CompareVariationsController"] = "POST only because it takes a body; it computes a comparison and changes nothing.",
        ["CompleteChatController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["GenerateEmbeddingController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["TranscribeSpeechToTextController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["GenerateImageController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["ExtractController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["AskDecisionController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["InvokePropertyValueOperationController"] = "Runs an AI call for the caller (a POST because it takes a body); SPEC leaves it open to every backoffice user.",
        ["EntityVersionHistoryController"] = "Rollback is the only write; its right depends on the entity type, so the action checks it itself.",
    };

    // Areas whose write endpoints need another area's right. An area not listed must use its own.
    private static readonly Dictionary<string, string> AreaVerbOverrides = new()
    {
        ["AuditLog"] = "Settings",
        ["TestRun"] = "Test",
    };

    private static readonly Type[] WriteControllers = typeof(AIAuthorizationPolicies).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false }
                    && typeof(ControllerBase).IsAssignableFrom(t)
                    && t.Namespace?.StartsWith(ManagementNamespace, StringComparison.Ordinal) == true)
        .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute)))
        .ToArray();

    // The verb of the one right a policy asks for, read from the registered policy rather than a second table.
    private static string? VerbOf(string? policyName)
    {
        if (!IsAIPolicy(policyName))
        {
            return null;
        }

        var options = new AuthorizationOptions();
        AIAuthorizationPolicies.Register(options);
        return options.GetPolicy(policyName!)!.Requirements.OfType<AIPermissionRequirement>().SingleOrDefault()?.Verb;
    }

    private static string AreaOf(Type controllerType)
        => controllerType.Namespace![ManagementNamespace.Length..].Split('.')[0];

    [Fact]
    public void EachWriteEndpoint_RequiresItsAreaPolicy()
    {
        // Given each controller with a write action
        // When  its authorization policies are read and the right they ask for is looked up on the registered policy
        var wrongArea = WriteControllers
            .Where(t => !WriteControllersWithSectionPolicyOnly.ContainsKey(t.Name))
            .Select(t => (Controller: t.Name, Area: AreaOf(t), Verbs: GetPolicies(t).Select(VerbOf).OfType<string>().ToList()))
            .Where(c => !c.Verbs.Any(v => v.Split('.')[1] == AreaVerbOverrides.GetValueOrDefault(c.Area, c.Area)))
            .Select(c => $"{c.Controller} ({c.Area}) has {string.Join(", ", c.Verbs.DefaultIfEmpty("no AI right"))}")
            .ToList();

        // Then  it carries a right from its own area (or the area the override map names) (AC2)
        WriteControllers.ShouldNotBeEmpty();
        wrongArea.ShouldBeEmpty();
    }

    [Fact]
    public void EveryWriteController_CarriesAnAreaPolicy()
    {
        // Given every controller under the Management API with a write action
        // When  the ones not listed as exceptions are checked for a policy other than plain section access
        var sectionOnly = WriteControllers
            .Where(t => !GetPolicies(t).Any(p => IsAIPolicy(p) && p != AIAuthorizationPolicies.SectionAccessAI))
            .Select(t => t.Name)
            .Except(WriteControllersWithSectionPolicyOnly.Keys)
            .ToList();

        // Then  none is left on the section policy alone, so a new write endpoint cannot skip its area right
        sectionOnly.ShouldBeEmpty();
    }

    [Fact]
    public void SectionOnlyExceptions_AreStillWriteControllers()
    {
        // Given the exceptions list
        // When  it is compared with the controllers that write
        var stale = WriteControllersWithSectionPolicyOnly.Keys.Except(WriteControllers.Select(t => t.Name)).ToList();

        // Then  nothing listed has stopped writing, so the list cannot rot
        stale.ShouldBeEmpty();
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
}
