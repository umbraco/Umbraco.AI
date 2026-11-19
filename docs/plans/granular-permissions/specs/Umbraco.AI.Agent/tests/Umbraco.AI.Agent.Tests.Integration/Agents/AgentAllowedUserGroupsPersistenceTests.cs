// GP-7 (AC4, AC5, AC7)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Agents;

/// <summary>
/// AllowedUserGroupIds is stored, read back, versioned and filtered on the agent table (SQLite).
/// </summary>
public class AgentAllowedUserGroupsPersistenceTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T14")]
    public async Task SaveAndReload_KeepsAllowedUserGroupIds()
    {
        // Given an agent saved with allowedUserGroupIds [editors key]
        // When  it is read back
        // Then  its AllowedUserGroupIds is [editors key] (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T14")]
    public async Task ExistingRow_ReadsAsEmptyList()
    {
        // Given an agent row from before the migration (column NULL)
        // When  it is read
        // Then  its AllowedUserGroupIds is empty (R9)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T14")]
    public async Task Rollback_RestoresEmptyList()
    {
        // Given an agent saved with no groups, then saved with [editors]
        // When  it is rolled back to the first version
        // Then  its AllowedUserGroupIds is empty (AC5)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T16")]
    public async Task ListFilter_OnlyDeletedGroup_MatchesNoOne()
    {
        // Given an agent limited only to a deleted group's key
        // When  a non-manager lists agents
        // Then  the agent isn't listed (AC7)
        await Task.CompletedTask;
    }

    #endregion
}
