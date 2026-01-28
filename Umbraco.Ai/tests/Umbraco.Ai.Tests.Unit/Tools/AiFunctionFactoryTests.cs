using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Tools;

public class AiFunctionFactoryTests
{
    private readonly AiFunctionFactory _factory;

    public AiFunctionFactoryTests()
    {
        _factory = new AiFunctionFactory();
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

    #endregion

    #region Create (multiple tools)

    [Fact]
    public void Create_WithMultipleTools_ReturnsListOfAIFunctions()
    {
        // Arrange
        var tools = new IAiTool[]
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
        var tools = Array.Empty<IAiTool>();

        // Act
        var functions = _factory.Create(tools);

        // Assert
        functions.ShouldBeEmpty();
    }

    #endregion
}
