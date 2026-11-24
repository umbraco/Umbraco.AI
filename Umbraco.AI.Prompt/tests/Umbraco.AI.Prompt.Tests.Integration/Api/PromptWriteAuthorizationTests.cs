// GP-2 (prompts): a new write endpoint can't skip the Manage prompts right

using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shouldly;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;
using Umbraco.AI.Web.Authorization;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Integration.Api;

/// <summary>
/// Every prompt controller with a POST, PUT, PATCH or DELETE action carries the Manage prompts policy, unless it
/// is listed here as running a prompt for the caller. Found by reflection, so a new write controller is
/// covered without editing a list.
/// </summary>
public class PromptWriteAuthorizationTests
{
    // Write verbs that run a prompt for the caller rather than change one, so any backoffice user may call them.
    private static readonly Dictionary<string, string> SectionOnlyWriteControllers = new()
    {
        [nameof(ExecutePromptController)] = "Runs a prompt for the caller; the service refuses a prompt they can't use.",
    };

    private static readonly Type[] WriteControllers = typeof(PromptControllerBase).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(PromptControllerBase).IsAssignableFrom(t))
        .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute)))
        .ToArray();

    private static IEnumerable<string?> PoliciesOf(Type controller)
        => controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Select(a => a.Policy);

    [Fact]
    public void EveryPromptWriteController_RequiresManagePrompts_OrIsListedAsNotChangingPrompts()
    {
        // Given every prompt controller with a write action
        // When  the ones not listed as exceptions are checked for the Manage prompts policy
        var unguarded = WriteControllers
            .Where(t => !SectionOnlyWriteControllers.ContainsKey(t.Name))
            .Where(t => !PoliciesOf(t).Contains(AIAuthorizationPolicies.ManagePromptsAI))
            .Select(t => t.Name)
            .ToList();

        // Then  none is left without it
        WriteControllers.ShouldNotBeEmpty();
        unguarded.ShouldBeEmpty();
    }

    [Fact]
    public void ManagePromptsPolicy_AsksForTheManagePromptsRight()
    {
        // Given the policy the prompt controllers use
        var options = new AuthorizationOptions();
        AIAuthorizationPolicies.Register(options);

        // When  its requirements are read
        var verbs = options.GetPolicy(AIAuthorizationPolicies.ManagePromptsAI)!.Requirements
            .OfType<AIPermissionRequirement>().Select(r => r.Verb);

        // Then  it asks for Uai.Prompt.Manage, not another area's right
        verbs.ShouldBe([AIPermissions.ManagePrompts]);
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
