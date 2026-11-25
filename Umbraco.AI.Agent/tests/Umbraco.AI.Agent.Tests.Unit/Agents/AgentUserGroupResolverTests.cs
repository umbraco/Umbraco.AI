// GP-14 (AC3, AC4)

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Cms.Core;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// IAIAgentUserGroupResolver collection turns an onBehalfOf value into group keys, or a refusal.
/// </summary>
public class AgentUserGroupResolverTests
{
    private static readonly Guid Automation = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task Resolve_KnownKind_ReturnsResolverGroups()
    {
        // Given a resolver for kind test returning [automation]
        var service = CreateService(new FakeResolver("test", _ => Succeed(Automation)));

        // When  onBehalfOf test:1 is resolved
        var result = await service.ResolveUserGroupsAsync("test:1");

        // Then  the result holds [automation]
        result.Success.ShouldBeTrue();
        result.Result.ShouldBe([Automation]);
    }

    [Fact]
    public async Task Resolve_KindInDifferentCase_StillFindsResolver()
    {
        // Given a resolver for kind test
        var service = CreateService(new FakeResolver("test", _ => Succeed(Automation)));

        // When  onBehalfOf TEST:1 is resolved
        var result = await service.ResolveUserGroupsAsync("TEST:1");

        // Then  the resolver answers
        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Resolve_ValueWithColons_PassesEverythingAfterFirstColon()
    {
        // Given a resolver that records the value it gets
        string? seen = null;
        var service = CreateService(new FakeResolver("test", v =>
        {
            seen = v;
            return Succeed();
        }));

        // When  onBehalfOf test:a:b is resolved
        await service.ResolveUserGroupsAsync("test:a:b");

        // Then  the resolver gets a:b
        seen.ShouldBe("a:b");
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Resolve_UnknownKind_IsNotHandled()
    {
        // Given no resolver for kind unknown
        var service = CreateService(new FakeResolver("test", _ => Succeed()));

        // When  onBehalfOf unknown:1 is resolved
        var result = await service.ResolveUserGroupsAsync("unknown:1");

        // Then  the result is not-handled (maps to 400, AC4)
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.NotHandled);
    }

    [Fact]
    public async Task Resolve_ResolverRefusesCaller_IsForbidden()
    {
        // Given a resolver that says the caller can't act for W
        var service = CreateService(new FakeResolver("test", _ => Fail(AIAgentUserGroupResolutionStatus.Forbidden)));

        // When  onBehalfOf test:W is resolved
        var result = await service.ResolveUserGroupsAsync("test:W");

        // Then  the result is forbidden (maps to 403, AC3)
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Fact]
    public async Task Resolve_NoUser_IsNoUser()
    {
        // Given a resolver that says W has no service account
        var service = CreateService(new FakeResolver("test", _ => Fail(AIAgentUserGroupResolutionStatus.NoUser)));

        // When  onBehalfOf test:W is resolved
        var result = await service.ResolveUserGroupsAsync("test:W");

        // Then  the result is no-user (maps to 400, AC4)
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.NoUser);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nocolon")]
    [InlineData(":1")]
    [InlineData("test:")]
    [InlineData(" :1")]
    [InlineData("test: ")]
    public async Task Resolve_MalformedValue_IsMalformedAndNoResolverRuns(string onBehalfOf)
    {
        // Given a resolver for kind test
        var called = false;
        var service = CreateService(new FakeResolver("test", _ =>
        {
            called = true;
            return Succeed();
        }));

        // When  a value without kind or value is resolved
        var result = await service.ResolveUserGroupsAsync(onBehalfOf);

        // Then  the result is malformed (maps to 400) and the resolver was not asked
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Malformed);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_TwoResolversSameKind_Throws()
    {
        // Given two resolvers claiming kind test (one in different case)
        var service = CreateService(
            new FakeResolver("test", _ => Succeed(Automation)),
            new FakeResolver("TEST", _ => Succeed()));

        // When  any onBehalfOf is resolved, then it fails closed naming the clash
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => service.ResolveUserGroupsAsync("test:1"));
        ex.Message.ShouldContain(typeof(FakeResolver).FullName!);
        ex.Message.ShouldContain("test");
    }

    [Fact]
    public async Task Resolve_TwoResolversSameKind_ThrowsForEveryKind()
    {
        // Given a clash on kind test and a fine resolver for kind other
        var service = CreateService(
            new FakeResolver("test", _ => Succeed()),
            new FakeResolver("test", _ => Succeed()),
            new FakeResolver("other", _ => Succeed()));

        // When  the other kind is resolved, then the misconfiguration still surfaces
        await Should.ThrowAsync<InvalidOperationException>(() => service.ResolveUserGroupsAsync("other:1"));
    }

    [Fact]
    public async Task Resolve_FailedResultWithSuccessStatus_IsForbidden()
    {
        var service = CreateService(new FakeResolver("test", _ => Fail(AIAgentUserGroupResolutionStatus.Success)));

        var result = await service.ResolveUserGroupsAsync("test:1");

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Fact]
    public async Task Resolve_SuccessWithNullList_IsForbidden()
    {
        var service = CreateService(new FakeResolver("test", _ =>
            Attempt.SucceedWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
                AIAgentUserGroupResolutionStatus.Success, null!)));

        var result = await service.ResolveUserGroupsAsync("test:1");

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Fact]
    public async Task Resolve_SuccessWithNonSuccessStatus_IsForbidden()
    {
        var service = CreateService(new FakeResolver("test", _ =>
            Attempt.SucceedWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
                AIAgentUserGroupResolutionStatus.NoUser, [Automation])));

        var result = await service.ResolveUserGroupsAsync("test:1");

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Theory]
    [InlineData(AIAgentUserGroupResolutionStatus.Malformed)]
    [InlineData(AIAgentUserGroupResolutionStatus.NotHandled)]
    public async Task Resolve_ResolverReturnsDispatcherStatus_IsForbidden(AIAgentUserGroupResolutionStatus status)
    {
        var service = CreateService(new FakeResolver("test", _ => Fail(status)));

        var result = await service.ResolveUserGroupsAsync("test:1");

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    #endregion

    private static AIAgentUserGroupResolutionService CreateService(params IAIAgentUserGroupResolver[] resolvers)
        => new(new AIAgentUserGroupResolverCollection(() => resolvers));

    private static Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> Succeed(params Guid[] groups)
        => Attempt.SucceedWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
            AIAgentUserGroupResolutionStatus.Success, groups);

    private static Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> Fail(
        AIAgentUserGroupResolutionStatus status)
        => Attempt.FailWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(status, []);

    private sealed class FakeResolver(
        string kind,
        Func<string, Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> resolve) : IAIAgentUserGroupResolver
    {
        public string Kind => kind;

        public Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
            string value,
            CancellationToken cancellationToken = default)
            => Task.FromResult(resolve(value));
    }
}
