// GP-14 (AC3, AC4)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// IAIAgentUserGroupResolver collection turns an onBehalfOf value into group keys, or a refusal.
/// </summary>
public class AgentUserGroupResolverTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T20")]
    public async Task Resolve_KnownKind_ReturnsResolverGroups()
    {
        // Given a resolver for kind test returning [automation]
        // When  onBehalfOf test:1 is resolved
        // Then  the result holds [automation]
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T20")]
    public async Task Resolve_UnknownKind_IsNotHandled()
    {
        // Given no resolver for kind unknown
        // When  onBehalfOf unknown:1 is resolved
        // Then  the result is not-handled (maps to 400, AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T20")]
    public async Task Resolve_ResolverRefusesCaller_IsForbidden()
    {
        // Given a resolver that says the caller can't act for W
        // When  onBehalfOf test:W is resolved
        // Then  the result is forbidden (maps to 403, AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T20")]
    public async Task Resolve_NoUser_IsNoUser()
    {
        // Given a resolver that says W has no service account
        // When  onBehalfOf test:W is resolved
        // Then  the result is no-user (maps to 400, AC4)
        await Task.CompletedTask;
    }

    #endregion
}
