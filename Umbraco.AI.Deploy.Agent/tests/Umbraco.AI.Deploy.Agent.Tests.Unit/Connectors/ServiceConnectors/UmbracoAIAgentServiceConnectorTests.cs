using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Deploy.Agent.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core;
using Xunit;

namespace Umbraco.AI.Deploy.Agent.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIAgentServiceConnectorTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIAgentServiceConnector _connector;

    public UmbracoAIAgentServiceConnectorTests()
    {
        _agentServiceMock = new Mock<IAIAgentService>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        _connector = new UmbracoAIAgentServiceConnector(
            _agentServiceMock.Object,
            _profileServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    [Fact]
    public async Task GetArtifactAsync_WithAllProperties_CreatesCompleteArtifact()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userGroupId1 = Guid.NewGuid();
        var userGroupId2 = Guid.NewGuid();

        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            Description = "Test description",
            ProfileId = profileId,
            ContextIds = [Guid.NewGuid()],
            SurfaceIds = ["backoffice", "frontend"],
            Scope = new AIAgentScope
            {
                AllowRules = [
                    new AIAgentScopeRule
                    {
                        Sections = ["content"],
                        EntityTypes = ["document"]
                    }
                ]
            },
            AllowedToolIds = ["search", "calculator"],
            AllowedToolScopeIds = ["content", "media"],
            UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions>
            {
                { userGroupId1, new AIAgentUserGroupPermissions { AllowedToolIds = ["read", "write"] } },
                { userGroupId2, new AIAgentUserGroupPermissions { AllowedToolIds = ["read"] } }
            },
            Instructions = "Agent instructions",
            IsActive = true
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, agent);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Alias.ShouldBe("test-agent");
        artifact.Name.ShouldBe("Test Agent");
        artifact.Description.ShouldBe("Test description");
        artifact.Instructions.ShouldBe("Agent instructions");
        artifact.IsActive.ShouldBeTrue();

        // Profile dependency
        artifact.ProfileUdi.ShouldNotBeNull();
        artifact.ProfileUdi.Guid.ShouldBe(profileId);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "umbraco-ai-profile" &&
            ((GuidUdi)d.Udi).Guid == profileId);

        // User group dependencies
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "user-group" &&
            ((GuidUdi)d.Udi).Guid == userGroupId1);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "user-group" &&
            ((GuidUdi)d.Udi).Guid == userGroupId2);

        // Arrays
        artifact.SurfaceIds.ShouldBe(new[] { "backoffice", "frontend" });
        artifact.AllowedToolIds.ShouldBe(new[] { "search", "calculator" });
        artifact.AllowedToolScopeIds.ShouldBe(new[] { "content", "media" });

        // JSON properties
        artifact.Scope.ShouldNotBeNull();
        var scope = JsonSerializer.Deserialize<Dictionary<string, object>>(artifact.Scope.Value);
        scope.ShouldNotBeNull();
        scope.ShouldContainKey("DocumentTypes");

        artifact.UserGroupPermissions.ShouldNotBeNull();
        var permissions = JsonSerializer.Deserialize<Dictionary<string, string[]>>(artifact.UserGroupPermissions.Value);
        permissions.ShouldNotBeNull();
        permissions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetArtifactAsync_WithoutOptionalProperties_CreatesMinimalArtifact()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = null,
            ContextIds = [],
            SurfaceIds = [],
            Scope = null,
            AllowedToolIds = [],
            AllowedToolScopeIds = [],
            UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions>(),
            Instructions = null,
            IsActive = false
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, agent);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.ProfileUdi.ShouldBeNull();
        artifact.Scope.ShouldBeNull();
        artifact.UserGroupPermissions.ShouldNotBeNull(); // Empty dict serialized
        artifact.Instructions.ShouldBeNull();
        artifact.IsActive.ShouldBeFalse();

        // Should not have profile or user group dependencies
        artifact.Dependencies.ShouldNotContain(d => d.Udi.EntityType == "umbraco-ai-profile");
        artifact.Dependencies.ShouldNotContain(d => d.Udi.EntityType == "user-group");
    }

    [Fact]
    public async Task GetArtifactAsync_WithUserGroupPermissions_AddsDependencies()
    {
        // Arrange
        var userGroupId1 = Guid.NewGuid();
        var userGroupId2 = Guid.NewGuid();

        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = null,
            UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions>
            {
                { userGroupId1, new AIAgentUserGroupPermissions { AllowedToolIds = ["read"] } },
                { userGroupId2, new AIAgentUserGroupPermissions { AllowedToolIds = ["write"] } }
            }
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, agent);

        // Assert
        artifact.ShouldNotBeNull();

        // Verify user group dependencies are added
        artifact.Dependencies.Count(d => d.Udi.EntityType == "user-group").ShouldBe(2);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "user-group" &&
            ((GuidUdi)d.Udi).Guid == userGroupId1 &&
            d.Mode == Cms.Core.Deploy.ArtifactDependencyMode.Exist);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "user-group" &&
            ((GuidUdi)d.Udi).Guid == userGroupId2 &&
            d.Mode == Cms.Core.Deploy.ArtifactDependencyMode.Exist);
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullAgent_ReturnsNull()
    {
        // Arrange
        var udi = new GuidUdi("umbraco-ai-agent", Guid.NewGuid());

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, null);

        // Assert
        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = null
        };

        _agentServiceMock
            .Setup(x => x.GetAgentAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var result = await _connector.GetEntityAsync(agentId);

        // Assert
        result.ShouldBe(agent);
    }

    [Fact]
    public void GetEntityName_ReturnsAgentName()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            ProfileId = null
        };

        // Act
        var name = _connector.GetEntityName(agent);

        // Assert
        name.ShouldBe("Test Agent");
    }

    [Fact]
    public void UdiEntityType_ReturnsCorrectType()
    {
        // Assert
        _connector.UdiEntityType.ShouldBe("umbraco-ai-agent");
    }
}
