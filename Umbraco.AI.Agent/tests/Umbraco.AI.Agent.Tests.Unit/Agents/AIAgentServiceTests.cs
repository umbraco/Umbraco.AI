using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentServiceTests
{
    private readonly Mock<IAIAgentRepository> _repositoryMock;
    private readonly AIToolCollection _toolCollection;
    private readonly AIAgentService _service;

    public AIAgentServiceTests()
    {
        _repositoryMock = new Mock<IAIAgentRepository>();

        // Create fake tools with scopes
        var tools = new[]
        {
            CreateTool("tool1", scopeId: "content-read"),
            CreateTool("tool2", scopeId: "content-write"),
            CreateTool("tool3", scopeId: "search"),
            CreateTool("specific-tool", scopeId: null),
        };
        _toolCollection = new AIToolCollection(() => tools);

        _service = new AIAgentService(
            _repositoryMock.Object,
            null!, // IAIEntityVersionService
            null!, // IAIAgentFactory
            null!, // IAGUIStreamingService
            null!, // IAGUIContextConverter
            _toolCollection,
            null  // IBackOfficeSecurityAccessor
        );
    }

    #region GetEnabledToolIdsAsync

    [Fact]
    public async Task GetEnabledToolIdsAsync_WithAgent_ReturnsEnabledToolIds()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolIds: ["tool1", "tool2"]);

        // Act
        var result = await _service.GetEnabledToolIdsAsync(agent);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("tool1");
        result.ShouldContain("tool2");
    }

    [Fact]
    public async Task GetEnabledToolIdsAsync_WithAgentHavingNoTools_ReturnsEmptyList()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolIds: []);

        // Act
        var result = await _service.GetEnabledToolIdsAsync(agent);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetEnabledToolIdsAsync_WithEnabledScopes_ReturnsToolsInThoseScopes()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolScopeIds: ["content-read", "search"]);

        // Act
        var result = await _service.GetEnabledToolIdsAsync(agent);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("tool1"); // content-read scope
        result.ShouldContain("tool3"); // search scope
    }

    #endregion

    #region IsToolEnabledAsync

    [Fact]
    public async Task IsToolEnabledAsync_ToolInEnabledToolIds_ReturnsTrue()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolIds: ["tool1", "another-tool"]);

        // Act
        var result = await _service.IsToolEnabledAsync(agent, "tool1");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsToolEnabledAsync_ToolWithScopeInEnabledScopes_ReturnsTrue()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolScopeIds: ["content-read", "search"]);

        // Act
        var result = await _service.IsToolEnabledAsync(agent, "tool1"); // tool1 has content-read scope

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsToolEnabledAsync_ToolNotInAnyEnabledList_ReturnsFalse()
    {
        // Arrange
        var agent = CreateAgent(
            Guid.NewGuid(),
            enabledToolIds: ["other-tool"],
            enabledToolScopeIds: ["other-scope"]
        );

        // Act
        var result = await _service.IsToolEnabledAsync(agent, "tool1");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsToolEnabledAsync_ToolNotFound_ReturnsFalse()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid(), enabledToolIds: ["tool1"]);

        // Act
        var result = await _service.IsToolEnabledAsync(agent, "non-existent-tool");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsToolEnabledAsync_CaseInsensitiveComparison()
    {
        // Arrange
        var agent = CreateAgent(
            Guid.NewGuid(),
            enabledToolIds: ["Tool1"],
            enabledToolScopeIds: ["Content-Read"]
        );

        // Act
        var result1 = await _service.IsToolEnabledAsync(agent, "TOOL1");
        var result2 = await _service.IsToolEnabledAsync(agent, "tool3"); // tool3 has search scope, but only content-read is enabled

        // Assert
        result1.ShouldBeTrue(); // Direct match via enabled tool IDs
        result2.ShouldBeFalse(); // Not in enabled scopes
    }

    #endregion

    #region Helper Methods

    private static AIAgent CreateAgent(
        Guid id,
        IReadOnlyList<string>? enabledToolIds = null,
        IReadOnlyList<string>? enabledToolScopeIds = null)
    {
        return new AIAgent
        {
            Id = id,
            Alias = "test-agent",
            Name = "Test Agent",
            EnabledToolIds = enabledToolIds ?? [],
            EnabledToolScopeIds = enabledToolScopeIds ?? [],
            IsActive = true
        };
    }

    private static IAITool CreateTool(string id, string? scopeId = null)
    {
        var mock = new Mock<IAITool>();
        mock.Setup(x => x.Id).Returns(id);
        mock.Setup(x => x.Name).Returns($"Tool {id}");
        mock.Setup(x => x.Description).Returns($"Description for {id}");
        mock.Setup(x => x.ScopeId).Returns(scopeId);
        mock.Setup(x => x.IsDestructive).Returns(false);
        mock.Setup(x => x.Tags).Returns(Array.Empty<string>());
        mock.Setup(x => x.ArgsType).Returns((Type?)null);
        return mock.Object;
    }

    #endregion
}
