// GP-12 (AC3)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

/// <summary>
/// AllowedUserGroupIds is stored, read back and versioned on prompts.
/// </summary>
public class PromptAllowedUserGroupsTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T28")]
    public async Task SaveAndReload_KeepsAllowedUserGroupIds()
    {
        // Given a prompt saved with [editors key]
        // When  it is read back
        // Then  its AllowedUserGroupIds is [editors key]
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T28")]
    public async Task Rollback_RestoresEmptyList()
    {
        // Given a prompt saved with no groups, then with [editors]
        // When  it is rolled back to the first version
        // Then  its AllowedUserGroupIds is empty (AC3)
        await Task.CompletedTask;
    }

    #endregion
}
