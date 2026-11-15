using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;

using Umbraco.Cms.Core.Models;

namespace Umbraco.Abilities.Tests;

/// <summary>The value tree and the checks on it that need no product.</summary>
public class EntityDocumentTests
{
    private static readonly ItemTypeRegistry ItemTypes = new ItemTypeRegistry()
        .Add(new ItemTypeDefinition("blocks", "hero", "Hero", "A hero.", ElementShape.Of(
            new FieldDefinition("heading", "Heading.", FieldType.String) { Variations = ContentVariation.Culture },
            FieldDefinition.ObjectOf("settings", "Settings.", ElementShape.Of(new FieldDefinition("theme", "Theme.", FieldType.String))))));

    private static readonly ElementShape Page = ElementShape.Of(
        FieldDefinition.ItemsOf("blocks", "Blocks.", "blocks"),
        new FieldDefinition("data", "Any JSON.", FieldType.Json));

    [Fact]
    public void A_path_goes_through_items_by_key_and_objects_by_field_in_the_variant_each_value_varies_by()
    {
        var key = Guid.NewGuid();
        var root = JsonNode.Parse($$"""
            { "blocks": [ { "key": "{{key}}", "type": "hero", "heading": { "en-US": "Hi", "da-DK": "Hej" }, "settings": { "theme": "dark" } } ] }
            """)!.AsObject();

        PathTarget Find(string path) => EntityValues.Find(root, Page, ItemTypes, path, (field, _) => new VariantId("da-DK", null).Narrow(field.Variations), create: false);

        Assert.Equal("Hej", Find($"blocks/{key}/heading").Value!.GetValue<string>());
        Assert.Equal("dark", Find($"blocks/{key}/settings/theme").Value!.GetValue<string>());
        Assert.Equal(key, Find($"blocks/{key}").ItemKey);
        Assert.Throws<AbilityInputException>(() => Find("blocks/0/heading")); // never by position
    }

    [Fact]
    public void A_JSON_value_shaped_like_items_is_a_value_because_the_structure_says_so()
    {
        var root = JsonNode.Parse($$"""{ "data": [ { "key": "{{Guid.NewGuid()}}", "type": "hero" } ] }""")!.AsObject();

        var values = EntityValues.Walk(root, Page, ItemTypes).ToList();

        Assert.Equal(["data"], values.Select(v => v.Path));
        Assert.Equal(FieldType.Json, values[0].Field.Type);
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
        after.Root["blocks"] = new JsonArray(EntityValues.NewItem(Guid.NewGuid(), "text"));
        Assert.Empty(EntityAccess.NewItemTypesNotAvailable(null, after, shape, itemTypes, restricted));

        after.Root["grid"] = new JsonArray(EntityValues.NewItem(Guid.NewGuid(), "text"));
        Assert.Equal(["text"], EntityAccess.NewItemTypesNotAvailable(null, after, shape, itemTypes, restricted));
    }
}
