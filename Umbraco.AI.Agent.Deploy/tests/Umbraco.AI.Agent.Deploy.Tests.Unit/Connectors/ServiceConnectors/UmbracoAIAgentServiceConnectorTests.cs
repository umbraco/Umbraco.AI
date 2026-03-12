using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core;
using Xunit;

namespace Umbraco.AI.Agent.Deploy.Tests.Unit.Connectors.ServiceConnectors;

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

        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            Description = "Test description",
            AgentType = AIAgentType.Standard,
            ProfileId = profileId,
            Config = new AIStandardAgentConfig
            {
                ContextIds = [Guid.NewGuid()],
                Instructions = "Agent instructions",
                AllowedToolIds = ["search", "calculator"],
                AllowedToolScopeIds = ["content", "media"],
                UserGroupPermissions = new Dictionary<Guid, AIAgentUserGroupPermissions>
                {
                    { Guid.NewGuid(), new AIAgentUserGroupPermissions { AllowedToolIds = ["read", "write"] } },
                    { Guid.NewGuid(), new AIAgentUserGroupPermissions { AllowedToolIds = ["read"] } }
                },
            },
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
        artifact.AgentType.ShouldBe("Standard");
        artifact.IsActive.ShouldBeTrue();

        // Profile dependency
        artifact.ProfileUdi.ShouldNotBeNull();
        artifact.ProfileUdi.Guid.ShouldBe(profileId);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "umbraco-ai-profile" &&
            ((GuidUdi)d.Udi).Guid == profileId);

        // Arrays
        artifact.SurfaceIds.ShouldBe(new[] { "backoffice", "frontend" });

        // JSON properties
        artifact.Scope.ShouldNotBeNull();
        var scope = JsonSerializer.Deserialize<Dictionary<string, object>>(artifact.Scope.Value);
        scope.ShouldNotBeNull();
        scope.ShouldContainKey("AllowRules");

        // Config should be serialized as JSON
        artifact.Config.ShouldNotBeNull();
        var config = JsonSerializer.Deserialize<JsonElement>(artifact.Config);
        config.TryGetProperty("instructions", out var instructions).ShouldBeTrue();
        instructions.GetString().ShouldBe("Agent instructions");
    }

    [Fact]
    public async Task GetArtifactAsync_WithoutOptionalProperties_CreatesMinimalArtifact()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            ProfileId = null,
            Config = null,
            SurfaceIds = [],
            Scope = null,
            IsActive = false
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, agent);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.ProfileUdi.ShouldBeNull();
        artifact.Scope.ShouldBeNull();
        artifact.Config.ShouldBeNull();
        artifact.AgentType.ShouldBe("Standard");
        artifact.IsActive.ShouldBeFalse();

        // Should not have profile dependencies
        artifact.Dependencies.ShouldNotContain(d => d.Udi.EntityType == "umbraco-ai-profile");
    }

    [Fact]
    public async Task GetArtifactAsync_WithOrchestratedAgent_SerializesAgentType()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-orchestration",
            Name = "Test Orchestration",
            AgentType = AIAgentType.Orchestrated,
            ProfileId = null,
            Config = new AIOrchestratedAgentConfig(),
            SurfaceIds = ["backoffice"],
            IsActive = true
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, agent);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.AgentType.ShouldBe("Orchestrated");
        artifact.Config.ShouldNotBeNull();
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
