using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.Ai.Prompt.Core.Templates.Processors;
using Xunit;

namespace Umbraco.Ai.Prompt.Tests.Unit.Templates;

public class TextTemplateVariableProcessorTests
{
    private readonly TextTemplateVariableProcessor _processor = new();

    [Fact]
    public void Prefix_ReturnsDefaultIndicator()
    {
        // Act
        var result = _processor.Prefix;

        // Assert
        result.ShouldBe("*");
    }

    [Fact]
    public void Process_WithSimplePath_ReturnsTextContent()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["name"] = "Test Value"
        };

        // Act
        var result = _processor.Process("name", context).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Test Value");
    }

    [Fact]
    public void Process_WithNestedPath_ReturnsTextContent()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["entity"] = new Dictionary<string, object?>
            {
                ["name"] = "Nested Value"
            }
        };

        // Act
        var result = _processor.Process("entity.name", context).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Nested Value");
    }

    [Fact]
    public void Process_WithMissingPath_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>();

        // Act
        var result = _processor.Process("missing", context).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Process_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["nullValue"] = null
        };

        // Act
        var result = _processor.Process("nullValue", context).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Process_CaseInsensitive_ReturnsTextContent()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["Name"] = "Case Test"
        };

        // Act
        var result = _processor.Process("name", context).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Case Test");
    }

    [Fact]
    public void Process_WithIntegerValue_ConvertsToString()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["count"] = 42
        };

        // Act
        var result = _processor.Process("count", context).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("42");
    }

    [Fact]
    public void Process_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new Dictionary<string, object?>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _processor.Process(null!, context).ToList());
    }

    [Fact]
    public void Process_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _processor.Process("path", null!).ToList());
    }
}
