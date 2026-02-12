using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Tests.Unit.Formatters;

public class AIDocumentEntityFormatterTests
{
    private readonly AIDocumentEntityFormatter _formatter = new();

    [Fact]
    public void EntityType_ReturnsDocument()
    {
        // Assert
        _formatter.EntityType.ShouldBe("document");
    }

    [Fact]
    public void Format_WithCmsStructure_FormatsAsProperties()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "contentType": "blogPost",
                "properties": [
                    {
                        "alias": "title",
                        "label": "Title",
                        "editorAlias": "Umbraco.TextBox",
                        "value": "Hello World"
                    },
                    {
                        "alias": "bodyText",
                        "label": "Body Text",
                        "editorAlias": "Umbraco.TextArea",
                        "value": "This is the content."
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-123",
            Name = "My Blog Post",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldContain("## Current Entity Context");
        result.ShouldContain("Key: `doc-123`");
        result.ShouldContain("Name: `My Blog Post`");
        result.ShouldContain("Type: `document`");
        result.ShouldContain("Content type: blogPost");
        result.ShouldContain("### Properties");
        result.ShouldContain("**Title** (`title`): Hello World");
        result.ShouldContain("**Body Text** (`bodyText`): This is the content.");
    }

    [Fact]
    public void Format_WithoutContentType_OmitsContentTypeLine()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    {
                        "alias": "title",
                        "label": "Title",
                        "editorAlias": "Umbraco.TextBox",
                        "value": "Test"
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-456",
            Name = "Test Doc",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldNotContain("Content type:");
        result.ShouldContain("**Title** (`title`): Test");
    }

    [Fact]
    public void Format_WithEmptyValueProperty_ShowsEmpty()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "contentType": "article",
                "properties": [
                    {
                        "alias": "title",
                        "label": "Title",
                        "editorAlias": "Umbraco.TextBox",
                        "value": null
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-789",
            Name = "Empty Article",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert
        result.ShouldContain("**Title** (`title`): (empty)");
    }

    [Fact]
    public void Format_WithNonCmsStructure_FallsBackToGenericFormatter()
    {
        // Arrange - data that doesn't match CMS structure (no properties array)
        var data = JsonDocument.Parse("""
            {
                "sku": "12345",
                "price": 29.99
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-999",
            Name = "Non-CMS Entity",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert - should fall back to generic JSON formatting
        result.ShouldContain("### Entity Data");
        result.ShouldContain("```json");
        result.ShouldContain("\"sku\": \"12345\"");
        result.ShouldContain("\"price\": 29.99");
        result.ShouldNotContain("### Properties");
    }

    [Fact]
    public void Format_WithInvalidPropertyStructure_SkipsInvalidProperties()
    {
        // Arrange - properties array with invalid items
        var data = JsonDocument.Parse("""
            {
                "contentType": "test",
                "properties": [
                    {
                        "alias": "valid",
                        "label": "Valid Property",
                        "editorAlias": "Umbraco.TextBox",
                        "value": "OK"
                    },
                    {
                        "alias": null,
                        "label": "Invalid",
                        "editorAlias": "Umbraco.TextBox",
                        "value": "Bad"
                    },
                    "not-an-object"
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-invalid",
            Name = "Test",
            Data = data
        };

        // Act
        var result = _formatter.Format(entity);

        // Assert - should only include valid property
        result.ShouldContain("**Valid Property** (`valid`): OK");
        result.ShouldNotContain("Invalid");
        result.ShouldNotContain("Bad");
    }

    [Fact]
    public void Format_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _formatter.Format(null!));
    }
}
