using Umbraco.AI.Core;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;

namespace Umbraco.AI.Tests.Unit.Guardrails.Resolvers;

/// <summary>
/// Verifies that WithGuardrails truly replaces profile-level guardrails (override suppresses profile resolver)
/// and AppendGuardrails truly appends (additional resolver runs on top).
/// </summary>
public class GuardrailResolverOverrideTests
{
    private readonly Mock<IAIGuardrailService> _guardrailServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    [Fact]
    public async Task ProfileResolver_OverridePresent_ReturnsEmpty()
    {
        var profileId = Guid.NewGuid();
        var profileGuardrailId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { GuardrailIds = [profileGuardrailId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        runtimeContext.SetValue(Constants.ContextKeys.GuardrailIdsOverride, (IReadOnlyList<Guid>)[overrideId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var resolver = new ProfileGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _profileServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
        _guardrailServiceMock.Verify(
            x => x.GetGuardrailsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProfileResolver_NoOverride_ReturnsProfileGuardrails()
    {
        var profileId = Guid.NewGuid();
        var profileGuardrailId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { GuardrailIds = [profileGuardrailId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _guardrailServiceMock
            .Setup(x => x.GetGuardrailsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AIGuardrailBuilder().WithId(profileGuardrailId).Build()]);

        var resolver = new ProfileGuardrailResolver(
            _runtimeContextAccessorMock.Object, _guardrailServiceMock.Object, _profileServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBe([profileGuardrailId]);
    }

    [Fact]
    public async Task AdditionalResolver_AdditionalKeyPresent_ReturnsIds()
    {
        var additionalId = Guid.NewGuid();
        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.AdditionalGuardrailIds, (IReadOnlyList<Guid>)[additionalId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _guardrailServiceMock
            .Setup(x => x.GetGuardrailsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AIGuardrailBuilder().WithId(additionalId).Build()]);

        var resolver = new AdditionalGuardrailIdsResolver(_runtimeContextAccessorMock.Object, _guardrailServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBe([additionalId]);
        result.Source.ShouldBe("Additional");
    }

    [Fact]
    public async Task AdditionalResolver_NotSet_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var resolver = new AdditionalGuardrailIdsResolver(_runtimeContextAccessorMock.Object, _guardrailServiceMock.Object);

        var result = await resolver.ResolveAsync();

        result.GuardrailIds.ShouldBeEmpty();
    }
}
