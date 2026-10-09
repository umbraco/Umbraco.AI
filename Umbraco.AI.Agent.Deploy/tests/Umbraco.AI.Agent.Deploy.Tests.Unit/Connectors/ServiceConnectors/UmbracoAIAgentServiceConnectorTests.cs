using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Deploy.Artifacts;
using Umbraco.AI.Agent.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
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
            StarterPrompts = [new AIStarterPrompt { Prompt = "Summarize this page" }],
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

        // Starter prompts
        artifact.StarterPrompts.Select(p => p.Prompt).ShouldBe(["Summarize this page"]);

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

    [Fact]
    public async Task ProcessAsync_WithStarterPrompts_RoundTripsThem()
    {
        // Arrange
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig(),
            StarterPrompts =
            [
                new AIStarterPrompt { Prompt = "First starter" },
                new AIStarterPrompt { Prompt = "Second starter" }
            ],
            IsActive = true
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);
        var artifact = await _connector.GetArtifactAsync(udi, agent);
        artifact.ShouldNotBeNull();

        AIAgent? savedAgent = null;
        _agentServiceMock
            .Setup(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()))
            .Callback<AIAgent, CancellationToken>((a, _) => savedAgent = a)
            .ReturnsAsync((AIAgent a, CancellationToken _) => a);

        var state = new ArtifactDeployState<AIAgentArtifact, AIAgent>(artifact, null, _connector, 3);

        // Act
        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        // Assert
        savedAgent.ShouldNotBeNull();
        savedAgent.StarterPrompts.Select(p => p.Prompt).ShouldBe(["First starter", "Second starter"]);
    }

    [Fact]
    public async Task ProcessAsync_WithMoreThanFourStarterPromptsInArtifact_ClampsToFourRatherThanThrowing()
    {
        // Arrange — an artifact from another version line, or after a future cap change, must not
        // fail the whole transfer over a presentation rule
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig(),
            IsActive = true
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);
        var artifact = await _connector.GetArtifactAsync(udi, agent);
        artifact.ShouldNotBeNull();
        artifact.StarterPrompts = Enumerable.Range(1, 5)
            .Select(i => new AIStarterPrompt { Prompt = $"Starter {i}" })
            .ToList();

        AIAgent? savedAgent = null;
        _agentServiceMock
            .Setup(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()))
            .Callback<AIAgent, CancellationToken>((a, _) => savedAgent = a)
            .ReturnsAsync((AIAgent a, CancellationToken _) => a);

        var state = new ArtifactDeployState<AIAgentArtifact, AIAgent>(artifact, null, _connector, 3);

        // Act
        await Should.NotThrowAsync(() => _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3));

        // Assert
        savedAgent.ShouldNotBeNull();
        savedAgent.StarterPrompts.Count.ShouldBe(4);
        savedAgent.StarterPrompts.Select(p => p.Prompt).ShouldBe(["Starter 1", "Starter 2", "Starter 3", "Starter 4"]);
    }

    [Fact]
    public async Task ProcessAsync_WithOverLongStarterPromptInArtifact_TruncatesRatherThanThrowing()
    {
        // Arrange — the length cap is clamped for the same reason the count cap is: leaving it to
        // SaveAgentAsync's guard would throw and fail the whole transfer
        var agent = new AIAgent
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig(),
            IsActive = true
        };

        var udi = new GuidUdi("umbraco-ai-agent", agent.Id);
        var artifact = await _connector.GetArtifactAsync(udi, agent);
        artifact.ShouldNotBeNull();
        artifact.StarterPrompts = [new AIStarterPrompt { Prompt = new string('a', 250) }];

        AIAgent? savedAgent = null;
        _agentServiceMock
            .Setup(x => x.SaveAgentAsync(It.IsAny<AIAgent>(), It.IsAny<CancellationToken>()))
            .Callback<AIAgent, CancellationToken>((a, _) => savedAgent = a)
            .ReturnsAsync((AIAgent a, CancellationToken _) => a);

        var state = new ArtifactDeployState<AIAgentArtifact, AIAgent>(artifact, null, _connector, 3);

        // Act
        await Should.NotThrowAsync(() => _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3));

        // Assert
        savedAgent.ShouldNotBeNull();
        savedAgent.StarterPrompts.Single().Prompt.Length.ShouldBe(200);
    }
}
