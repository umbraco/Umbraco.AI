using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Tests.Unit.EntityAdapter;

public class AIEntityContextHelperTests
{
    private readonly Mock<AIEntityFormatterCollection> _formatterCollectionMock;
    private readonly AIEntityContextHelper _helper;

    public AIEntityContextHelperTests()
    {
        // Create mock formatter collection with a default formatter
        _formatterCollectionMock = new Mock<AIEntityFormatterCollection>(() => []);

        var defaultFormatterMock = new Mock<IAIEntityFormatter>();
        defaultFormatterMock.Setup(f => f.EntityType).Returns((string?)null);
        defaultFormatterMock.Setup(f => f.Format(It.IsAny<AISerializedEntity>()))
            .Returns("Mocked formatted output");

        _formatterCollectionMock
            .Setup(c => c.GetFormatter(It.IsAny<string>()))
            .Returns(defaultFormatterMock.Object);

        _helper = new AIEntityContextHelper(_formatterCollectionMock.Object);
    }

    [Fact]
    public void BuildContextDictionary_WithBasicEntity_IncludesBasicFields()
    {
        // Arrange
        var data = JsonDocument.Parse("{}").RootElement;
        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-123",
            Name = "Test Document",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["entityType"].ShouldBe("document");
        result["entityId"].ShouldBe("doc-123");
        result["entityName"].ShouldBe("Test Document");
    }

    [Fact]
    public void BuildContextDictionary_WithContentTypeInData_ExtractsContentType()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "contentType": "blogPost",
                "otherField": "value"
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-456",
            Name = "Blog Post",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["contentType"].ShouldBe("blogPost");
    }

    [Fact]
    public void BuildContextDictionary_WithPropertiesInData_ExtractsPropertyValues()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "contentType": "article",
                "properties": [
                    {
                        "alias": "title",
                        "label": "Title",
                        "value": "Hello World"
                    },
                    {
                        "alias": "bodyText",
                        "label": "Body",
                        "value": "Content here"
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-789",
            Name = "Article",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["title"].ShouldBe("Hello World");
        result["bodyText"].ShouldBe("Content here");
    }

    [Fact]
    public void BuildContextDictionary_WithNumericValue_ExtractsAsDouble()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    {
                        "alias": "price",
                        "label": "Price",
                        "value": 29.99
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "product",
            Unique = "prod-1",
            Name = "Widget",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["price"].ShouldBe(29.99);
    }

    [Fact]
    public void BuildContextDictionary_WithBooleanValue_ExtractsAsBool()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    {
                        "alias": "featured",
                        "label": "Featured",
                        "value": true
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Test",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["featured"].ShouldBe(true);
    }

    [Fact]
    public void BuildContextDictionary_WithNullValue_ExtractsAsNull()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    {
                        "alias": "optional",
                        "label": "Optional",
                        "value": null
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Test",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["optional"].ShouldBeNull();
    }

    [Fact]
    public void BuildContextDictionary_WithComplexValue_ExtractsAsJsonString()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    {
                        "alias": "config",
                        "label": "Config",
                        "value": {
                            "nested": "value",
                            "count": 42
                        }
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Test",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["config"].ShouldBeOfType<string>();
        ((string)result["config"]!).ShouldContain("nested");
        ((string)result["config"]!).ShouldContain("value");
    }

    [Fact]
    public void BuildContextDictionary_WithoutContentType_DoesNotIncludeContentType()
    {
        // Arrange
        var data = JsonDocument.Parse("{}").RootElement;
        var entity = new AISerializedEntity
        {
            EntityType = "product",
            Unique = "prod-1",
            Name = "Widget",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result.ShouldNotContainKey("contentType");
    }

    [Fact]
    public void BuildContextDictionary_WithoutProperties_OnlyIncludesBasicFields()
    {
        // Arrange
        var data = JsonDocument.Parse("""
            {
                "someField": "value"
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "custom",
            Unique = "custom-1",
            Name = "Custom Entity",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result.Keys.Count.ShouldBe(3); // Only entityType, entityId, entityName
        result["entityType"].ShouldBe("custom");
        result["entityId"].ShouldBe("custom-1");
        result["entityName"].ShouldBe("Custom Entity");
    }

    [Fact]
    public void BuildContextDictionary_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _helper.BuildContextDictionary(null!));
    }

    [Fact]
    public void FormatForLlm_GetsFormatterForEntityType()
    {
        // Arrange
        var data = JsonDocument.Parse("{}").RootElement;
        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Test",
            Data = data
        };

        // Act
        var result = _helper.FormatForLlm(entity);

        // Assert
        _formatterCollectionMock.Verify(c => c.GetFormatter("document"), Times.Once);
        result.ShouldBe("Mocked formatted output");
    }

    [Fact]
    public void FormatForLlm_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _helper.FormatForLlm(null!));
    }
}
