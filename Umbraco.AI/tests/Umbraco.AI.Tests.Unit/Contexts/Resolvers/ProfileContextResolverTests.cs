using Umbraco.AI.Core;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;

namespace Umbraco.AI.Tests.Unit.Contexts.Resolvers;

public class ProfileContextResolverTests
{
    private readonly Mock<IAIContextService> _contextServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock = new();

    private ProfileContextResolver CreateResolver() =>
        new(_runtimeContextAccessorMock.Object, _contextServiceMock.Object, _profileServiceMock.Object);

    [Fact]
    public async Task ResolveAsync_NoProfileId_ReturnsEmpty()
    {
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

        var result = await CreateResolver().ResolveAsync();

        result.Resources.ShouldBeEmpty();
        result.Sources.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ProfileContextIds_Resolved()
    {
        var profileId = Guid.NewGuid();
        var contextId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { ContextIds = [contextId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _contextServiceMock.Setup(x => x.GetContextAsync(contextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIContextBuilder().WithId(contextId).WithName("profile-ctx").Build());

        var result = await CreateResolver().ResolveAsync();

        result.Sources.ShouldContain(s => s.EntityName == profile.Name && s.ContextName == "profile-ctx");
    }

    [Fact]
    public async Task ResolveAsync_OverrideTakesPrecedenceOverProfile()
    {
        var profileId = Guid.NewGuid();
        var profileContextId = Guid.NewGuid();
        var overrideContextId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { ContextIds = [profileContextId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        runtimeContext.SetValue(Constants.ContextKeys.ContextIdsOverride, (IReadOnlyList<Guid>)[overrideContextId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _contextServiceMock.Setup(x => x.GetContextAsync(overrideContextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIContextBuilder().WithId(overrideContextId).WithName("override-ctx").Build());

        var result = await CreateResolver().ResolveAsync();

        result.Sources.ShouldContain(s => s.ContextName == "override-ctx");
        _contextServiceMock.Verify(x => x.GetContextAsync(profileContextId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_EmptyOverride_ReturnsEmpty()
    {
        var profileId = Guid.NewGuid();
        var profileContextId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { ContextIds = [profileContextId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        runtimeContext.SetValue(Constants.ContextKeys.ContextIdsOverride, (IReadOnlyList<Guid>)[]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await CreateResolver().ResolveAsync();

        result.Resources.ShouldBeEmpty();
        _contextServiceMock.Verify(x => x.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AdditionalContextIds_AppendedToProfile()
    {
        var profileId = Guid.NewGuid();
        var profileContextId = Guid.NewGuid();
        var extraContextId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { ContextIds = [profileContextId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        runtimeContext.SetValue(Constants.ContextKeys.AdditionalContextIds, (IReadOnlyList<Guid>)[extraContextId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _contextServiceMock.Setup(x => x.GetContextAsync(profileContextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIContextBuilder().WithId(profileContextId).WithName("profile-ctx").Build());
        _contextServiceMock.Setup(x => x.GetContextAsync(extraContextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIContextBuilder().WithId(extraContextId).WithName("extra-ctx").Build());

        var result = await CreateResolver().ResolveAsync();

        result.Sources.Select(s => s.ContextName).ShouldBe(["profile-ctx", "extra-ctx"]);
    }

    [Fact]
    public async Task ResolveAsync_AdditionalDeduplicatedAgainstProfile()
    {
        var profileId = Guid.NewGuid();
        var sharedId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithSettings(new AIChatProfileSettings { ContextIds = [sharedId] })
            .Build();

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, profileId);
        runtimeContext.SetValue(Constants.ContextKeys.AdditionalContextIds, (IReadOnlyList<Guid>)[sharedId]);
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
        _profileServiceMock.Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _contextServiceMock.Setup(x => x.GetContextAsync(sharedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIContextBuilder().WithId(sharedId).WithName("shared-ctx").Build());

        var result = await CreateResolver().ResolveAsync();

        result.Sources.Count.ShouldBe(1);
        _contextServiceMock.Verify(x => x.GetContextAsync(sharedId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
