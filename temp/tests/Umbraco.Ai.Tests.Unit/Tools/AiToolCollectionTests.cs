using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Tools;

public class AiToolCollectionTests
{
    private readonly AiToolCollection _collection;
    private readonly FakeTool _tool1;
    private readonly FakeTool _tool2;
    private readonly FakeTool _tool3;
    private readonly FakeTool _destructiveTool;

    public AiToolCollectionTests()
    {
        _tool1 = new FakeTool("tool-1", "Tool One", category: "CategoryA", tags: ["tag1", "common"]);
        _tool2 = new FakeTool("tool-2", "Tool Two", category: "CategoryA", tags: ["tag2", "common"]);
        _tool3 = new FakeTool("tool-3", "Tool Three", category: "CategoryB", tags: ["tag3"]);
        _destructiveTool = new FakeTool("destructive-tool", "Destructive Tool", category: "CategoryB", isDestructive: true);

        _collection = new AiToolCollection(() => new IAiTool[] { _tool1, _tool2, _tool3, _destructiveTool });
    }

    #region GetById

    [Fact]
    public void GetById_WithExistingId_ReturnsTool()
    {
        // Act
        var result = _collection.GetById("tool-1");

        // Assert
        result.ShouldBe(_tool1);
    }

    [Fact]
    public void GetById_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = _collection.GetById("non-existent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        // Act
        var result = _collection.GetById("TOOL-1");

        // Assert
        result.ShouldBe(_tool1);
    }

    #endregion

    #region GetByCategory

    [Fact]
    public void GetByCategory_WithExistingCategory_ReturnsMatchingTools()
    {
        // Act
        var results = _collection.GetByCategory("CategoryA").ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(_tool1);
        results.ShouldContain(_tool2);
    }

    [Fact]
    public void GetByCategory_WithNonExistingCategory_ReturnsEmpty()
    {
        // Act
        var results = _collection.GetByCategory("NonExistent").ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetByCategory_IsCaseInsensitive()
    {
        // Act
        var results = _collection.GetByCategory("categorya").ToList();

        // Assert
        results.Count.ShouldBe(2);
    }

    #endregion

    #region GetWithTag

    [Fact]
    public void GetWithTag_WithExistingTag_ReturnsMatchingTools()
    {
        // Act
        var results = _collection.GetWithTag("common").ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(_tool1);
        results.ShouldContain(_tool2);
    }

    [Fact]
    public void GetWithTag_WithNonExistingTag_ReturnsEmpty()
    {
        // Act
        var results = _collection.GetWithTag("nonexistent").ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetWithTag_IsCaseInsensitive()
    {
        // Act
        var results = _collection.GetWithTag("TAG1").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results.ShouldContain(_tool1);
    }

    #endregion

    #region GetDestructive / GetNonDestructive

    [Fact]
    public void GetDestructive_ReturnsOnlyDestructiveTools()
    {
        // Act
        var results = _collection.GetDestructive().ToList();

        // Assert
        results.Count.ShouldBe(1);
        results.ShouldContain(_destructiveTool);
    }

    [Fact]
    public void GetNonDestructive_ReturnsOnlyNonDestructiveTools()
    {
        // Act
        var results = _collection.GetNonDestructive().ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldNotContain(_destructiveTool);
    }

    #endregion

    #region Enumeration

    [Fact]
    public void Collection_IsEnumerable()
    {
        // Act
        var count = _collection.Count();

        // Assert
        count.ShouldBe(4);
    }

    #endregion

    #region GetSystemTools / GetUserTools

    [Fact]
    public void GetSystemTools_ReturnsOnlyToolsImplementingIAiSystemTool()
    {
        // Arrange
        var systemTool1 = new FakeSystemTool("system-1", "System Tool One");
        var systemTool2 = new FakeSystemTool("system-2", "System Tool Two");
        var userTool = new FakeTool("user-1", "User Tool");
        var collection = new AiToolCollection(() => [systemTool1, systemTool2, userTool]);

        // Act
        var results = collection.GetSystemTools().ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(systemTool1);
        results.ShouldContain(systemTool2);
        results.ShouldNotContain(userTool);
    }

    [Fact]
    public void GetUserTools_ReturnsOnlyToolsNotImplementingIAiSystemTool()
    {
        // Arrange
        var systemTool = new FakeSystemTool("system-1", "System Tool");
        var userTool1 = new FakeTool("user-1", "User Tool One");
        var userTool2 = new FakeTool("user-2", "User Tool Two");
        var collection = new AiToolCollection(() => [systemTool, userTool1, userTool2]);

        // Act
        var results = collection.GetUserTools().ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(userTool1);
        results.ShouldContain(userTool2);
        results.ShouldNotContain(systemTool);
    }

    [Fact]
    public void GetSystemTools_WithNoSystemTools_ReturnsEmpty()
    {
        // Arrange - collection only has regular FakeTools (not system tools)

        // Act
        var results = _collection.GetSystemTools().ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetUserTools_WithNoUserTools_ReturnsEmpty()
    {
        // Arrange
        var systemTool1 = new FakeSystemTool("system-1", "System Tool One");
        var systemTool2 = new FakeSystemTool("system-2", "System Tool Two");
        var collection = new AiToolCollection(() => [systemTool1, systemTool2]);

        // Act
        var results = collection.GetUserTools().ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetUserTools_WithMixedTools_ExcludesSystemTools()
    {
        // Arrange - use the default collection which has only user tools
        // and add system tools to test mixed scenario
        var systemTool = new FakeSystemTool("system-1", "System Tool");
        var collection = new AiToolCollection(() => [_tool1, _tool2, systemTool]);

        // Act
        var systemResults = collection.GetSystemTools().ToList();
        var userResults = collection.GetUserTools().ToList();

        // Assert
        systemResults.Count.ShouldBe(1);
        systemResults.ShouldContain(systemTool);

        userResults.Count.ShouldBe(2);
        userResults.ShouldContain(_tool1);
        userResults.ShouldContain(_tool2);
    }

    #endregion
}
