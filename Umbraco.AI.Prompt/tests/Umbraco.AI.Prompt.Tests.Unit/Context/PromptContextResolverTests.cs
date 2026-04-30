using Moq;
using Shouldly;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Prompt.Core.Context;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;
using CoreConstants = Umbraco.AI.Core.Constants;
using PromptConstants = Umbraco.AI.Prompt.Core.Constants;

namespace Umbraco.AI.Prompt.Tests.Unit.Context;

/// <summary>
/// Verifies that PromptContextResolver suppresses itself when a full context override is set on the
/// runtime context — the ProfileContextResolver surfaces the override set, so the prompt resolver
/// must stay silent.
/// </summary>
public class PromptContextResolverTests
{
    private readonly Mock<IAIContextService> _contextServiceMock = new();
    private readonly Mock<IAIPromptService> _promptServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    [Fact]
    public async Task ResolveAsync_OverridePresent_ReturnsEmpty()
    {
        var promptId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(PromptConstants.MetadataKeys.PromptId, promptId);
        runtimeContext.SetValue(CoreConstants.ContextKeys.ContextIdsOverride, (IReadOnlyList<Guid>)[overrideId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        var resolver = new PromptContextResolver(
            _runtimeContextAccessorMock.Object, _contextServiceMock.Object, _promptServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
        _promptServiceMock.Verify(
            x => x.GetPromptAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contextServiceMock.Verify(
            x => x.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NoPromptId_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var resolver = new PromptContextResolver(
            _runtimeContextAccessorMock.Object, _contextServiceMock.Object, _promptServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }
}
