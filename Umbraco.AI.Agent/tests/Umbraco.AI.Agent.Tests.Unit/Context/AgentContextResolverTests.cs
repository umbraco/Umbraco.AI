using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Context;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.Context;

/// <summary>
/// Verifies that AgentContextResolver suppresses itself when a full context override is set on the
/// runtime context (e.g., via AIInlineAgentBuilder.SetContexts or an execution-options override) —
/// the ProfileContextResolver surfaces the override set, so the agent resolver must stay silent.
/// </summary>
public class AgentContextResolverTests
{
    private readonly Mock<IAIContextService> _contextServiceMock = new();
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    [Fact]
    public async Task ResolveAsync_OverridePresent_ReturnsEmpty()
    {
        var agentId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(AgentConstants.ContextKeys.AgentId, agentId);
        runtimeContext.SetValue(CoreConstants.ContextKeys.ContextIdsOverride, (IReadOnlyList<Guid>)[overrideId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        var resolver = new AgentContextResolver(
            _runtimeContextAccessorMock.Object, _contextServiceMock.Object, _agentServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
        _agentServiceMock.Verify(
            x => x.GetAgentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contextServiceMock.Verify(
            x => x.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NoAgentId_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var resolver = new AgentContextResolver(
            _runtimeContextAccessorMock.Object, _contextServiceMock.Object, _agentServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }
}
