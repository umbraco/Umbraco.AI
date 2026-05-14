using System.Text.Json.Nodes;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class RichTextPropertyValueHandlerTests
{
    [Fact]
    public void ValidateAddItem_AlwaysRejects_WithMarkupGuidance()
    {
        var handler = new RichTextPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var args = new AIAddItemArgs(ElementType: Guid.NewGuid().ToString(), Values: new JsonObject());

        var result = handler.ValidateAddItem(null, args, BuildContext());

        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.OperationNotSupported);
        result.Error.Message.ShouldContain("markup");
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesBlockFromInnerEnvelope()
    {
        var handler = new RichTextPropertyValueHandler(new Mock<IContentTypeService>().Object);

        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();

        var rteValue = new JsonObject
        {
            ["markup"] = "<p>Hello</p>",
            ["blocks"] = new JsonObject
            {
                ["layout"] = new JsonObject
                {
                    ["Umbraco.RichText.Blocks"] = new JsonArray
                    {
                        new JsonObject { ["contentKey"] = keep },
                        new JsonObject { ["contentKey"] = remove },
                    },
                },
                ["contentData"] = new JsonArray
                {
                    new JsonObject { ["key"] = keep, ["values"] = new JsonArray() },
                    new JsonObject { ["key"] = remove, ["values"] = new JsonArray() },
                },
                ["settingsData"] = new JsonArray(),
                ["expose"] = new JsonArray(),
            },
        };

        var result = await handler.RemoveItemAsync(rteValue, remove, BuildContext());

        var rte = (JsonObject)result!;
        rte["markup"]!.GetValue<string>().ShouldBe("<p>Hello</p>");
        var inner = (JsonObject)rte["blocks"]!;
        var layout = (JsonArray)inner["layout"]!["Umbraco.RichText.Blocks"]!;
        layout.Count.ShouldBe(1);
        layout[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(keep);
    }

    [Fact]
    public async Task ClearAsync_ReturnsEmptyMarkupAndEmptyBlocksEnvelope()
    {
        var handler = new RichTextPropertyValueHandler(new Mock<IContentTypeService>().Object);

        var rteValue = new JsonObject
        {
            ["markup"] = "<p>Hello</p>",
            ["blocks"] = new JsonObject(),
        };

        var result = await handler.ClearAsync(rteValue, BuildContext());

        var rte = (JsonObject)result!;
        rte["markup"]!.GetValue<string>().ShouldBe(string.Empty);
        rte["blocks"]!["layout"]!["Umbraco.RichText.Blocks"].ShouldBeOfType<JsonArray>();
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
