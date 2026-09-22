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

    [Fact]
    public void ValidateRemoveItem_AcceptsRootLevelBlock()
    {
        // Arrange
        var handler = new BlockGridPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var rootContentKey = Guid.NewGuid();
        var envelope = BuildEnvelopeWithNestedBlock(rootContentKey, out _);

        // Act
        var result = handler.ValidateRemoveItem(envelope, rootContentKey, BuildContext());

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateRemoveItem_RejectsBlockNestedInArea()
    {
        // Arrange
        var handler = new BlockGridPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var envelope = BuildEnvelopeWithNestedBlock(Guid.NewGuid(), out var nestedContentKey);

        // Act
        var result = handler.ValidateRemoveItem(envelope, nestedContentKey, BuildContext());

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.OperationNotSupported);
    }

    /// <summary>
    /// Builds an envelope with one root-level block containing a single nested block inside its
    /// first area, mirroring content a third party (or a future v2) could have produced directly
    /// via the CMS backoffice.
    /// </summary>
    private static JsonObject BuildEnvelopeWithNestedBlock(Guid rootContentKey, out Guid nestedContentKey)
    {
        nestedContentKey = Guid.NewGuid();
        var rootContentTypeKey = Guid.NewGuid();
        var nestedContentTypeKey = Guid.NewGuid();

        return new JsonObject
        {
            [BlockEnvelopeOps.LayoutPropertyName] = new JsonObject
            {
                [LayoutKey] = new JsonArray
                {
                    new JsonObject
                    {
                        ["contentKey"] = rootContentKey,
                        ["columnSpan"] = 12,
                        ["rowSpan"] = 1,
                        ["areas"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["key"] = Guid.NewGuid(),
                                ["items"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["contentKey"] = nestedContentKey,
                                        ["columnSpan"] = 12,
                                        ["rowSpan"] = 1,
                                        ["areas"] = new JsonArray(),
                                    },
                                },
                            },
                        },
                    },
                },
            },
            [BlockEnvelopeOps.ContentDataPropertyName] = new JsonArray
            {
                new JsonObject { ["key"] = rootContentKey, ["contentTypeKey"] = rootContentTypeKey, ["values"] = new JsonArray() },
                new JsonObject { ["key"] = nestedContentKey, ["contentTypeKey"] = nestedContentTypeKey, ["values"] = new JsonArray() },
            },
            [BlockEnvelopeOps.SettingsDataPropertyName] = new JsonArray(),
            [BlockEnvelopeOps.ExposePropertyName] = new JsonArray(),
        };
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
