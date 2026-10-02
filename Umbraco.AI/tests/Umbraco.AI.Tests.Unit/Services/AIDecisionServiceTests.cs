#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Tests.Unit.Services;

/// <summary>
/// DC-3 (AC3) — real entry point. <see cref="IAIDecisionService.AskAsync{TResponse}(string, AIDecisionQuestion{TResponse}, AIDecisionOptions?, CancellationToken)"/>
/// resolved exactly as a caller would use it (alias in, typed response out), not the capability/client
/// directly.
/// </summary>
/// <remarks>
/// The profile-alias overload delegates internally to the builder-based main path (T8's inline execution
/// pipeline — see <see cref="AIDecisionServiceRealPipelineTests"/> for coverage of that pipeline's
/// notification/scope wiring against a real factory), so this only needs to pin the two collaborators
/// the contract in <c>SPEC.md</c> requires (<see cref="IAIProfileService"/> for alias resolution,
/// <see cref="IAIDecisionClientFactory"/> for building the client) plus the runtime-context/event
/// plumbing the delegated builder path now also depends on.
/// </remarks>
public class AIDecisionServiceTests
{
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIDecisionClientFactory> _clientFactoryMock = new();
    private readonly Mock<IEventAggregator> _eventAggregatorMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock = new();
    private readonly Mock<IAIRuntimeContextScopeProvider> _scopeProviderMock = new();

    private AIDecisionService CreateService()
    {
        _eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var runtimeContext = new AIRuntimeContext([]);
        _contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);

        var mockScope = new Mock<IAIRuntimeContextScope>();
        mockScope.Setup(s => s.Context).Returns(runtimeContext);
        _scopeProviderMock
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                _contextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
                return mockScope.Object;
            });

        var contributors = new AIRuntimeContextContributorCollection(() => []);

        return new AIDecisionService(
            _profileServiceMock.Object,
            _clientFactoryMock.Object,
            _eventAggregatorMock.Object,
            _contextAccessorMock.Object,
            _scopeProviderMock.Object,
            contributors);
    }

    [Fact]
    public async Task AskAsync_WithKnownAlias_ResolvesProfileAndReturnsResponse()
    {
        // Arrange
        var profile = new AIProfileBuilder()
            .WithAlias("spam-check")
            .WithCapability(AICapability.Decision)
            .Build();
        var fakeClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.95 });

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync("spam-check", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        var service = CreateService();
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        var response = await service.AskAsync("spam-check", question);

        // Assert
        response.ShouldBeOfType<AIBinaryDecisionResponse>().Answer.ShouldBe(true);
    }

    [Fact]
    public async Task AskAsync_WithProfileId_ResolvesProfileAndReturnsResponse()
    {
        // Arrange
        var profile = new AIProfileBuilder()
            .WithAlias("spam-check")
            .WithCapability(AICapability.Decision)
            .Build();
        var fakeClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.95 });

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        var service = CreateService();
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        var response = await service.AskAsync(profile.Id, question);

        // Assert
        response.ShouldBeOfType<AIBinaryDecisionResponse>().Answer.ShouldBe(true);
    }

    [Fact]
    public async Task AskAsync_WithUnknownAlias_ThrowsInvalidOperationException()
    {
        // Arrange
        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        var service = CreateService();
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.AskAsync("missing", question));
    }
}
