using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.EntityAdapter.Adapters;

namespace Umbraco.AI.Tests.Unit.Formatters;

public class GenericEntityAdapterTests
{
    private readonly GenericEntityAdapter _adapter = new();

    [Fact]
    public void EntityType_ReturnsNull()
    {
        // Assert - generic adapter is the default (null entity type)
        _adapter.EntityType.ShouldBeNull();
    }

    [Fact]
    public void FormatForLlm_WithSimpleJsonData_FormatsAsJson()
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
        var result = _adapter.FormatForLlm(entity);

        // Assert
        result.ShouldContain("## Entity Context");
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
    public void FormatForLlm_WithNestedJsonData_FormatsWithIndentation()
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
        var result = _adapter.FormatForLlm(entity);

        // Assert
        result.ShouldContain("```json");
        result.ShouldContain("\"category\": \"electronics\"");
        result.ShouldContain("\"variants\":");
        result.ShouldContain("\"color\": \"red\"");
        result.ShouldContain("\"size\": \"large\"");
    }

    [Fact]
    public void FormatForLlm_WithEmptyObject_FormatsCorrectly()
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
        var result = _adapter.FormatForLlm(entity);

        // Assert
        result.ShouldContain("## Entity Context");
        result.ShouldContain("```json");
        result.ShouldContain("{}");
    }

    [Fact]
    public void FormatForLlm_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _adapter.FormatForLlm(null!));
    }
}
