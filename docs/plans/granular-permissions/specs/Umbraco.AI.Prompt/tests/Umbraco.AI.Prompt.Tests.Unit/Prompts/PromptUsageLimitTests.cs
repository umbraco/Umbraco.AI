// GP-13 (AC2, AC3, AC5), GP-12 (AC4, AC5)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

/// <summary>
/// IAIPromptService hides and refuses limited prompts, and guards the group list.
/// </summary>
public class PromptUsageLimitTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T30")]
    public async Task List_ManagedAccessWithManagePrompts_IncludesLimitedPrompt()
    {
        // Given a user with Uai.Prompt.Manage not in editors
        // When  prompts are listed with managed access
        // Then  the limited prompt is included (GP-13 AC5)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T30")]
    public async Task List_ForNonMember_ExcludesLimitedPrompt()
    {
        // Given a prompt limited to editors and a user not in it
        // When  prompts are listed
        // Then  the prompt isn't included (GP-13 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T30")]
    public async Task GetByAlias_ForNonMemberWithoutManage_ReturnsNull()
    {
        // Given that user, without Uai.Prompt.Manage
        // When  the prompt is fetched by alias
        // Then  it returns null (GP-13 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T30")]
    public async Task Execute_ForNonMember_IsRefused()
    {
        // Given that user
        // When  the prompt is executed
        // Then  a prompt-not-available failure is returned (GP-13 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T30")]
    public async Task Save_AddingSomeoneElsesGroup_IsRefused()
    {
        // Given a non-admin with Uai.Prompt.Manage in editors only
        // When  they save adding translators
        // Then  the save is refused (GP-12 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T30")]
    public async Task Save_EmptyingTheList_ByNonAdmin_IsRefused()
    {
        // Given a prompt limited to editors and a non-admin manager in editors
        // When  they remove editors and save
        // Then  the save is refused (GP-12 AC5)
        await Task.CompletedTask;
    }

    #endregion
}
