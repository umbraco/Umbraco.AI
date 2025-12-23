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
}
