using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Prompt.Deploy.Connectors.ServiceConnectors;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core;
using Xunit;

namespace Umbraco.AI.Prompt.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIPromptServiceConnectorTests
{
    private readonly Mock<IAIPromptService> _promptServiceMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIPromptServiceConnector _connector;

    public UmbracoAIPromptServiceConnectorTests()
    {
        _promptServiceMock = new Mock<IAIPromptService>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        _connector = new UmbracoAIPromptServiceConnector(
            _promptServiceMock.Object,
            _profileServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    [Fact]
    public async Task GetArtifactAsync_WithProfileDependency_CreatesArtifactWithProfileUdi()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var prompt = new AIPrompt
        {
            Alias = "test-prompt",
            Name = "Test Prompt",
            Description = "Test description",
            Instructions = "Test instructions",
            ProfileId = profileId,
            ContextIds = [Guid.NewGuid(), Guid.NewGuid()],
            Tags = ["test", "prompt"],
            IsActive = true,
            IncludeEntityContext = true,
            OptionCount = 3,
            Scope = new AIPromptScope
            {
                AllowRules = [
                    new AIPromptScopeRule
                    {
                        ContentTypeAliases = ["article"]
                    }
                ]
            }
        };

        var udi = new GuidUdi("umbraco-ai-prompt", prompt.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, prompt);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Alias.ShouldBe("test-prompt");
        artifact.Name.ShouldBe("Test Prompt");
        artifact.Description.ShouldBe("Test description");
        artifact.Instructions.ShouldBe("Test instructions");
        artifact.Tags.ShouldBe(new[] { "test", "prompt" });
        artifact.IsActive.ShouldBeTrue();
        artifact.IncludeEntityContext.ShouldBeTrue();
        artifact.OptionCount.ShouldBe(3);

        // Profile dependency should be added
        artifact.ProfileUdi.ShouldNotBeNull();
        artifact.ProfileUdi.Guid.ShouldBe(profileId);
        artifact.ProfileUdi.EntityType.ShouldBe("umbraco-ai-profile");

        // Dependency should be in the dependencies collection
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == "umbraco-ai-profile" &&
            ((GuidUdi)d.Udi).Guid == profileId);

        // ContextIds should be preserved
        artifact.ContextIds.Count().ShouldBe(2);

        // Scope should be serialized as JSON
        artifact.Scope.ShouldNotBeNull();
        var scope = JsonSerializer.Deserialize<Dictionary<string, object>>(artifact.Scope.Value);
        scope.ShouldNotBeNull();
        scope.ShouldContainKey("AllowRules");
    }

    [Fact]
    public async Task GetArtifactAsync_WithoutProfile_CreatesArtifactWithNullProfileUdi()
    {
        // Arrange
        var prompt = new AIPrompt
        {
            Alias = "test-prompt",
            Name = "Test Prompt",
            Instructions = "Test instructions",
            ProfileId = null,
            ContextIds = [],
            Tags = [],
            IsActive = true,
            IncludeEntityContext = false,
            OptionCount = 1,
            Scope = null
        };

        var udi = new GuidUdi("umbraco-ai-prompt", prompt.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, prompt);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.ProfileUdi.ShouldBeNull();
        artifact.Dependencies.ShouldNotContain(d => d.Udi.EntityType == "umbraco-ai-profile");
        artifact.Scope.ShouldBeNull();
        artifact.ContextIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullPrompt_ReturnsNull()
    {
        // Arrange
        var udi = new GuidUdi("umbraco-ai-prompt", Guid.NewGuid());

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, null);

        // Assert
        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsPrompt()
    {
        // Arrange
        var promptId = Guid.NewGuid();
        var prompt = new AIPrompt
        {
            Alias = "test-prompt",
            Name = "Test Prompt",
            Instructions = "Test instructions",
            ProfileId = null
        };

        _promptServiceMock
            .Setup(x => x.GetPromptAsync(promptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        // Act
        var result = await _connector.GetEntityAsync(promptId);

        // Assert
        result.ShouldBe(prompt);
    }

    [Fact]
    public void GetEntityName_ReturnsPromptName()
    {
        // Arrange
        var prompt = new AIPrompt
        {
            Alias = "test-prompt",
            Name = "Test Prompt",
            Instructions = "Test instructions",
            ProfileId = null
        };

        // Act
        var name = _connector.GetEntityName(prompt);

        // Assert
        name.ShouldBe("Test Prompt");
    }

    [Fact]
    public void UdiEntityType_ReturnsCorrectType()
    {
        // Assert
        _connector.UdiEntityType.ShouldBe("umbraco-ai-prompt");
    }
}
