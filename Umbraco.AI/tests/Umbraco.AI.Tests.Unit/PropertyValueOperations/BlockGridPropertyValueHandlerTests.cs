using System.Text.Json.Nodes;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class BlockGridPropertyValueHandlerTests
{
    private const string LayoutKey = "Umbraco.BlockGrid";

    [Fact]
    public async Task AddItemAsync_AtRoot_AppendsLayoutEntryWithDefaultColumnSpan()
    {
        // Arrange
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeService(elementTypeKey);
        var handler = new BlockGridPropertyValueHandler(contentTypeService);

        var args = new AIAddItemArgs(
            ElementType: elementTypeKey.ToString(),
            Values: new JsonObject { ["title"] = "Hello" });

        // Act
        var result = await handler.AddItemAsync(value: null, args, BuildContext());

        // Assert
        var envelope = (JsonObject)result.Value!;
        var layout = (JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layout.Count.ShouldBe(1);
        layout[0]!["columnSpan"]!.GetValue<int>().ShouldBe(12);
        layout[0]!["rowSpan"]!.GetValue<int>().ShouldBe(1);
        ((JsonArray)layout[0]!["areas"]!).Count.ShouldBe(0);
    }

    [Fact]
    public void ValidateAddItem_RejectsExtraFields()
    {
        // Arrange
        var handler = new BlockGridPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var args = new AIAddItemArgs(
            ElementType: Guid.NewGuid().ToString(),
            Extra: new JsonObject { ["gridArea"] = "header" });

        // Act
        var result = handler.ValidateAddItem(value: null, args, BuildContext());

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.OperationNotSupported);
    }

    [Fact]
    public void ValidateAddItem_AcceptsRootLevelAdd()
    {
        var handler = new BlockGridPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var args = new AIAddItemArgs(ElementType: Guid.NewGuid().ToString(), Values: new JsonObject { ["title"] = "x" });

        var result = handler.ValidateAddItem(value: null, args, BuildContext());

        result.IsValid.ShouldBeTrue();
    }

    private static IContentTypeService BuildContentTypeService(Guid contentTypeKey)
    {
        var contentType = new Mock<IContentType>();
        contentType.Setup(c => c.Key).Returns(contentTypeKey);
        contentType.As<IContentTypeComposition>()
            .Setup(c => c.CompositionPropertyTypes)
            .Returns(Array.Empty<IPropertyType>());

        var service = new Mock<IContentTypeService>();
        service.Setup(s => s.Get(contentTypeKey)).Returns(contentType.Object);
        return service.Object;
    }

    private static AIPropertyValueOperationContext BuildContext()
    {
        var schemaService = new Mock<IPropertyEditorSchemaService>();
        schemaService.Setup(s => s.GetSchemaAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Attempt<PropertyValueSchema, PropertyEditorSchemaOperationStatus>.Fail(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        return new AIPropertyValueOperationContext(
            schemaService.Object,
            new Mock<IAIPropertyDefaultValueProvider>().Object,
            new AIDocumentMetadata(
                ContentTypeKey: Guid.NewGuid(),
                Variants: [new AIVariantId(null, null)],
                IsVariant: false,
                IsSegmented: false),
            new Mock<IAIPropertyValueDispatcher>().Object);
    }
}
