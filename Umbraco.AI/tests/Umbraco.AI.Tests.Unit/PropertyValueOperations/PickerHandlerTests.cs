using System.Text.Json.Nodes;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class PickerHandlerTests
{
    [Fact]
    public async Task MediaPicker3_AddItem_AppendsToArrayWithGeneratedKey()
    {
        var handler = new MediaPicker3PropertyValueHandler();
        var mediaKey = Guid.NewGuid();

        var args = new AIAddItemArgs(Values: new JsonObject { ["mediaKey"] = mediaKey });
        var result = await handler.AddItemAsync(value: null, args, BuildContext());

        var array = (JsonArray)result.Value!;
        array.Count.ShouldBe(1);
        array[0]!["key"]!.GetValue<Guid>().ShouldBe(result.BlockKey);
        array[0]!["mediaKey"]!.GetValue<Guid>().ShouldBe(mediaKey);
    }

    [Fact]
    public void MediaPicker3_ValidateAddItem_RequiresMediaKey()
    {
        var handler = new MediaPicker3PropertyValueHandler();

        var result = handler.ValidateAddItem(null, new AIAddItemArgs(Values: new JsonObject()), BuildContext());

        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.SchemaMismatch);
    }

    [Fact]
    public async Task MultiUrlPicker_AddItem_AcceptsExternalLink()
    {
        var handler = new MultiUrlPickerPropertyValueHandler();

        var args = new AIAddItemArgs(Values: new JsonObject
        {
            ["type"] = "external",
            ["url"] = "https://example.com",
            ["name"] = "Example",
        });

        var result = await handler.AddItemAsync(value: null, args, BuildContext());

        var array = (JsonArray)result.Value!;
        array.Count.ShouldBe(1);
        array[0]!["type"]!.GetValue<string>().ShouldBe("external");
        array[0]!["url"]!.GetValue<string>().ShouldBe("https://example.com");
        array[0]!["name"]!.GetValue<string>().ShouldBe("Example");
    }

    [Fact]
    public async Task MultiUrlPicker_SetItemPropertyValue_UpdatesEditableField()
    {
        var handler = new MultiUrlPickerPropertyValueHandler();
        var key = Guid.NewGuid();

        var existing = new JsonArray
        {
            new JsonObject { ["key"] = key, ["type"] = "external", ["name"] = "Old" },
        };

        var result = await handler.SetItemPropertyValueAsync(
            existing, key, "name", JsonValue.Create("New"), variantId: null, BuildContext());

        var array = (JsonArray)result!;
        array[0]!["name"]!.GetValue<string>().ShouldBe("New");
    }

    [Fact]
    public async Task MultiUrlPicker_SetItemPropertyValue_RejectsNonEditableField()
    {
        var handler = new MultiUrlPickerPropertyValueHandler();
        var key = Guid.NewGuid();

        var existing = new JsonArray
        {
            new JsonObject { ["key"] = key, ["type"] = "external" },
        };

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.SetItemPropertyValueAsync(
                existing, key, "type", JsonValue.Create("media"), variantId: null, BuildContext()));
    }

    [Fact]
    public async Task MediaPicker3_RemoveItem_RemovesByKey()
    {
        var handler = new MediaPicker3PropertyValueHandler();
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();

        var existing = new JsonArray
        {
            new JsonObject { ["key"] = keep, ["mediaKey"] = Guid.NewGuid() },
            new JsonObject { ["key"] = remove, ["mediaKey"] = Guid.NewGuid() },
        };

        var result = await handler.RemoveItemAsync(existing, remove, BuildContext());

        var array = (JsonArray)result!;
        array.Count.ShouldBe(1);
        array[0]!["key"]!.GetValue<Guid>().ShouldBe(keep);
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
