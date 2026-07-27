using System.Text.Json.Nodes;
using Moq;
using Shouldly;
using Umbraco.AI.Core.PropertyValueOperations;
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

    private AIPromptPropertyValueSchemaResolver CreateResolver(params IAISimplifiedPropertyValueTransformer[] transformers)
        => new(
            _mockContentTypeService.Object,
            _mockMediaTypeService.Object,
            _mockMemberTypeService.Object,
            _mockPropertyEditorSchemaService.Object,
            new AISimplifiedPropertyValueTransformerCollection(() => transformers));

    private static Mock<IPropertyType> CreatePropertyType(string alias, Guid dataTypeKey, string editorAlias = "Umbraco.TextBox")
    {
        var propertyType = new Mock<IPropertyType>();
        propertyType.SetupGet(p => p.Alias).Returns(alias);
        propertyType.SetupGet(p => p.DataTypeKey).Returns(dataTypeKey);
        propertyType.SetupGet(p => p.PropertyEditorAlias).Returns(editorAlias);
        return propertyType;
    }

    private void SetupDocumentProperty(string contentTypeAlias, IPropertyType propertyType)
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([propertyType]);
        _mockContentTypeService.Setup(s => s.Get(contentTypeAlias)).Returns(contentType.Object);
    }

    private void SetupWriteSchema(Guid dataTypeKey, JsonObject? schema, bool success = true)
        => _mockPropertyEditorSchemaService
            .Setup(s => s.GetSchemaAsync(dataTypeKey))
            .ReturnsAsync(success
                ? Attempt.SucceedWithStatus(PropertyEditorSchemaOperationStatus.Success, new PropertyValueSchema(typeof(JsonObject), schema))
                : Attempt.FailWithStatus(PropertyEditorSchemaOperationStatus.SchemaNotSupported, new PropertyValueSchema(null, null)));

    private sealed class FakeTransformer(string editorAlias, JsonNode? simplifiedSchema) : IAISimplifiedPropertyValueTransformer
    {
        public string ForPropertyEditorSchemaAlias => editorAlias;
        public Task<JsonNode?> GetSimplifiedSchemaAsync(Guid dataTypeKey, CancellationToken cancellationToken = default)
            => Task.FromResult(simplifiedSchema);
        public Task<JsonNode?> TransformToWriteValueAsync(JsonNode? simplifiedValue, JsonNode? currentValue, Guid dataTypeKey, CancellationToken cancellationToken = default)
            => Task.FromResult<JsonNode?>(simplifiedValue);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_DocumentPropertyWithSchema_ReturnsWriteSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        SetupDocumentProperty("page", CreatePropertyType("colour", dataTypeKey).Object);
        var schema = new JsonObject { ["type"] = "object" };
        SetupWriteSchema(dataTypeKey, schema);

        var result = await CreateResolver().ResolvePropertyValueSchemaAsync("page", "document", "colour");

        result.ShouldNotBeNull();
        result!.Schema.ShouldBe(schema);
        result.IsSimplified.ShouldBeFalse();
        result.DataTypeKey.ShouldBe(dataTypeKey);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_MediaEntityType_QueriesMediaTypeService()
    {
        var dataTypeKey = Guid.NewGuid();
        var mediaType = new Mock<IMediaType>();
        mediaType.SetupGet(c => c.CompositionPropertyTypes).Returns([CreatePropertyType("colour", dataTypeKey).Object]);
        _mockMediaTypeService.Setup(s => s.Get("image")).Returns(mediaType.Object);
        var schema = new JsonObject { ["type"] = "string" };
        SetupWriteSchema(dataTypeKey, schema);

        var result = await CreateResolver().ResolvePropertyValueSchemaAsync("image", "media", "colour");

        result!.Schema.ShouldBe(schema);
        _mockContentTypeService.Verify(s => s.Get(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_RegisteredTransformer_ReturnsSimplifiedSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        SetupDocumentProperty("page", CreatePropertyType("body", dataTypeKey, "Umbraco.RichText").Object);
        var simplified = new JsonObject { ["type"] = "string" };

        var result = await CreateResolver(new FakeTransformer("Umbraco.RichText", simplified))
            .ResolvePropertyValueSchemaAsync("page", "document", "body");

        result.ShouldNotBeNull();
        result!.IsSimplified.ShouldBeTrue();
        result.Schema.ShouldBe(simplified);
        result.EditorAlias.ShouldBe("Umbraco.RichText");
        // The write schema is not consulted when a transformer supplies a simplified schema.
        _mockPropertyEditorSchemaService.Verify(s => s.GetSchemaAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_TransformerReturnsNonObjectSchema_FallsBackToWriteSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        SetupDocumentProperty("page", CreatePropertyType("body", dataTypeKey, "Umbraco.RichText").Object);
        var writeSchema = new JsonObject { ["type"] = "object" };
        SetupWriteSchema(dataTypeKey, writeSchema);

        // A non-JsonObject simplified schema (a bare string node) must not throw — fall back to the write schema.
        var result = await CreateResolver(new FakeTransformer("Umbraco.RichText", JsonValue.Create("nonsense")))
            .ResolvePropertyValueSchemaAsync("page", "document", "body");

        result.ShouldNotBeNull();
        result!.IsSimplified.ShouldBeFalse();
        result.Schema.ShouldBe(writeSchema);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_PropertyNotFound_ReturnsNull()
    {
        var contentType = new Mock<IContentType>();
        contentType.SetupGet(c => c.CompositionPropertyTypes).Returns([]);
        _mockContentTypeService.Setup(s => s.Get("page")).Returns(contentType.Object);

        var result = await CreateResolver().ResolvePropertyValueSchemaAsync("page", "document", "missing");

        result.ShouldBeNull();
        _mockPropertyEditorSchemaService.Verify(s => s.GetSchemaAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ResolvePropertyValueSchemaAsync_EditorDoesNotSupportSchema_ReturnsResolutionWithNullSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        SetupDocumentProperty("page", CreatePropertyType("colour", dataTypeKey).Object);
        SetupWriteSchema(dataTypeKey, null, success: false);

        var result = await CreateResolver().ResolvePropertyValueSchemaAsync("page", "document", "colour");

        result.ShouldNotBeNull();
        result!.Schema.ShouldBeNull();
        result.IsSimplified.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ResolvePropertyValueSchemaAsync_MissingContentTypeAlias_ReturnsNullWithoutQuerying(string? alias)
    {
        var result = await CreateResolver().ResolvePropertyValueSchemaAsync(alias!, "document", "colour");

        result.ShouldBeNull();
        _mockContentTypeService.Verify(s => s.Get(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolveValueSchemaAsync_Obsolete_StillReturnsWriteSchema()
    {
        var dataTypeKey = Guid.NewGuid();
        SetupDocumentProperty("page", CreatePropertyType("colour", dataTypeKey).Object);
        var schema = new JsonObject { ["type"] = "object" };
        SetupWriteSchema(dataTypeKey, schema);

#pragma warning disable CS0618 // deliberately exercising the obsolete delegating method
        var result = await CreateResolver().ResolveValueSchemaAsync("page", "document", "colour");
#pragma warning restore CS0618

        result.ShouldBe(schema);
    }
}
