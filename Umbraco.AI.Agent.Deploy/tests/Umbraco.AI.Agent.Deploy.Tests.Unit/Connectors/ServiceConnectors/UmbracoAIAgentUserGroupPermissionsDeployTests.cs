using System;
using System.Collections.Generic;
using System.Text.Json;
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
/// User groups have different keys on each environment (Deploy doesn't transfer them), so an agent's
/// user group permissions travel by alias and map back to the target's own groups.
/// </summary>
public class UmbracoAIAgentUserGroupPermissionsDeployTests
{
    private static readonly Guid SourceMarketingKey = Guid.NewGuid();
    private static readonly Guid TargetMarketingKey = Guid.NewGuid();
    private static readonly Guid EditorsKey = Guid.NewGuid(); // built-in groups share a key everywhere

    private readonly Mock<IAIAgentService> _agentService = new();
    private readonly Mock<IUserGroupService> _sourceGroups = new();
    private readonly Mock<IUserGroupService> _targetGroups = new();
    private AIAgent? _saved;

    public UmbracoAIAgentUserGroupPermissionsDeployTests()
    {
        _sourceGroups.Setup(x => x.GetAsync(SourceMarketingKey)).ReturnsAsync(Group("marketing", SourceMarketingKey));
        _sourceGroups.Setup(x => x.GetAsync(EditorsKey)).ReturnsAsync(Group("editor", EditorsKey));

        _targetGroups.Setup(x => x.GetAsync("marketing")).ReturnsAsync(Group("marketing", TargetMarketingKey));
        _targetGroups.Setup(x => x.GetAsync("editor")).ReturnsAsync(Group("editor", EditorsKey));
        _targetGroups.Setup(x => x.GetAsync(TargetMarketingKey)).ReturnsAsync(Group("marketing", TargetMarketingKey));
        _targetGroups.Setup(x => x.GetAsync(EditorsKey)).ReturnsAsync(Group("editor", EditorsKey));

        _agentService
            .Setup(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent a, CancellationToken _) => _saved = a);
    }

    [Fact]
    public async Task Export_KeysPermissionsByUserGroupAlias()
    {
        var artifact = await Connector(_sourceGroups).GetArtifactAsync(Udi(), Agent(SourceMarketingKey, Deny("delete_umbraco_content")));

        var permissions = PermissionsIn(artifact!.Config!);
        permissions.TryGetProperty("marketing", out _).ShouldBeTrue();
        permissions.TryGetProperty(SourceMarketingKey.ToString(), out _).ShouldBeFalse();
    }

    [Fact]
    public async Task RoundTrip_DenyFollowsTheGroupToItsKeyOnTheTarget()
    {
        var artifact = await Connector(_sourceGroups).GetArtifactAsync(Udi(), Agent(SourceMarketingKey, Deny("delete_umbraco_content")));

        await ImportAsync(artifact!);

        var permissions = SavedPermissions();
        permissions.ContainsKey(TargetMarketingKey).ShouldBeTrue();
        permissions.ContainsKey(SourceMarketingKey).ShouldBeFalse();
        permissions[TargetMarketingKey].DeniedToolIds.ShouldBe(["delete_umbraco_content"]);
    }

    [Fact]
    public async Task Import_DenyForAGroupMissingOnTheTarget_FailsTheImport()
    {
        var artifact = ArtifactWith("""{"sales":{"deniedToolScopeIds":["content-write"]}}""");

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => ImportAsync(artifact));

        ex.Message.ShouldContain("sales");
        _saved.ShouldBeNull();
    }

    [Fact]
    public async Task Import_AllowOnlyForAGroupMissingOnTheTarget_IsDropped()
    {
        var artifact = ArtifactWith("""{"sales":{"allowedToolIds":["search_umbraco"]},"marketing":{"allowedToolIds":["search_umbraco"]}}""");

        await ImportAsync(artifact);

        var permissions = SavedPermissions();
        permissions.Keys.ShouldBe([TargetMarketingKey]);
    }

    [Fact]
    public async Task Import_OldArtifactKeyedByAGroupKeyThatExistsOnTheTarget_IsKept()
    {
        var artifact = ArtifactWith($$$"""{"{{{EditorsKey}}}":{"deniedToolIds":["delete_umbraco_content"]}}""");

        await ImportAsync(artifact);

        SavedPermissions()[EditorsKey].DeniedToolIds.ShouldBe(["delete_umbraco_content"]);
    }

    [Fact]
    public async Task Import_OldArtifactDenyKeyedByAGroupKeyMissingOnTheTarget_FailsTheImport()
    {
        // Written by an affected version: keyed by the source's group key, which the target doesn't have.
        var artifact = ArtifactWith($$$"""{"{{{SourceMarketingKey}}}":{"deniedToolIds":["delete_umbraco_content"]}}""");

        await Should.ThrowAsync<InvalidOperationException>(() => ImportAsync(artifact));
        _saved.ShouldBeNull();
    }

    [Fact]
    public async Task Import_OldArtifactAllowKeyedByAGroupKeyMissingOnTheTarget_IsDropped()
    {
        var artifact = ArtifactWith($$$"""{"{{{SourceMarketingKey}}}":{"allowedToolIds":["search_umbraco"]}}""");

        await ImportAsync(artifact);

        SavedPermissions().ShouldBeEmpty();
    }

    private UmbracoAIAgentServiceConnector Connector(Mock<IUserGroupService> userGroups)
    {
        var settings = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        settings.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        return new UmbracoAIAgentServiceConnector(_agentService.Object, new Mock<IAIProfileService>().Object, settings.Object, userGroups.Object);
    }

    private async Task ImportAsync(AIAgentArtifact artifact)
    {
        var connector = Connector(_targetGroups);
        var state = new ArtifactDeployState<AIAgentArtifact, AIAgent>(artifact, null, connector, 3);
        await connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);
    }

    private static AIAgentArtifact ArtifactWith(string userGroupPermissionsJson) => new(Udi())
    {
        Alias = "agent",
        Name = "Agent",
        AgentType = nameof(AIAgentType.Standard),
        Config = $$"""{"userGroupPermissions":{{userGroupPermissionsJson}}}""",
    };

    private static AIAgent Agent(Guid userGroupKey, AIAgentUserGroupPermissions permissions) => new()
    {
        Alias = "agent",
        Name = "Agent",
        AgentType = AIAgentType.Standard,
        Config = new AIStandardAgentConfig
        {
            UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions> { [userGroupKey] = permissions },
        },
    };

    private static AIAgentUserGroupPermissions Deny(string toolId) => new() { DeniedToolIds = [toolId] };

    private static GuidUdi Udi() => new(UmbracoAIAgentConstants.UdiEntityType.Agent, Guid.NewGuid());

    private static IUserGroup Group(string alias, Guid key)
    {
        var group = new Mock<IUserGroup>();
        group.SetupGet(x => x.Alias).Returns(alias);
        group.SetupGet(x => x.Key).Returns(key);
        return group.Object;
    }

    private static JsonElement PermissionsIn(string configJson)
        => JsonDocument.Parse(configJson).RootElement.GetProperty("userGroupPermissions").Clone();

    private IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions> SavedPermissions()
    {
        _saved.ShouldNotBeNull();
        return _saved.Config.ShouldBeOfType<AIStandardAgentConfig>().UserGroupPermissions;
    }
}
