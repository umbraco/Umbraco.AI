using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Tests.Unit.Formatters;

public class AIGenericEntityFormatterTests
{
    private readonly AIGenericEntityFormatter _formatter = new();

    [Fact]
    public void EntityType_ReturnsNull()
    {
        // Assert - generic formatter is the default (null entity type)
        _formatter.EntityType.ShouldBeNull();
    }

    [Fact]
    public void Format_WithSimpleJsonData_FormatsAsJson()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "sku": "12345",
                "price": 29.99,
                "inStock": true
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "product",
            Unique = "prod-123",
            Name = "Widget",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldContain("## Current Entity Context");
        result.ShouldContain("Key: `prod-123`");
        result.ShouldContain("Name: `Widget`");
        result.ShouldContain("Type: `product`");
        result.ShouldContain("### Entity Data");
        result.ShouldContain("```json");
        result.ShouldContain("\"sku\": \"12345\"");
        result.ShouldContain("\"price\": 29.99");
        result.ShouldContain("\"inStock\": true");
    }

    [Fact]
    public void Format_WithNestedJsonData_FormatsWithIndentation()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "category": "electronics",
                "variants": [
                    { "color": "red", "size": "large" },
                    { "color": "blue", "size": "small" }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "product",
            Unique = "prod-456",
            Name = "Gadget",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldContain("```json");
        result.ShouldContain("\"category\": \"electronics\"");
        result.ShouldContain("\"variants\":");
        result.ShouldContain("\"color\": \"red\"");
        result.ShouldContain("\"size\": \"large\"");
    }

    [Fact]
    public void Format_WithEmptyObject_FormatsCorrectly()
    {
        // Arrange
        var data = JsonDocument.Parse("{}").RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "empty",
            Unique = "empty-1",
            Name = "Empty Entity",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldContain("## Current Entity Context");
        result.ShouldContain("```json");
        result.ShouldContain("{}");
    }

    [Fact]
    public void Format_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _formatter.Format(null!));
    }
}
