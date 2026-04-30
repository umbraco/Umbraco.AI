using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Guardrails;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.Guardrails;

/// <summary>
/// Verifies that AgentGuardrailResolver suppresses itself when a full guardrail override is set
/// on the runtime context (e.g., via AIInlineAgentBuilder.SetGuardrails or an execution-options override).
/// Mirrors the Core ProfileGuardrailResolver suppression test so the profile/agent/prompt axis is covered.
/// </summary>
public class AgentGuardrailResolverTests
{
    private readonly Mock<IAIGuardrailService> _guardrailServiceMock = new();
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    [Fact]
    public async Task ResolveAsync_OverridePresent_ReturnsEmpty()
    {
        var agentId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(AgentConstants.ContextKeys.AgentId, agentId);
        runtimeContext.SetValue(CoreConstants.ContextKeys.GuardrailIdsOverride, (IReadOnlyList<Guid>)[overrideId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        var resolver = new AgentGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _agentServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
        _agentServiceMock.Verify(
            x => x.GetAgentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _guardrailServiceMock.Verify(
            x => x.GetGuardrailsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NoAgentId_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var resolver = new AgentGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _agentServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
    }
}
