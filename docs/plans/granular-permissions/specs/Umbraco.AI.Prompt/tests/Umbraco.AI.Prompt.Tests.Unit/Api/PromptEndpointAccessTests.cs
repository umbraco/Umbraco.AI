// GP-2 (prompts), GP-13 (AC2-AC5), GP-12 (AC4, AC5)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Api;

/// <summary>
/// Prompt controllers map the service results to HTTP; T32 proves it live.
/// </summary>
public class PromptEndpointAccessTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T31")]
    public async Task GetPrompts_IncludeRestricted_ForManager_ListsLimitedPrompt()
    {
        // Given a user with Uai.Prompt.Manage not in editors
        // When  they GET prompts?includeRestricted=true
        // Then  the limited prompt is listed (GP-13 AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T31")]
    public async Task WriteControllers_RequireManagePromptsPolicy()
    {
        // Given the create, update and delete prompt controllers (theory)
        // When  their policies are read
        // Then  they contain the Manage prompts policy (GP-2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T31")]
    public async Task GetPrompts_ForNonMember_HidesLimitedPrompt()
    {
        // Given a prompt limited to editors and a user not in it
        // When  they GET prompts
        // Then  the prompt isn't listed (GP-13 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T31")]
    public async Task GetPromptDetail_ForNonMemberWithoutManage_Returns404()
    {
        // Given that user, without Uai.Prompt.Manage
        // When  they GET prompts/{alias}
        // Then  the response is 404 (GP-13 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T31")]
    public async Task Execute_ForNonMember_Returns403PromptNotAvailable()
    {
        // Given that user
        // When  they POST prompts/{alias}/execute
        // Then  the response is 403 ProblemDetails titled "Prompt not available" (GP-13 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T31")]
    public async Task PutPrompt_AddingSomeoneElsesGroup_Returns403()
    {
        // Given a non-admin with Uai.Prompt.Manage in editors only
        // When  they PUT prompts/{alias} adding translators
        // Then  the response is 403 (GP-12 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T31")]
    public async Task PutPrompt_EmptyingTheList_ByNonAdmin_Returns403()
    {
        // Given a prompt limited to editors and a non-admin manager in editors
        // When  they PUT prompts/{alias} removing editors
        // Then  the response is 403 (GP-12 AC5)
        await Task.CompletedTask;
    }

    #endregion
}
