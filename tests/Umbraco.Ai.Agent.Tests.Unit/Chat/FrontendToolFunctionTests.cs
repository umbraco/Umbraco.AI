using System.Text.Json;
using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agui.Models;
using Xunit;

namespace Umbraco.Ai.Agent.Tests.Unit.Chat;

public class FrontendToolFunctionTests
{
    [Fact]
    public void Constructor_WithAguiTool_SetsNameAndDescription()
    {
        // Arrange
        var tool = new AguiTool
        {
            Name = "test-tool",
            Description = "A test tool for testing",
            Parameters = new AguiToolParameters
            {
                Type = "object",
                Properties = JsonSerializer.SerializeToElement(new
                {
                    param1 = new { type = "string" }
                })
            }
        };

        // Act
        var function = new FrontendToolFunction(tool);

        // Assert
        function.Name.ShouldBe("test-tool");
        function.Description.ShouldBe("A test tool for testing");
    }

    [Fact]
    public void Constructor_WithAguiTool_BuildsJsonSchema()
    {
        // Arrange
        var tool = new AguiTool
        {
            Name = "test-tool",
            Description = "A test tool",
            Parameters = new AguiToolParameters
            {
                Type = "object",
                Properties = JsonSerializer.SerializeToElement(new
                {
                    name = new { type = "string", description = "The name" },
                    count = new { type = "integer" }
                }),
                Required = ["name"]
            }
        };

        // Act
        var function = new FrontendToolFunction(tool);

        // Assert
        function.JsonSchema.TryGetProperty("type", out var typeElement).ShouldBeTrue();
        typeElement.GetString().ShouldBe("object");

        function.JsonSchema.TryGetProperty("properties", out var propsElement).ShouldBeTrue();
        propsElement.ValueKind.ShouldBe(JsonValueKind.Object);

        function.JsonSchema.TryGetProperty("required", out var requiredElement).ShouldBeTrue();
        requiredElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public void Constructor_WithExplicitParameters_SetsProperties()
    {
        // Arrange
        var schema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { message = new { type = "string" } }
        });

        // Act
        var function = new FrontendToolFunction("my-tool", "My description", schema);

        // Assert
        function.Name.ShouldBe("my-tool");
        function.Description.ShouldBe("My description");
        function.JsonSchema.GetProperty("type").GetString().ShouldBe("object");
    }

    [Fact]
    public void Constructor_WithNullDescription_SetsEmptyDescription()
    {
        // Arrange
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });

        // Act
        var function = new FrontendToolFunction("my-tool", null!, schema);

        // Assert
        function.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_WithNullTool_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FrontendToolFunction(null!));
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FrontendToolFunction(null!, "desc", schema));
    }

    [Fact]
    public async Task InvokeAsync_ReturnsNull()
    {
        // Arrange
        var tool = new AguiTool
        {
            Name = "test-tool",
            Description = "A test tool",
            Parameters = new AguiToolParameters
            {
                Type = "object",
                Properties = JsonSerializer.SerializeToElement(new { param1 = new { type = "string" } })
            }
        };
        var function = new FrontendToolFunction(tool);
        var arguments = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["param1"] = "value1"
        });

        // Act
        var result = await function.InvokeAsync(arguments);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNoRequiredParams_OmitsRequiredFromSchema()
    {
        // Arrange
        var tool = new AguiTool
        {
            Name = "test-tool",
            Description = "A test tool",
            Parameters = new AguiToolParameters
            {
                Type = "object",
                Properties = JsonSerializer.SerializeToElement(new
                {
                    optional = new { type = "string" }
                }),
                Required = null
            }
        };

        // Act
        var function = new FrontendToolFunction(tool);

        // Assert
        function.JsonSchema.TryGetProperty("required", out _).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyRequiredParams_OmitsRequiredFromSchema()
    {
        // Arrange
        var tool = new AguiTool
        {
            Name = "test-tool",
            Description = "A test tool",
            Parameters = new AguiToolParameters
            {
                Type = "object",
                Properties = JsonSerializer.SerializeToElement(new
                {
                    optional = new { type = "string" }
                }),
                Required = []
            }
        };

        // Act
        var function = new FrontendToolFunction(tool);

        // Assert
        function.JsonSchema.TryGetProperty("required", out _).ShouldBeFalse();
    }
}
