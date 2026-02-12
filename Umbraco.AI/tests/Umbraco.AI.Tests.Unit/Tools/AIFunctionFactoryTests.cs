using Moq;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Tools;

public class AIFunctionFactoryTests
{
    private readonly AIFunctionFactory _factory;
    private readonly Mock<AIToolScopeCollection> _scopeCollectionMock;

    public AIFunctionFactoryTests()
    {
        _scopeCollectionMock = new Mock<AIToolScopeCollection>(Array.Empty<IAIToolScope>());
        _factory = new AIFunctionFactory(_scopeCollectionMock.Object);
    }

    #region Create (single tool)

    [Fact]
    public void Create_WithUntypedTool_ReturnsAIFunctionWithCorrectMetadata()
    {
        // Arrange
        var tool = new FakeTool(
            id: "my-tool",
            name: "My Tool",
            description: "Does something useful");

        // Act
        var function = _factory.Create(tool);

        // Assert
        function.ShouldNotBeNull();
        function.Name.ShouldBe("my-tool");
        function.Description.ShouldBe("Does something useful");
    }

    [Fact]
    public void Create_WithTypedTool_ReturnsAIFunctionWithCorrectMetadata()
    {
        // Arrange
        var tool = new FakeTypedTool<FakeToolArgs>(
            id: "typed-tool",
            name: "Typed Tool",
            description: "Does something with args");

        // Act
        var function = _factory.Create(tool);

        // Assert
        function.ShouldNotBeNull();
        function.Name.ShouldBe("typed-tool");
        function.Description.ShouldBe("Does something with args");
    }

    [Fact]
    public void Create_WithToolScopeMetadata_EnrichesDescriptionWithForEntityTypes()
    {
        // Arrange
        var scopeMock = new Mock<IAIToolScope>();
        scopeMock.Setup(s => s.Id).Returns("content-read");
        scopeMock.Setup(s => s.ForEntityTypes).Returns(new List<string> { "document", "documentType" });

        _scopeCollectionMock.Setup(c => c.GetById("content-read")).Returns(scopeMock.Object);

        var tool = new FakeTool(
            id: "get-content",
            name: "Get Content",
            description: "Retrieves content documents",
            scopeId: "content-read");

        // Act
        var function = _factory.Create(tool);

        // Assert
        function.ShouldNotBeNull();
        function.Name.ShouldBe("get-content");
        function.Description.ShouldContain("Retrieves content documents");
        function.Description.ShouldContain("[Suitable for entity types: document, documentType]");
    }

    [Fact]
    public void Create_WithoutForEntityTypes_DoesNotEnrichDescription()
    {
        // Arrange
        var scopeMock = new Mock<IAIToolScope>();
        scopeMock.Setup(s => s.Id).Returns("search");
        scopeMock.Setup(s => s.ForEntityTypes).Returns(new List<string>()); // Empty

        _scopeCollectionMock.Setup(c => c.GetById("search")).Returns(scopeMock.Object);

        var tool = new FakeTool(
            id: "search-all",
            name: "Search All",
            description: "Searches all content",
            scopeId: "search");

        // Act
        var function = _factory.Create(tool);

        // Assert
        function.ShouldNotBeNull();
        function.Name.ShouldBe("search-all");
        function.Description.ShouldBe("Searches all content"); // Unchanged
        function.Description.ShouldNotContain("[Suitable for");
    }

    #endregion

    #region Create (multiple tools)

    [Fact]
    public void Create_WithMultipleTools_ReturnsListOfAIFunctions()
    {
        // Arrange
        var tools = new IAITool[]
        {
            new FakeTool(id: "tool-1", name: "Tool 1", description: "First tool"),
            new FakeTool(id: "tool-2", name: "Tool 2", description: "Second tool"),
            new FakeTypedTool<FakeToolArgs>(id: "tool-3", name: "Tool 3", description: "Third tool")
        };

        // Act
        var functions = _factory.Create(tools);

        // Assert
        functions.Count.ShouldBe(3);
        functions[0].Name.ShouldBe("tool-1");
        functions[1].Name.ShouldBe("tool-2");
        functions[2].Name.ShouldBe("tool-3");
    }

    [Fact]
    public void Create_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var tools = Array.Empty<IAITool>();

        // Act
        var functions = _factory.Create(tools);

        // Assert
        functions.ShouldBeEmpty();
    }

    #endregion
}
