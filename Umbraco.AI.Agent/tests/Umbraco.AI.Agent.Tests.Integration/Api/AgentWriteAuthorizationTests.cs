// GP-2 (agents): a new write endpoint can't skip the Manage agents right

using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shouldly;

using Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Authorization;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Api;

/// <summary>
/// Every agent controller with a POST, PUT, PATCH or DELETE action carries the Manage agents policy, unless it
/// is listed here as running or suggesting for the caller. Found by reflection, so a new write controller is
/// covered without editing a list.
/// </summary>
public class AgentWriteAuthorizationTests
{
    // Write verbs that run or suggest for the caller rather than change an agent, so any backoffice user may call them.
    private static readonly Dictionary<string, string> SectionOnlyWriteControllers = new()
    {
        [nameof(RunAgentController)] = "Runs an agent for the caller; the service refuses an agent they can't use.",
        [nameof(StreamAgentController)] = "Streams an agent for the caller; the service refuses an agent they can't use.",
        [nameof(StreamAgentAGUIController)] = "Streams an agent for the caller; the service refuses an agent they can't use.",
        [nameof(SuggestStartersAgentController)] = "Drafts starter prompts for the caller and saves nothing.",
    };

    private static readonly Type[] WriteControllers = typeof(AgentControllerBase).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AgentControllerBase).IsAssignableFrom(t))
        .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute)))
        .ToArray();

    private static IEnumerable<string?> PoliciesOf(Type controller)
        => controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Select(a => a.Policy);

    [Fact]
    public void EveryAgentWriteController_RequiresManageAgents_OrIsListedAsNotChangingAgents()
    {
        // Given every agent controller with a write action
        // When  the ones not listed as exceptions are checked for the Manage agents policy
        var unguarded = WriteControllers
            .Where(t => !SectionOnlyWriteControllers.ContainsKey(t.Name))
            .Where(t => !PoliciesOf(t).Contains(AIAuthorizationPolicies.ManageAgentsAI))
            .Select(t => t.Name)
            .ToList();

        // Then  none is left without it
        WriteControllers.ShouldNotBeEmpty();
        unguarded.ShouldBeEmpty();
    }

    [Fact]
    public void ManageAgentsPolicy_AsksForTheManageAgentsRight()
    {
        // Given the policy the agent controllers use
        var options = new AuthorizationOptions();
        AIAuthorizationPolicies.Register(options);

        // When  its requirements are read
        var verbs = options.GetPolicy(AIAuthorizationPolicies.ManageAgentsAI)!.Requirements
            .OfType<AIPermissionRequirement>().Select(r => r.Verb);

        // Then  it asks for Uai.Agent.Manage, not another area's right
        verbs.ShouldBe([AIPermissions.ManageAgents]);
    }

    [Fact]
    public void SectionOnlyExceptions_AreStillWriteControllers()
    {
        // Given the exceptions list
        // When  it is compared with the controllers that write
        var stale = SectionOnlyWriteControllers.Keys.Except(WriteControllers.Select(t => t.Name)).ToList();

        // Then  nothing listed has stopped writing, so the list cannot rot
        stale.ShouldBeEmpty();
    }
}
