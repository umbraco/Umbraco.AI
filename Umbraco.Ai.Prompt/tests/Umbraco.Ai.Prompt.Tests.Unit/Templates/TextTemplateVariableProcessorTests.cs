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
    public async Task ProcessAsync_WithSimplePath_ReturnsTextContent()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["name"] = "Test Value"
        };

        // Act
        var result = (await _processor.ProcessAsync("name", context)).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Test Value");
    }

    [Fact]
    public async Task ProcessAsync_WithNestedPath_ReturnsTextContent()
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
        var result = (await _processor.ProcessAsync("entity.name", context)).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Nested Value");
    }

    [Fact]
    public async Task ProcessAsync_WithMissingPath_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>();

        // Act
        var result = (await _processor.ProcessAsync("missing", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["nullValue"] = null
        };

        // Act
        var result = (await _processor.ProcessAsync("nullValue", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_CaseInsensitive_ReturnsTextContent()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["Name"] = "Case Test"
        };

        // Act
        var result = (await _processor.ProcessAsync("name", context)).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Case Test");
    }

    [Fact]
    public async Task ProcessAsync_WithIntegerValue_ConvertsToString()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["count"] = 42
        };

        // Act
        var result = (await _processor.ProcessAsync("count", context)).ToList();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("42");
    }

    [Fact]
    public async Task ProcessAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new Dictionary<string, object?>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => (await _processor.ProcessAsync(null!, context)).ToList());
    }

    [Fact]
    public async Task ProcessAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => (await _processor.ProcessAsync("path", null!)).ToList());
    }
}
