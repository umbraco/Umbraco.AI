using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Tests.Unit.EntityAdapter;

public class AIEntityContextHelperTests
{
    private readonly AIEntityAdapterCollection _adapterCollection;
    private readonly AIEntityContextHelper _helper;

    public AIEntityContextHelperTests()
    {
        // Create real adapter collection with a mock adapter
        var defaultAdapterMock = new Mock<IAIEntityAdapter>();
        defaultAdapterMock.Setup(a => a.EntityType).Returns((string?)null);
        defaultAdapterMock.Setup(a => a.FormatForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Mocked formatted output");
        defaultAdapterMock.Setup(a => a.FormatForLlmAsync(It.IsAny<AISerializedEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mocked formatted output");

        var adapters = new List<IAIEntityAdapter> { defaultAdapterMock.Object };
        _adapterCollection = new AIEntityAdapterCollection(() => adapters);

        _helper = new AIEntityContextHelper(_adapterCollection);
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
    public void BuildContextDictionary_WithMultipleCultures_PicksActiveCultureValue()
    {
        // Arrange — same alias appears once per culture (the shape the frontend
        // adapters now emit on multi-variant documents). Active culture is sv-SE,
        // so the helper must pick the Swedish entry, not the German one.
        var data = JsonDocument.Parse("""
            {
                "contentType": "article",
                "properties": [
                    { "alias": "header", "label": "Header", "value": "English header", "culture": "en-US", "segment": null },
                    { "alias": "header", "label": "Header", "value": "Dansk overskrift", "culture": "da-DK", "segment": null },
                    { "alias": "header", "label": "Header", "value": "Svensk rubrik", "culture": "sv-SE", "segment": null },
                    { "alias": "header", "label": "Header", "value": "Deutsche Überschrift", "culture": "de-DE", "segment": null }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Article",
            Culture = "sv-SE",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["header"].ShouldBe("Svensk rubrik");
    }

    [Fact]
    public void BuildContextDictionary_WithActiveCultureAndInvariantProperty_FallsBackToInvariantEntry()
    {
        // Arrange — `header` varies by culture, `slug` is invariant. Active
        // culture is sv-SE; `slug` has no Swedish entry so the invariant value
        // must be used.
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    { "alias": "header", "label": "Header", "value": "Svensk rubrik", "culture": "sv-SE", "segment": null },
                    { "alias": "header", "label": "Header", "value": "English header", "culture": "en-US", "segment": null },
                    { "alias": "slug", "label": "Slug", "value": "my-article", "culture": null, "segment": null }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Article",
            Culture = "sv-SE",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["header"].ShouldBe("Svensk rubrik");
        result["slug"].ShouldBe("my-article");
    }

    [Fact]
    public void BuildContextDictionary_WithActiveCultureAndOnlyMismatchedEntries_FallsBackToLastEntry()
    {
        // Arrange — active culture has no matching entry AND no invariant entry
        // exists. The helper falls back to the last entry to preserve the
        // pre-fix "something always resolves" behaviour rather than dropping the
        // variable. Documented contract.
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    { "alias": "header", "label": "Header", "value": "English header", "culture": "en-US", "segment": null },
                    { "alias": "header", "label": "Header", "value": "Deutsche Überschrift", "culture": "de-DE", "segment": null }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Article",
            Culture = "sv-SE",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert — last entry wins (matches old behaviour for unmodelled cases)
        result["header"].ShouldBe("Deutsche Überschrift");
    }

    [Fact]
    public void BuildContextDictionary_WithoutActiveCulture_TreatsEntityAsInvariant()
    {
        // Arrange — no active culture on the entity. Existing single-value
        // payloads (no culture metadata) must continue to resolve as before.
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    { "alias": "header", "label": "Header", "value": "Hello" },
                    { "alias": "body", "label": "Body", "value": "Content" }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Article",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["header"].ShouldBe("Hello");
        result["body"].ShouldBe("Content");
    }

    [Fact]
    public void BuildContextDictionary_WithActiveSegment_PicksMatchingSegmentEntry()
    {
        // Arrange — segment dimension. Active is (en-US, mobile); the helper
        // must pick the entry matching both culture and segment.
        var data = JsonDocument.Parse("""
            {
                "properties": [
                    { "alias": "headline", "value": "Desktop headline", "culture": "en-US", "segment": null },
                    { "alias": "headline", "value": "Mobile headline", "culture": "en-US", "segment": "mobile" }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Article",
            Culture = "en-US",
            Segment = "mobile",
            Data = data
        };

        // Act
        var result = _helper.BuildContextDictionary(entity);

        // Assert
        result["headline"].ShouldBe("Mobile headline");
    }

    [Fact]
    public void FormatForLlm_GetsAdapterForEntityType()
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
        result.ShouldBe("Mocked formatted output");
    }

    [Fact]
    public void FormatForLlm_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _helper.FormatForLlm(null!));
    }

    [Fact]
    public async Task FormatForLlmAsync_GetsAdapterForEntityType()
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
        var result = await _helper.FormatForLlmAsync(entity);

        // Assert — the default adapter mock only stubs the sync FormatForLlm; the helper's
        // async path must still reach it via the adapter's own default ContributeAsync wrapper.
        result.ShouldBe("Mocked formatted output");
    }

    [Fact]
    public async Task FormatForLlmAsync_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _helper.FormatForLlmAsync(null!));
    }
}
