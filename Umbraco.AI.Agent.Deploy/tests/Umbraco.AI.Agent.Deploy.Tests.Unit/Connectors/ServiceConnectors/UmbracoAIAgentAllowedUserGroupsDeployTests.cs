using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using Xunit;

namespace Umbraco.AI.Agent.Deploy.Tests.Unit.Connectors.ServiceConnectors;

/// <summary>
/// An agent's allowed user groups (empty means everyone) travel by alias, because a custom user group
/// has a different key on each environment and Deploy doesn't transfer user groups.
/// </summary>
public class UmbracoAIAgentAllowedUserGroupsDeployTests
{
    private static readonly Guid SourceTranslatorsKey = Guid.NewGuid();
    private static readonly Guid TargetTranslatorsKey = Guid.NewGuid();

    private readonly Mock<IAIAgentService> _agentService = new();
    private readonly Mock<IUserGroupService> _sourceGroups = new();
    private readonly Mock<IUserGroupService> _targetGroups = new();
    private AIAgent? _saved;

    public UmbracoAIAgentAllowedUserGroupsDeployTests()
    {
        _sourceGroups.Setup(x => x.GetAsync(SourceTranslatorsKey)).ReturnsAsync(Group("translators", SourceTranslatorsKey));

        _targetGroups.Setup(x => x.GetAsync("translators")).ReturnsAsync(Group("translators", TargetTranslatorsKey));
        _targetGroups.Setup(x => x.GetAsync(TargetTranslatorsKey)).ReturnsAsync(Group("translators", TargetTranslatorsKey));

        _agentService
            .Setup(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent a, CancellationToken _) => _saved = a);
    }

    [Fact]
    public async Task Export_ListsTheGroupByAlias()
    {
        var artifact = await Connector(_sourceGroups).GetArtifactAsync(Udi(), Agent([SourceTranslatorsKey]));

        artifact!.AllowedUserGroups.ShouldBe(["translators"]);
    }

    [Fact]
    public async Task Import_HoldsTheTargetsKeyForTheAlias()
    {
        await ImportAsync(new AIAgentArtifact(Udi()) { Alias = "agent", Name = "Agent", AllowedUserGroups = ["translators"] });

        _saved!.AllowedUserGroupIds.ShouldBe([TargetTranslatorsKey]);
    }

    [Fact]
    public async Task RoundTrip_FollowsTheGroupToItsKeyOnTheTarget()
    {
        var artifact = await Connector(_sourceGroups).GetArtifactAsync(Udi(), Agent([SourceTranslatorsKey]));

        await ImportAsync(artifact!);

        _saved!.AllowedUserGroupIds.ShouldBe([TargetTranslatorsKey]);
    }

    [Fact]
    public async Task Import_AliasMissingOnTheTarget_FailsNamingTheAlias()
    {
        var artifact = new AIAgentArtifact(Udi()) { Alias = "agent", Name = "Agent", AllowedUserGroups = ["translators", "sales"] };

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => ImportAsync(artifact));

        ex.Message.ShouldContain("sales");
    }

    [Fact]
    public async Task Import_AliasMissingOnTheTarget_SavesNothing()
    {
        var artifact = new AIAgentArtifact(Udi()) { Alias = "agent", Name = "Agent", AllowedUserGroups = ["sales"] };

        await Should.ThrowAsync<InvalidOperationException>(() => ImportAsync(artifact));

        _agentService.Verify(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmptyList_RoundTripsAsEveryone()
    {
        var artifact = await Connector(_sourceGroups).GetArtifactAsync(Udi(), Agent([]));

        await ImportAsync(artifact!);

        artifact!.AllowedUserGroups.ShouldBeEmpty();
        _saved!.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Import_OldArtifactWithoutTheProperty_ImportsAsEveryone()
    {
        // An artifact written before the property existed never sets it, so it keeps the default.
        await ImportAsync(new AIAgentArtifact(Udi()) { Alias = "agent", Name = "Agent" });

        _saved!.AllowedUserGroupIds.ShouldBeEmpty();
    }

    private UmbracoAIAgentServiceConnector Connector(Mock<IUserGroupService> userGroups)
    {
        var settings = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        settings.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        return new UmbracoAIAgentServiceConnector(_agentService.Object, new Mock<IAIProfileService>().Object, settings.Object, userGroups.Object, new Mock<IAIAgentUnrestrictedReader>().Object);
    }

    private async Task ImportAsync(AIAgentArtifact artifact)
    {
        var connector = Connector(_targetGroups);
        var state = new ArtifactDeployState<AIAgentArtifact, AIAgent>(artifact, null, connector, 3);
        await connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);
    }

    private static AIAgent Agent(IReadOnlyList<Guid> allowedUserGroupIds) => new()
    {
        Alias = "agent",
        Name = "Agent",
        AgentType = AIAgentType.Standard,
        Config = new AIStandardAgentConfig(),
        AllowedUserGroupIds = allowedUserGroupIds,
    };

    private static GuidUdi Udi() => new(UmbracoAIAgentConstants.UdiEntityType.Agent, Guid.NewGuid());

    private static IUserGroup Group(string alias, Guid key)
    {
        var group = new Mock<IUserGroup>();
        group.SetupGet(x => x.Alias).Returns(alias);
        group.SetupGet(x => x.Key).Returns(key);
        return group.Object;
    }
}
