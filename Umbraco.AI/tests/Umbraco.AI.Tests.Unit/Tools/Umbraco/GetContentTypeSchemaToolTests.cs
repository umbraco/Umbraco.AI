using System.Text.Json.Nodes;

using Moq;
using Shouldly;

using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentTypeSchemaToolTests
{
    private readonly Mock<IPublishedContentTypeCache> _publishedContentTypeCacheMock;
    private readonly Mock<IPropertyEditorSchemaService> _propertyEditorSchemaServiceMock;
    private readonly Mock<IIdKeyMap> _idKeyMapMock;
    private readonly IAITool _tool;

    public GetContentTypeSchemaToolTests()
    {
        _publishedContentTypeCacheMock = new Mock<IPublishedContentTypeCache>();
        _propertyEditorSchemaServiceMock = new Mock<IPropertyEditorSchemaService>();
        _idKeyMapMock = new Mock<IIdKeyMap>();

        _tool = new GetContentTypeSchemaTool(
            _publishedContentTypeCacheMock.Object,
            _propertyEditorSchemaServiceMock.Object,
            _idKeyMapMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyAlias_ReturnsError(string? alias)
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(alias!);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldNotBeNull();
        schemaResult.Message.ShouldContain("empty");
        schemaResult.Schema.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentAlias_ReturnsNotFound()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs("nonExistentType");

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(It.IsAny<PublishedItemType>(), "nonExistentType"))
            .Returns((IPublishedContentType?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldNotBeNull();
        schemaResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_PropertyWithSchemaSupport_EmbedsValueSchemaAndDataTypeKey()
    {
        // Arrange
        var dataTypeKey = Guid.NewGuid();
        var jsonSchema = JsonNode.Parse("""{ "type": "string", "maxLength": 250 }""")!.AsObject();

        var contentType = BuildContentType(
            alias: "blogPost",
            properties: [BuildPropertyType("title", dataTypeId: 42, editorAlias: "Umbraco.TextBox")]);

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(PublishedItemType.Content, "blogPost"))
            .Returns(contentType);

        _idKeyMapMock
            .Setup(x => x.GetKeyForId(42, UmbracoObjectTypes.DataType))
            .Returns(Attempt<Guid>.Succeed(dataTypeKey));

        _propertyEditorSchemaServiceMock
            .Setup(x => x.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(Attempt.SucceedWithStatus(
                PropertyEditorSchemaOperationStatus.Success,
                new PropertyValueSchema(typeof(string), jsonSchema)));

        // Act
        var result = await _tool.ExecuteAsync(new GetContentTypeSchemaArgs("blogPost"), CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        schemaResult.Success.ShouldBeTrue();
        schemaResult.Schema.ShouldNotBeNull();
        schemaResult.Schema!.Properties.Count.ShouldBe(1);

        var prop = schemaResult.Schema.Properties[0];
        prop.Alias.ShouldBe("title");
        prop.EditorAlias.ShouldBe("Umbraco.TextBox");
        prop.DataTypeKey.ShouldBe(dataTypeKey);
        prop.ValueSchema.ShouldNotBeNull();
        prop.ValueSchema!["type"]?.GetValue<string>().ShouldBe("string");
    }

    [Fact]
    public async Task ExecuteAsync_PropertyWithoutSchemaSupport_OmitsValueSchemaButKeepsDataTypeKey()
    {
        // Arrange
        var dataTypeKey = Guid.NewGuid();

        var contentType = BuildContentType(
            alias: "blogPost",
            properties: [BuildPropertyType("legacyField", dataTypeId: 7, editorAlias: "Custom.LegacyEditor")]);

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(PublishedItemType.Content, "blogPost"))
            .Returns(contentType);

        _idKeyMapMock
            .Setup(x => x.GetKeyForId(7, UmbracoObjectTypes.DataType))
            .Returns(Attempt<Guid>.Succeed(dataTypeKey));

        _propertyEditorSchemaServiceMock
            .Setup(x => x.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(Attempt.FailWithStatus(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        // Act
        var result = await _tool.ExecuteAsync(new GetContentTypeSchemaArgs("blogPost"), CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        schemaResult.Success.ShouldBeTrue();
        var prop = schemaResult.Schema!.Properties[0];
        prop.DataTypeKey.ShouldBe(dataTypeKey);
        prop.ValueSchema.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_PropertyWithUnresolvableDataTypeKey_OmitsBoth()
    {
        // Arrange
        var contentType = BuildContentType(
            alias: "blogPost",
            properties: [BuildPropertyType("orphan", dataTypeId: 99, editorAlias: "Umbraco.TextBox")]);

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(PublishedItemType.Content, "blogPost"))
            .Returns(contentType);

        _idKeyMapMock
            .Setup(x => x.GetKeyForId(99, UmbracoObjectTypes.DataType))
            .Returns(Attempt<Guid>.Fail());

        // Act
        var result = await _tool.ExecuteAsync(new GetContentTypeSchemaArgs("blogPost"), CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        schemaResult.Success.ShouldBeTrue();
        var prop = schemaResult.Schema!.Properties[0];
        prop.DataTypeKey.ShouldBeNull();
        prop.ValueSchema.ShouldBeNull();
        _propertyEditorSchemaServiceMock.Verify(
            x => x.GetSchemaAsync(It.IsAny<Guid>()),
            Times.Never,
            "schema lookup must be skipped when the data type GUID can't be resolved");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleProperties_ResolvesEachIndependently()
    {
        // Arrange
        var keyA = Guid.NewGuid();
        var keyB = Guid.NewGuid();
        var schemaA = JsonNode.Parse("""{ "type": "string" }""")!.AsObject();

        var contentType = BuildContentType(
            alias: "page",
            properties:
            [
                BuildPropertyType("title", dataTypeId: 1, editorAlias: "Umbraco.TextBox"),
                BuildPropertyType("body", dataTypeId: 2, editorAlias: "Umbraco.RichText"),
            ]);

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(PublishedItemType.Content, "page"))
            .Returns(contentType);
        _idKeyMapMock.Setup(x => x.GetKeyForId(1, UmbracoObjectTypes.DataType))
            .Returns(Attempt<Guid>.Succeed(keyA));
        _idKeyMapMock.Setup(x => x.GetKeyForId(2, UmbracoObjectTypes.DataType))
            .Returns(Attempt<Guid>.Succeed(keyB));
        _propertyEditorSchemaServiceMock.Setup(x => x.GetSchemaAsync(keyA))
            .ReturnsAsync(Attempt.SucceedWithStatus(
                PropertyEditorSchemaOperationStatus.Success,
                new PropertyValueSchema(typeof(string), schemaA)));
        _propertyEditorSchemaServiceMock.Setup(x => x.GetSchemaAsync(keyB))
            .ReturnsAsync(Attempt.FailWithStatus(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        // Act
        var result = await _tool.ExecuteAsync(new GetContentTypeSchemaArgs("page"), CancellationToken.None);

        // Assert
        var schemaResult = result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var props = schemaResult.Schema!.Properties;
        props.Count.ShouldBe(2);
        props.Single(p => p.Alias == "title").ValueSchema.ShouldNotBeNull();
        props.Single(p => p.Alias == "body").ValueSchema.ShouldBeNull();
    }

    [Fact]
    public void Description_MentionsValueSchemaAndComplementaryTool()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("ValueSchema");
        description.ShouldContain("get_property_value_schema");
    }

    private static IPublishedContentType BuildContentType(string alias, IPublishedPropertyType[] properties)
    {
        var ct = new Mock<IPublishedContentType>();
        ct.SetupGet(x => x.Alias).Returns(alias);
        ct.SetupGet(x => x.IsElement).Returns(false);
        ct.SetupGet(x => x.PropertyTypes).Returns(properties);
        ct.SetupGet(x => x.CompositionAliases).Returns(new HashSet<string>());
        return ct.Object;
    }

    private static IPublishedPropertyType BuildPropertyType(string alias, int dataTypeId, string editorAlias)
    {
        var dataType = new PublishedDataType(dataTypeId, editorAlias, editorAlias, new Lazy<object?>(() => null));

        var pt = new Mock<IPublishedPropertyType>();
        pt.SetupGet(x => x.Alias).Returns(alias);
        pt.SetupGet(x => x.DataType).Returns(dataType);
        pt.SetupGet(x => x.ModelClrType).Returns(typeof(string));
        return pt.Object;
    }
}
