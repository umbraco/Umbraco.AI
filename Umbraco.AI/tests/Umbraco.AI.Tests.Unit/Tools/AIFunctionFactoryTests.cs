using Moq;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Tests.Common.Fakes;
using MeaiAIFunctionArguments = Microsoft.Extensions.AI.AIFunctionArguments;

namespace Umbraco.AI.Tests.Unit.Tools;

public class AIFunctionFactoryTests
{
    private readonly AIFunctionFactory _factory;
    private readonly List<IAIToolScope> _scopes;
    private readonly AIToolScopeCollection _scopeCollection;

    public AIFunctionFactoryTests()
    {
        _scopes = new List<IAIToolScope>();
        _scopeCollection = new AIToolScopeCollection(() => _scopes);
        _factory = new AIFunctionFactory(_scopeCollection);
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
    public void Create_WithUntypedTool_ExposesEmptyObjectSchemaWithoutArgsWrapper()
    {
        // Arrange
        // Regression test: MEAI's reflection-based AIFunctionFactory.Create(Delegate), given
        // IAITool.ExecuteAsync's `object? args` parameter, infers the JSON Schema boolean `true`
        // for it (no shape to infer from `object`) and marks it required — a schema of
        // {"properties":{"args":true},"required":["args"]}. OpenAI and Anthropic tolerate the
        // stray boolean-schema property; Moonshot's stricter validator rejects it outright
        // ("property schema for 'args' must be an object"), failing every request for an agent
        // with any no-argument tool available. A no-argument tool should describe no parameters.
        var tool = new FakeTool(id: "my-tool");

        // Act
        var function = _factory.Create(tool);
        var schema = function.JsonSchema;

        // Assert
        schema.ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Object);

        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("args", out _).ShouldBeFalse(
            "Untyped tool schema must not expose a boolean-schema 'args' property; Moonshot rejects it.");
        properties.EnumerateObject().Any().ShouldBeFalse("A no-argument tool should describe no properties.");

        schema.TryGetProperty("required", out _).ShouldBeFalse(
            "A no-argument tool schema must not declare any required properties.");
    }

    [Fact]
    public async Task Create_WithUntypedTool_InvokesToolWithNullArgs()
    {
        // Arrange
        object? capturedArgs = "not-null";
        var tool = new FakeTool(id: "my-tool")
        {
            ExecuteHandler = (args, _) =>
            {
                capturedArgs = args;
                return Task.FromResult<object>(new { });
            },
        };
        var function = _factory.Create(tool);

        // Act
        await function.InvokeAsync(new MeaiAIFunctionArguments(), CancellationToken.None);

        // Assert
        capturedArgs.ShouldBeNull();
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
    public void Create_WithTypedTool_ExposesTArgsPropertiesAsTopLevelSchemaParameters()
    {
        // Arrange
        // Regression test: Google Gemini's function calling does not populate nested object
        // parameters. Previously the factory wrapped TArgs under a single "args" property,
        // causing Gemini to emit {"args": null} and every typed tool call to fail. The
        // schema must expose TArgs properties at the top level for all providers.
        var tool = new FakeTypedTool<FakeToolArgs>(id: "typed-tool");

        // Act
        var function = _factory.Create(tool);
        var schema = function.JsonSchema;

        // Assert
        schema.ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Object);

        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("args", out _).ShouldBeFalse(
            "Schema must not wrap TArgs under a nested 'args' property; Gemini does not populate nested objects.");

        // TArgs properties (camelCased) must appear at the top level of the schema.
        properties.TryGetProperty("message", out _).ShouldBeTrue();
        properties.TryGetProperty("count", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WithTypedTool_BindsFlatArgumentsIntoTArgs()
    {
        // Arrange
        FakeToolArgs? capturedArgs = null;
        var tool = new FakeTypedTool<FakeToolArgs>(id: "typed-tool")
            .WithExecuteHandler((args, _) =>
            {
                capturedArgs = args;
                return Task.FromResult<object>(args);
            });
        var function = _factory.Create(tool);

        // Act — invoke with args flattened at the top level, as any conforming provider
        // (Gemini, OpenAI, Anthropic) would emit given the flattened schema.
        var arguments = new MeaiAIFunctionArguments
        {
            ["message"] = "hello",
            ["count"] = 3,
        };
        await function.InvokeAsync(arguments, CancellationToken.None);

        // Assert
        capturedArgs.ShouldNotBeNull();
        capturedArgs!.Message.ShouldBe("hello");
        capturedArgs.Count.ShouldBe(3);
    }

    [Fact]
    public void Create_WithToolScopeMetadata_EnrichesDescriptionWithForEntityTypes()
    {
        // Arrange
        var scopeMock = new Mock<IAIToolScope>();
        scopeMock.Setup(s => s.Id).Returns("content-read");
        scopeMock.Setup(s => s.ForEntityTypes).Returns(new List<string> { "document", "documentType" });

        _scopes.Add(scopeMock.Object);

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

        _scopes.Add(scopeMock.Object);

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
