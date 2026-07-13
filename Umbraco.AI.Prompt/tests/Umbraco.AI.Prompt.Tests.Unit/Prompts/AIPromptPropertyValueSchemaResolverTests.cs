using System.Text.Json.Nodes;
using Moq;
using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptPropertyValueSchemaResolverTests
{
    private readonly Mock<IContentTypeService> _mockContentTypeService = new();
    private readonly Mock<IMediaTypeService> _mockMediaTypeService = new();
    private readonly Mock<IMemberTypeService> _mockMemberTypeService = new();
    private readonly Mock<IPropertyEditorSchemaService> _mockPropertyEditorSchemaService = new();
    private readonly AIPromptPropertyValueSchemaResolver _resolver;

    public AIPromptPropertyValueSchemaResolverTests()
    {
        _resolver = new AIPromptPropertyValueSchemaResolver(
            _mockContentTypeService.Object,
            _mockMediaTypeService.Object,
            _mockMemberTypeService.Object,
            _mockPropertyEditorSchemaService.Object);
    }

    private static Mock<IPropertyType> CreatePropertyType(string alias, Guid dataTypeKey)
    {
        var propertyType = new Mock<IPropertyType>();
        propertyType.SetupGet(p => p.Alias).Returns(alias);
        propertyType.SetupGet(p => p.DataTypeKey).Returns(dataTypeKey);
        return propertyType;
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_DocumentPropertyWithSchema_ReturnsJsonSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        var propertyType = CreatePropertyType("colour", dataTypeKey);

        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([propertyType.Object]);
        _mockContentTypeService.Setup(s => s.Get("page")).Returns(contentType.Object);

        var schema = new JsonObject { ["type"] = "object" };
        _mockPropertyEditorSchemaService
            .Setup(s => s.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(Attempt.SucceedWithStatus(
                PropertyEditorSchemaOperationStatus.Success,
                new PropertyValueSchema(typeof(JsonObject), schema)));

        var result = await _resolver.ResolveValueSchemaAsync("page", "document", "colour");

        result.ShouldBe(schema);
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_MediaEntityType_QueriesMediaTypeService()
    {
        var dataTypeKey = Guid.NewGuid();
        var propertyType = CreatePropertyType("colour", dataTypeKey);

        var mediaType = new Mock<IMediaType>();
        mediaType.SetupGet(c => c.CompositionPropertyTypes).Returns([propertyType.Object]);
        _mockMediaTypeService.Setup(s => s.Get("image")).Returns(mediaType.Object);

        var schema = new JsonObject { ["type"] = "string" };
        _mockPropertyEditorSchemaService
            .Setup(s => s.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(Attempt.SucceedWithStatus(
                PropertyEditorSchemaOperationStatus.Success,
                new PropertyValueSchema(typeof(string), schema)));

        var result = await _resolver.ResolveValueSchemaAsync("image", "media", "colour");

        result.ShouldBe(schema);
        _mockContentTypeService.Verify(s => s.Get(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_PropertyNotFoundOnContentType_ReturnsNull()
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([]);
        _mockContentTypeService.Setup(s => s.Get("page")).Returns(contentType.Object);

        var result = await _resolver.ResolveValueSchemaAsync("page", "document", "missing");

        result.ShouldBeNull();
        _mockPropertyEditorSchemaService.Verify(
            s => s.GetSchemaAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_ContentTypeNotFound_ReturnsNull()
    {
        _mockContentTypeService.Setup(s => s.Get("page")).Returns((IContentType?)null);

        var result = await _resolver.ResolveValueSchemaAsync("page", "document", "colour");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_EditorDoesNotSupportSchema_ReturnsNull()
    {
        var dataTypeKey = Guid.NewGuid();
        var propertyType = CreatePropertyType("colour", dataTypeKey);

        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([propertyType.Object]);
        _mockContentTypeService.Setup(s => s.Get("page")).Returns(contentType.Object);

        _mockPropertyEditorSchemaService
            .Setup(s => s.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(Attempt.FailWithStatus(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        var result = await _resolver.ResolveValueSchemaAsync("page", "document", "colour");

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ResolveValueSchemaAsync_MissingContentTypeAlias_ReturnsNullWithoutQuerying(string? alias)
    {
        var result = await _resolver.ResolveValueSchemaAsync(alias!, "document", "colour");

        result.ShouldBeNull();
        _mockContentTypeService.Verify(s => s.Get(It.IsAny<string>()), Times.Never);
    }
}
