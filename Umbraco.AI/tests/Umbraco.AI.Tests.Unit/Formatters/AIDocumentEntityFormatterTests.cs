using System.Text.Json;
using System.Text.Json.Nodes;

using Moq;
using Shouldly;

using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.EntityAdapter.Adapters;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.Formatters;

public class CmsEntityFormatHelperTests
{
    [Fact]
    public void FormatCmsEntity_WithCmsStructure_FormatsAsProperties()
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
        var result = CmsEntityFormatHelper.FormatCmsEntity(entity);

        // Assert
        result.ShouldContain("## Entity Context");
        result.ShouldContain("Key: `doc-123`");
        result.ShouldContain("Name: `My Blog Post`");
        result.ShouldContain("Type: `document`");
        result.ShouldContain("Content type: blogPost");
        result.ShouldContain("### Properties");
        result.ShouldContain("**Title** (`title`): Hello World");
        result.ShouldContain("**Body Text** (`bodyText`): This is the content.");
    }

    [Fact]
    public void FormatCmsEntity_WithoutContentType_OmitsContentTypeLine()
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
        var result = CmsEntityFormatHelper.FormatCmsEntity(entity);

        // Assert
        result.ShouldNotContain("Content type:");
        result.ShouldContain("**Title** (`title`): Test");
    }

    [Fact]
    public void FormatCmsEntity_WithEmptyValueProperty_ShowsEmpty()
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
        var result = CmsEntityFormatHelper.FormatCmsEntity(entity);

        // Assert
        result.ShouldContain("**Title** (`title`): (empty)");
    }

    [Fact]
    public void FormatCmsEntity_WithNonCmsStructure_FallsBackToGenericFormat()
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
        var result = CmsEntityFormatHelper.FormatCmsEntity(entity);

        // Assert - should fall back to generic JSON formatting
        result.ShouldContain("### Entity Data");
        result.ShouldContain("```json");
        result.ShouldContain("\"sku\": \"12345\"");
        result.ShouldContain("\"price\": 29.99");
        result.ShouldNotContain("### Properties");
    }

    [Fact]
    public void FormatCmsEntity_WithSchemaServices_EmbedsJsonSchemaPerProperty()
    {
        // Arrange
        var contentTypeKey = Guid.NewGuid();
        var data = JsonDocument.Parse($$"""
            {
                "contentType": "{{contentTypeKey}}",
                "properties": [
                    {
                        "alias": "mainImage",
                        "label": "Main Image",
                        "editorAlias": "Umbraco.MediaPicker3",
                        "value": "[]"
                    },
                    {
                        "alias": "title",
                        "label": "Title",
                        "editorAlias": "Umbraco.TextBox",
                        "value": "Hello"
                    }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-1",
            Name = "Contact",
            Data = data
        };

        var mediaPickerSchema = JsonNode.Parse("""
            { "type": "array", "items": { "type": "object", "properties": { "key": { "type": "string", "format": "uuid" } } } }
            """)!.AsObject();

        var mainImageDataType = new PublishedDataType(11, "Umbraco.MediaPicker3", "Umbraco.MediaPicker3", new Lazy<object?>(() => null));
        var titleDataType = new PublishedDataType(12, "Umbraco.TextBox", "Umbraco.TextBox", new Lazy<object?>(() => null));

        var mainImageProp = new Mock<IPublishedPropertyType>();
        mainImageProp.SetupGet(p => p.Alias).Returns("mainImage");
        mainImageProp.SetupGet(p => p.DataType).Returns(mainImageDataType);

        var titleProp = new Mock<IPublishedPropertyType>();
        titleProp.SetupGet(p => p.Alias).Returns("title");
        titleProp.SetupGet(p => p.DataType).Returns(titleDataType);

        var publishedContentType = new Mock<IPublishedContentType>();
        publishedContentType.SetupGet(x => x.PropertyTypes)
            .Returns([mainImageProp.Object, titleProp.Object]);

        var typeCacheMock = new Mock<IPublishedContentTypeCache>();
        typeCacheMock.Setup(x => x.Get(PublishedItemType.Content, contentTypeKey))
            .Returns(publishedContentType.Object);

        var schemaServiceMock = new Mock<IPropertyEditorSchemaService>();
        schemaServiceMock.Setup(x => x.GetValueSchema("Umbraco.MediaPicker3", It.IsAny<object?>()))
            .Returns(mediaPickerSchema);
        schemaServiceMock.Setup(x => x.GetValueSchema("Umbraco.TextBox", It.IsAny<object?>()))
            .Returns((JsonObject?)null);

        // Act
        var result = CmsEntityFormatHelper.FormatCmsEntity(
            entity,
            typeCacheMock.Object,
            schemaServiceMock.Object,
            PublishedItemType.Content);

        // Assert: introductory note and inline schema are present
        result.ShouldContain("source of truth when calling set_value");
        result.ShouldContain("**Main Image** (`mainImage`)");
        result.ShouldContain("editor: `Umbraco.MediaPicker3`");
        result.ShouldContain("current value: []");
        result.ShouldContain("input shape (JSON Schema):");
        result.ShouldContain("\"format\":\"uuid\"");

        // Properties without schema still render the editor + current value
        result.ShouldContain("**Title** (`title`)");
        result.ShouldContain("editor: `Umbraco.TextBox`");
        result.ShouldContain("current value: Hello");
    }

    [Fact]
    public void FormatCmsEntity_WithSchemaServicesButUnknownContentType_FallsBackToFlatRendering()
    {
        // Arrange — content type GUID not registered in cache
        var contentTypeKey = Guid.NewGuid();
        var data = JsonDocument.Parse($$"""
            {
                "contentType": "{{contentTypeKey}}",
                "properties": [
                    { "alias": "title", "label": "Title", "editorAlias": "Umbraco.TextBox", "value": "Hi" }
                ]
            }
            """).RootElement;

        var entity = new AISerializedEntity
        {
            EntityType = "document",
            Unique = "doc-2",
            Name = "Doc",
            Data = data
        };

        var typeCacheMock = new Mock<IPublishedContentTypeCache>();
        typeCacheMock.Setup(x => x.Get(It.IsAny<PublishedItemType>(), It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("not registered"));

        var schemaServiceMock = new Mock<IPropertyEditorSchemaService>();

        // Act
        var result = CmsEntityFormatHelper.FormatCmsEntity(
            entity,
            typeCacheMock.Object,
            schemaServiceMock.Object,
            PublishedItemType.Content);

        // Assert: falls back to flat single-line per property, no schema header
        result.ShouldContain("**Title** (`title`): Hi");
        result.ShouldNotContain("source of truth when calling set_value");
        schemaServiceMock.Verify(
            x => x.GetValueSchema(It.IsAny<string>(), It.IsAny<object?>()),
            Times.Never);
    }

    [Fact]
    public void FormatCmsEntity_WithInvalidPropertyStructure_SkipsInvalidProperties()
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
        var result = CmsEntityFormatHelper.FormatCmsEntity(entity);

        // Assert - should only include valid property (invalid ones are skipped)
        result.ShouldContain("**Valid Property** (`valid`): OK");
        // "Invalid" appears in the test label, so we can't check for its absence
        // But "Bad" (the value) should not appear
        result.ShouldNotContain("Bad");
    }
}
