using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Tests;

/// <summary>The working copy and the checks on it that need no product.</summary>
public class EntityDocumentTests
{
    [Fact]
    public void A_copy_keeps_a_JSON_value_that_looks_like_items_as_a_value()
    {
        var element = new EntityElement();
        element.Fields["data"] = JsonNode.Parse($$"""[{ "key": "{{Guid.NewGuid()}}", "type": "row" }]""");
        element.ItemsOf("blocks").Add(new EntityItem { Key = Guid.NewGuid(), Type = "hero", Settings = new EntityElement() });

        var copy = EntityJson.Clone(element);

        Assert.True(JsonNode.DeepEquals(element.Fields["data"], copy.Fields["data"]));
        Assert.Equal(["blocks"], copy.Items.Keys);
        Assert.NotSame(element.Items["blocks"][0], copy.Items["blocks"][0]);
        Assert.NotNull(copy.Items["blocks"][0].Settings);
    }

    [Fact]
    public void An_item_type_is_checked_in_its_own_fields_group_when_its_alias_is_in_several()
    {
        var restricted = Guid.NewGuid();
        var itemTypes = new ItemTypeRegistry()
            .Add(new ItemTypeDefinition("grid", "text", "Text", "Text in a grid.", ElementShape.Of()) { AvailableTo = user => user != restricted })
            .Add(new ItemTypeDefinition("list", "text", "Text", "Text in a list.", ElementShape.Of()));
        var shape = ElementShape.Of(FieldDefinition.ItemsOf("blocks", "Blocks.", "list"), FieldDefinition.ItemsOf("grid", "Grid.", "grid"));

        var after = new EntityDocument { EntityType = "page", Key = Guid.NewGuid() };
        after.Root.ItemsOf("blocks").Add(new EntityItem { Key = Guid.NewGuid(), Type = "text" });
        Assert.Empty(EntityAccess.NewItemTypesNotAvailable(null, after, shape, itemTypes, restricted));

        after.Root.ItemsOf("grid").Add(new EntityItem { Key = Guid.NewGuid(), Type = "text" });
        Assert.Equal(["text"], EntityAccess.NewItemTypesNotAvailable(null, after, shape, itemTypes, restricted));
    }
}
