using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class SingleEditorHandlerTests
{
    private static readonly IAIPropertyValueHandler[] Handlers =
    [
        new MediaPicker3PropertyValueHandler(),
        new SingleMediaPickerPropertyValueHandler(),
        new MultiUrlPickerPropertyValueHandler(),
        new SingleUrlPickerPropertyValueHandler(),
    ];

    [Theory]
    [InlineData(CmsConstants.PropertyEditors.Aliases.MediaPicker3, typeof(MediaPicker3PropertyValueHandler))]
    [InlineData(CmsConstants.PropertyEditors.Aliases.SingleMediaPicker, typeof(SingleMediaPickerPropertyValueHandler))]
    [InlineData(CmsConstants.PropertyEditors.Aliases.MultiUrlPicker, typeof(MultiUrlPickerPropertyValueHandler))]
    [InlineData(CmsConstants.PropertyEditors.Aliases.SingleUrlPicker, typeof(SingleUrlPickerPropertyValueHandler))]
    public void Collection_ResolvesHandlerByEditorAlias(string alias, Type expected)
    {
        var collection = new AIPropertyValueHandlerCollection(() => Handlers);

        collection.GetByEditorSchemaAlias(alias)!.GetType().ShouldBe(expected);
    }

    [Fact]
    public void SingleMediaPicker_UsesTheNewAlias()
        => new SingleMediaPickerPropertyValueHandler().ForPropertyEditorSchemaAlias.ShouldBe("Umbraco.MediaPicker.Single");

    [Fact]
    public void SingleUrlPicker_UsesTheNewAlias()
        => new SingleUrlPickerPropertyValueHandler().ForPropertyEditorSchemaAlias.ShouldBe("Umbraco.UrlPicker.Single");

    [Fact]
    public void SingleMediaPicker_ValidateAddItem_RequiresMediaKey()
    {
        var result = new SingleMediaPickerPropertyValueHandler()
            .ValidateAddItem(null, new AIAddItemArgs(Values: new JsonObject()), null!);

        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.SchemaMismatch);
    }

    [Fact]
    public async Task SingleMediaPicker_AddItem_StoresSameShapeAsMediaPicker3()
    {
        var mediaKey = Guid.NewGuid();
        var args = new AIAddItemArgs(Values: new JsonObject { ["mediaKey"] = mediaKey });

        var result = await new SingleMediaPickerPropertyValueHandler().AddItemAsync(null, args, null!);

        var array = (JsonArray)result.Value!;
        array.Count.ShouldBe(1);
        array[0]!["mediaKey"]!.GetValue<Guid>().ShouldBe(mediaKey);
        array[0]!["key"]!.GetValue<Guid>().ShouldBe(result.BlockKey);
    }

    [Fact]
    public void SingleMediaPicker_ValidateAddItem_RejectsASecondItem()
    {
        var existing = new JsonArray { new JsonObject { ["key"] = Guid.NewGuid(), ["mediaKey"] = Guid.NewGuid() } };
        var args = new AIAddItemArgs(Values: new JsonObject { ["mediaKey"] = Guid.NewGuid() });

        var result = new SingleMediaPickerPropertyValueHandler().ValidateAddItem(existing, args, null!);

        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.SchemaMismatch);
    }

    [Fact]
    public void MediaPicker3_ValidateAddItem_AllowsASecondItem()
    {
        var existing = new JsonArray { new JsonObject { ["key"] = Guid.NewGuid(), ["mediaKey"] = Guid.NewGuid() } };
        var args = new AIAddItemArgs(Values: new JsonObject { ["mediaKey"] = Guid.NewGuid() });

        new MediaPicker3PropertyValueHandler().ValidateAddItem(existing, args, null!).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SingleUrlPicker_ValidateAddItem_RejectsASecondLink()
    {
        var existing = new JsonArray { new JsonObject { ["key"] = Guid.NewGuid(), ["type"] = "external" } };
        var args = new AIAddItemArgs(Values: new JsonObject { ["type"] = "external", ["url"] = "https://example.com" });

        var result = new SingleUrlPickerPropertyValueHandler().ValidateAddItem(existing, args, null!);

        result.IsValid.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.SchemaMismatch);
    }

    [Fact]
    public void MultiUrlPicker_ValidateAddItem_AllowsASecondLink()
    {
        var existing = new JsonArray { new JsonObject { ["key"] = Guid.NewGuid(), ["type"] = "external" } };
        var args = new AIAddItemArgs(Values: new JsonObject { ["type"] = "external", ["url"] = "https://example.com" });

        new MultiUrlPickerPropertyValueHandler().ValidateAddItem(existing, args, null!).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task SingleUrlPicker_AddItem_AcceptsExternalLink()
    {
        var args = new AIAddItemArgs(Values: new JsonObject
        {
            ["type"] = "external",
            ["url"] = "https://example.com",
            ["name"] = "Example",
        });

        var result = await new SingleUrlPickerPropertyValueHandler().AddItemAsync(null, args, null!);

        var array = (JsonArray)result.Value!;
        array.Count.ShouldBe(1);
        array[0]!["url"]!.GetValue<string>().ShouldBe("https://example.com");
    }

    [Fact]
    public async Task SingleUrlPicker_SetItemPropertyValue_RejectsNonEditableField()
    {
        var key = Guid.NewGuid();
        var existing = new JsonArray { new JsonObject { ["key"] = key, ["type"] = "external" } };

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await new SingleUrlPickerPropertyValueHandler().SetItemPropertyValueAsync(
                existing, key, "type", JsonValue.Create("media"), variantId: null, null!));
    }
}
