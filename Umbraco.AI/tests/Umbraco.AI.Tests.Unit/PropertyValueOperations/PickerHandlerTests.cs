using System.Text.Json;
using System.Text.Json.Nodes;

using Moq;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
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

    [Theory]
    [InlineData("document", "unique")]
    [InlineData("document", "documentKey")]
    [InlineData("media", "mediaKey")]
    [InlineData("Document", "unique")]
    public async Task MultiUrlPicker_AddItem_ContentLink_KeepsTargetWhenReadByCms(string type, string targetField)
    {
        // The CMS reads the editor value as LinkDisplay and only keeps the target (Udi) when
        // `unique` is set. Written under any other name, the link is saved pointing nowhere.
        var handler = new MultiUrlPickerPropertyValueHandler();
        var target = Guid.NewGuid();

        var result = await handler.AddItemAsync(
            value: null,
            new AIAddItemArgs(Values: new JsonObject { ["type"] = type, [targetField] = target, ["name"] = "Link" }),
            BuildContext());

        var links = JsonSerializer.Deserialize<List<LinkDisplay>>(
            result.Value!.ToJsonString(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!; // as strict as the CMS serializer
        var link = links.ShouldHaveSingleItem();
        link.Type.ShouldBe(type.ToLowerInvariant());
        link.Unique.ShouldBe(target);
    }

    [Fact]
    public void MultiUrlPicker_ValidateAddItem_ContentLinkWithoutTarget_IsInvalid()
    {
        var handler = new MultiUrlPickerPropertyValueHandler();

        var result = handler.ValidateAddItem(
            null, new AIAddItemArgs(Values: new JsonObject { ["type"] = "document", ["name"] = "Link" }), BuildContext());

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task MultiUrlPicker_RemoveItem_MatchesSavedLinkByUnique()
    {
        // Links read back from a saved value have no `key`, only the target's `unique`.
        var handler = new MultiUrlPickerPropertyValueHandler();
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var existing = new JsonArray
        {
            new JsonObject { ["type"] = "document", ["unique"] = keep },
            new JsonObject { ["type"] = "media", ["unique"] = remove },
        };

        var result = await handler.RemoveItemAsync(existing, remove, BuildContext());

        var array = (JsonArray)result!;
        array.ShouldHaveSingleItem()!["unique"]!.GetValue<Guid>().ShouldBe(keep);
    }

    [Fact]
    public async Task MultiUrlPicker_RemoveItem_TwoLinksToSameItem_RemovesOnlyOne()
    {
        var handler = new MultiUrlPickerPropertyValueHandler();
        var target = Guid.NewGuid();
        var existing = new JsonArray
        {
            new JsonObject { ["type"] = "document", ["unique"] = target, ["name"] = "Nav" },
            new JsonObject { ["type"] = "document", ["unique"] = target, ["name"] = "Footer" },
        };

        var result = await handler.RemoveItemAsync(existing, target, BuildContext());

        var array = (JsonArray)result!;
        array.ShouldHaveSingleItem()!["name"]!.GetValue<string>().ShouldBe("Footer");
    }

    [Theory]
    [InlineData("""[{"type":"internal","unique":null,"url":"/features/","name":"Features"}]""", false)]
    [InlineData("""[{"type":"document","name":"Features"}]""", false)]
    [InlineData("""[{"type":"Document","unique":"a2807f32-ecd8-4110-9167-7dafeecd15c9"}]""", false)]
    [InlineData("""[{"type":"external","name":"Example"}]""", false)]
    [InlineData("""{"type":"external","url":"https://example.com"}""", false)]
    [InlineData("""[{"type":"document","unique":"a2807f32-ecd8-4110-9167-7dafeecd15c9","name":"Features"},{"type":"external","url":"https://example.com"}]""", true)]
    [InlineData("""[]""", true)]
    [InlineData("""[{"type":"external","url":"","queryString":"#contact","name":"Contact"}]""", true)]
    [InlineData("""[{"icon":"icon-document","name":"Features","published":true,"queryString":null,"target":null,"trashed":false,"type":"document","unique":"a2807f32-ecd8-4110-9167-7dafeecd15c9","url":"/features/","culture":null}]""", true)]
    public void MultiUrlPicker_ValidateSetValue_RequiresATargetForEveryLink(string json, bool expectedValid)
    {
        var handler = new MultiUrlPickerPropertyValueHandler();

        var result = handler.ValidateSetValue(null, JsonNode.Parse(json), BuildContext());

        result.IsValid.ShouldBe(expectedValid);
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
