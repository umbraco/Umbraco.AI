// GP-17 (AC1, AC2, AC4, AC5); spec guess 3
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Deploy.Tests.Unit.Connectors;

/// <summary>
/// Agent user groups travel by alias through the Deploy connector.
/// </summary>
public class AgentAllowedUserGroupsDeployTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T40")]
    public async Task Export_WritesGroupAlias()
    {
        // Given an agent limited to the custom group Translators
        // When  it is exported
        // Then  the artifact's AllowedUserGroups is [translators] (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T40")]
    public async Task Import_MapsAliasToTargetKey()
    {
        // Given a target site whose translators group has a different key
        // When  the agent is imported
        // Then  its AllowedUserGroupIds holds the target's translators key (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T40")]
    public async Task Import_WithMissingGroup_FailsNamingAlias()
    {
        // Given a target site with no translators group
        // When  the agent is imported
        // Then  the import fails with a message naming translators (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T40")]
    public async Task Import_WithMissingGroup_SavesNothing()
    {
        // Given that failed import
        // When  the target site's agent is read
        // Then  it is unchanged (AC5)
        await Task.CompletedTask;
    }

    #endregion
}
