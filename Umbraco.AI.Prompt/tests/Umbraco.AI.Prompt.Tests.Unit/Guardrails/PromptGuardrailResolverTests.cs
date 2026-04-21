using Moq;
using Shouldly;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Prompt.Core.Guardrails;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;
using CoreConstants = Umbraco.AI.Core.Constants;
using PromptConstants = Umbraco.AI.Prompt.Core.Constants;

namespace Umbraco.AI.Prompt.Tests.Unit.Guardrails;

/// <summary>
/// Verifies that PromptGuardrailResolver suppresses itself when a full guardrail override is set on
/// the runtime context. Mirrors the Core ProfileGuardrailResolver suppression test so the
/// profile/agent/prompt axis is covered end to end.
/// </summary>
public class PromptGuardrailResolverTests
{
    private readonly Mock<IAIGuardrailService> _guardrailServiceMock = new();
    private readonly Mock<IAIPromptService> _promptServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    [Fact]
    public async Task ResolveAsync_OverridePresent_ReturnsEmpty()
    {
        var promptId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(PromptConstants.MetadataKeys.PromptId, promptId);
        runtimeContext.SetValue(CoreConstants.ContextKeys.GuardrailIdsOverride, (IReadOnlyList<Guid>)[overrideId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        var resolver = new PromptGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _promptServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
        _promptServiceMock.Verify(
            x => x.GetPromptAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _guardrailServiceMock.Verify(
            x => x.GetGuardrailsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NoPromptId_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var resolver = new PromptGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _promptServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
    }
}
