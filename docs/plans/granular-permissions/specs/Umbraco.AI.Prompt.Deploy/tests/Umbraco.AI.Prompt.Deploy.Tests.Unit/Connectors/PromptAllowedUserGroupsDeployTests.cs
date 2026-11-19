// GP-17 (AC3, AC4, AC5)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Prompt.Deploy.Tests.Unit.Connectors;

/// <summary>
/// Prompt user groups travel by alias through the Deploy connector.
/// </summary>
public class PromptAllowedUserGroupsDeployTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T41")]
    public async Task ExportThenImport_MapsAliasToTargetKey()
    {
        // Given a prompt limited to Translators and a target whose translators group has a different key
        // When  it is exported and imported
        // Then  its AllowedUserGroupIds holds the target's translators key (AC3)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T41")]
    public async Task Import_WithMissingGroup_FailsNamingAlias()
    {
        // Given a target site with no translators group
        // When  the prompt is imported
        // Then  the import fails with a message naming translators (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T41")]
    public async Task Import_WithMissingGroup_SavesNothing()
    {
        // Given that failed import
        // When  the target site's prompt is read
        // Then  it is unchanged (AC5)
        await Task.CompletedTask;
    }

    #endregion
}
