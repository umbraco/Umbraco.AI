using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Deploy.Artifacts;
using Umbraco.AI.Prompt.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using Xunit;

namespace Umbraco.AI.Prompt.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIPromptServiceConnectorTests
{
    private readonly Mock<IAIPromptUnrestrictedWriter> _promptServiceMock;
    private readonly Mock<IAIPromptUnrestrictedReader> _promptReaderMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<IUserGroupService> _userGroupServiceMock;
    private readonly Mock<UmbracoAIDeploySettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAIPromptServiceConnector _connector;

    public UmbracoAIPromptServiceConnectorTests()
    {
        _promptServiceMock = new Mock<IAIPromptUnrestrictedWriter>();
        _promptReaderMock = new Mock<IAIPromptUnrestrictedReader>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _userGroupServiceMock = new Mock<IUserGroupService>();
        _settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);

        _settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        _connector = new UmbracoAIPromptServiceConnector(
            _profileServiceMock.Object,
            _settingsAccessorMock.Object,
            _userGroupServiceMock.Object,
            _promptReaderMock.Object,
            _promptServiceMock.Object);
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
    public async Task GetArtifactAsync_WithContexts_AddsContextDependencies()
    {
        // Arrange
        var contextIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var prompt = new AIPrompt
        {
            Alias = "test-prompt",
            Name = "Test Prompt",
            Instructions = "Test instructions",
            ContextIds = contextIds,
        };

        var udi = new GuidUdi("umbraco-ai-prompt", prompt.Id);

        // Act
        var artifact = await _connector.GetArtifactAsync(udi, prompt);

        // Assert
        artifact.ShouldNotBeNull();
        artifact.Dependencies
            .Where(d => d.Udi.EntityType == "umbraco-ai-context")
            .Select(d => ((GuidUdi)d.Udi).Guid)
            .ShouldBe(contextIds, ignoreOrder: true);
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

        _promptReaderMock
            .Setup(x => x.GetPromptUnrestrictedAsync(promptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        // Act
        var result = await _connector.GetEntityAsync(promptId);

        // Assert
        result.ShouldBe(prompt);
    }

    [Fact]
    public async Task GetEntitiesAsync_ReadsThroughTheUnrestrictedReader_SoALimitedPromptIsStillExported()
    {
        // Arrange: Deploy has no signed-in user, so IAIPromptService would hide a limited prompt
        var limited = new AIPrompt
        {
            Alias = "limited",
            Name = "Limited",
            Instructions = "Test instructions",
            AllowedUserGroupIds = [Guid.NewGuid()]
        };
        _promptReaderMock
            .Setup(x => x.GetPromptsUnrestrictedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([limited]);

        // Act
        var exported = new List<AIPrompt>();
        await foreach (var prompt in _connector.GetEntitiesAsync())
        {
            exported.Add(prompt);
        }

        // Assert
        exported.ShouldBe([limited]);
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

    [Fact]
    public async Task GetArtifactAsync_WithAllowedUserGroups_ExportsTheirAliases()
    {
        var group = Mock.Of<IUserGroup>(g => g.Alias == "translators");
        var groupKey = Guid.NewGuid();
        _userGroupServiceMock.Setup(x => x.GetAsync(groupKey)).ReturnsAsync(group);
        var prompt = new AIPrompt { Alias = "p", Name = "P", Instructions = "i", AllowedUserGroupIds = [groupKey] };

        var artifact = await _connector.GetArtifactAsync(new GuidUdi("umbraco-ai-prompt", prompt.Id), prompt);

        artifact.ShouldNotBeNull();
        artifact.AllowedUserGroups.ShouldBe(["translators"]);
    }

    [Fact]
    public async Task GetArtifactAsync_WithNoAllowedUserGroups_ExportsAnEmptyList()
    {
        var prompt = new AIPrompt { Alias = "p", Name = "P", Instructions = "i" };

        var artifact = await _connector.GetArtifactAsync(new GuidUdi("umbraco-ai-prompt", prompt.Id), prompt);

        artifact.ShouldNotBeNull();
        artifact.AllowedUserGroups.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithAllowedUserGroupAlias_SavesTheTargetsKey()
    {
        var targetKey = Guid.NewGuid();
        _userGroupServiceMock.Setup(x => x.GetAsync("translators"))
            .ReturnsAsync(Mock.Of<IUserGroup>(g => g.Key == targetKey));
        var saved = CaptureSave();
        var state = NewState(["translators"]);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        saved().ShouldNotBeNull().AllowedUserGroupIds.ShouldBe([targetKey]);
    }

    [Fact]
    public async Task ProcessAsync_WithAllowedUserGroupKey_SavesTheTargetsKey()
    {
        var sourceKey = Guid.NewGuid();
        var targetKey = Guid.NewGuid();
        _userGroupServiceMock.Setup(x => x.GetAsync(sourceKey))
            .ReturnsAsync(Mock.Of<IUserGroup>(g => g.Key == targetKey));
        var saved = CaptureSave();
        var state = NewState([sourceKey.ToString()]);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        saved().ShouldNotBeNull().AllowedUserGroupIds.ShouldBe([targetKey]);
    }

    [Fact]
    public async Task ProcessAsync_ThatChangesTheGroups_SavesThroughTheUnrestrictedWriter()
    {
        // The user group rule must not depend on who started the import, so the connector never uses the
        // per-user save.
        var targetKey = Guid.NewGuid();
        _userGroupServiceMock.Setup(x => x.GetAsync("translators"))
            .ReturnsAsync(Mock.Of<IUserGroup>(g => g.Key == targetKey));
        CaptureSave();

        await _connector.ProcessAsync(NewState(["translators"]), Mock.Of<IDeployContext>(), 3);

        _promptServiceMock.Verify(x => x.SavePromptUnrestrictedAsync(It.IsAny<AIPrompt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithNoAllowedUserGroups_SavesAnEmptyList()
    {
        var saved = CaptureSave();
        var state = NewState([]);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        saved().ShouldNotBeNull().AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithMissingUserGroup_FailsNamingTheAliasAndSavesNothing()
    {
        _userGroupServiceMock.Setup(x => x.GetAsync("translators")).ReturnsAsync((IUserGroup?)null);
        var saved = CaptureSave();
        var state = NewState(["translators"]);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3));

        ex.Message.ShouldContain("translators");
        saved().ShouldBeNull();
    }

    private Func<AIPrompt?> CaptureSave()
    {
        AIPrompt? saved = null;
        _promptServiceMock
            .Setup(x => x.SavePromptUnrestrictedAsync(It.IsAny<AIPrompt>(), It.IsAny<CancellationToken>()))
            .Callback<AIPrompt, CancellationToken>((p, _) => saved = p)
            .ReturnsAsync((AIPrompt p, CancellationToken _) => p);
        return () => saved;
    }

    private ArtifactDeployState<AIPromptArtifact, AIPrompt> NewState(string[] allowedUserGroups)
    {
        var artifact = new AIPromptArtifact(new GuidUdi("umbraco-ai-prompt", Guid.NewGuid()))
        {
            Alias = "p",
            Name = "P",
            Instructions = "i",
            AllowedUserGroups = allowedUserGroups,
        };
        return new ArtifactDeployState<AIPromptArtifact, AIPrompt>(artifact, null, _connector, 3);
    }
}
