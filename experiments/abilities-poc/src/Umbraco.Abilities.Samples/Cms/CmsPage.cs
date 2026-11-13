using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples.Cms;

/// <summary>
/// A culture-variant document type shaped like real CMS content, to check the structure language can carry it:
/// <list type="bullet">
/// <item>the name and some properties vary by culture, others are invariant;</item>
/// <item><c>blocks</c> is an invariant Block List whose blocks hold culture-variant values and have a settings element;</item>
/// <item><c>grid</c> is a Block Grid: column and row span are placement values, and areas are slots inside a block;</item>
/// <item><c>promos</c> is a culture-variant block property: a separate list per culture;</item>
/// <item><c>image</c> is a Media Picker: entries with their own key, a media reference and crop data;</item>
/// <item><c>tags</c> is a string array, and <c>related</c> a multi-node tree picker pointing at documents or media.</item>
/// </list>
/// A real definition would build this from the document type and data type configuration, and map it to
/// ContentCreateModel / ContentUpdateModel and block envelopes on save.
/// </summary>
public sealed class CmsPageDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "cms.page", "title", "/umbraco/section/content/workspace/document/edit")
{
    public static readonly IReadOnlyList<string> Cultures = ["en-US", "da-DK"];

    public const int GridColumns = 12;

    public const string BlockGroup = "cms.block";
    public const string CardGroup = "cms.card";
    public const string GridRootGroup = "cms.grid.root";
    public const string GridAreaGroup = "cms.grid.area";
    public const string PromoGroup = "cms.promo";
    public const string MediaEntryGroup = "cms.mediaEntry";

    public static void RegisterItemTypes(ItemTypeRegistry itemTypes)
    {
        // Block List blocks. The hero's heading varies by culture inside an invariant property, and it has a
        // settings element type, as blocks configured with SettingsElementTypeKey do.
        itemTypes.Add(new ItemTypeDefinition(BlockGroup, "hero", "Hero", "A large heading at the top of a page.", ElementShape.Of(
            new FieldDefinition("heading", "Main heading.", FieldType.String, Required: true) { Varies = Variance.Culture },
            new FieldDefinition("subheading", "Line under the heading.", FieldType.String) { Varies = Variance.Culture }))
        {
            SettingsShape = ElementShape.Of(
                new FieldDefinition("theme", "Colour theme.", FieldType.String, Required: true) { Allowed = ["light", "dark"] },
                new FieldDefinition("fullWidth", "Span the whole page width.", FieldType.Boolean)),
        });

        // A block with a block property of its own: a nested slot inside an item.
        itemTypes.Add(new ItemTypeDefinition(BlockGroup, "cardGrid", "Card grid", "A grid of cards.", ElementShape
            .Of(new FieldDefinition("heading", "Heading above the grid.", FieldType.String) { Varies = Variance.Culture })
            .WithSlots(new SlotDefinition("cards", "The cards in the grid.", CardGroup) { Min = 1 })));

        itemTypes.Add(new ItemTypeDefinition(CardGroup, "card", "Card", "A card with a title and text.", ElementShape.Of(
            new FieldDefinition("title", "Card title.", FieldType.String, Required: true),
            new FieldDefinition("text", "Card text.", FieldType.String))));

        // Block Grid. A layout block whose areas are slots; area limits are slot Min/Max.
        itemTypes.Add(new ItemTypeDefinition(GridRootGroup, "twoColumn", "Two columns", "A row with a left and a right area.", new ElementShape([],
        [
            new SlotDefinition("left", "Left area.", GridAreaGroup) { Min = 1, Max = 2 },
            new SlotDefinition("right", "Right area.", GridAreaGroup) { Max = 2 },
        ])));

        // Allowed both at the grid root and in areas (AllowAtRoot and AllowInAreas).
        itemTypes.Add(new ItemTypeDefinition(GridRootGroup, "richText", "Rich text", "A block of text.", ElementShape.Of(
            new FieldDefinition("text", "The text.", FieldType.String, Required: true) { Varies = Variance.Culture }))
        {
            AlsoIn = [GridAreaGroup],
        });

        itemTypes.Add(new ItemTypeDefinition(PromoGroup, "promo", "Promotion", "A promotion shown in one language.", ElementShape.Of(
            new FieldDefinition("text", "Promotion text.", FieldType.String, Required: true))));

        itemTypes.Add(new ItemTypeDefinition(MediaEntryGroup, "mediaEntry", "Media", "A picked media item.", ElementShape.Of(
            new FieldDefinition("media", "The media item.", FieldType.EntityReference, Required: true) { ReferencesEntityType = "cms.media" },
            new FieldDefinition("focalPoint", "Optional focal point.", FieldType.Json)
            {
                Schema = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject { ["left"] = new JsonObject { ["type"] = "number" }, ["top"] = new JsonObject { ["type"] = "number" } },
                },
            })));
    }

    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Page", "A content page.", ElementShape
            .Of(
                new FieldDefinition("title", "Page name.", FieldType.String, Required: true) { Varies = Variance.Culture },
                new FieldDefinition("urlSegment", "URL segment, lower case with hyphens.", FieldType.String) { Varies = Variance.Culture },
                new FieldDefinition("tags", "Tags.", FieldType.String) { Many = true },
                new FieldDefinition("related", "Related pages or media.", FieldType.EntityReference) { Many = true, ReferencesAnyOf = ["cms.page", "cms.media"] })
            .WithSlots(
                new SlotDefinition("blocks", "The page's content blocks.", BlockGroup),
                new SlotDefinition("grid", "A layout grid.", GridRootGroup)
                {
                    ItemLayout = ElementShape.Of(
                        new FieldDefinition("columnSpan", $"Columns the block spans, 1 to {GridColumns}.", FieldType.Integer, Required: true),
                        new FieldDefinition("rowSpan", "Rows the block spans.", FieldType.Integer)),
                },
                new SlotDefinition("promos", "Promotions, chosen per language.", PromoGroup) { Varies = Variance.Culture },
                new SlotDefinition("image", "The page image.", MediaEntryGroup) { Max = 1 }))
        {
            Varies = Variance.Culture,
            Cultures = Cultures,
        });

    public override Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        var problems = new List<Problem>();
        foreach (var item in Element.Slot(document.Root, "grid").OfType<JsonObject>())
        {
            if (item["layout"]?["fields"]?["columnSpan"] is JsonValue span
                && span.TryGetValue<int>(out var columns)
                && columns is < 1 or > GridColumns)
            {
                problems.Add(new Problem($"grid/{Element.ItemKey(item)}/@layout/columnSpan", $"Must be between 1 and {GridColumns}."));
            }
        }

        return Task.FromResult<IReadOnlyList<Problem>>(problems);
    }
}

/// <summary>Media items, so pickers and references can point at media created in the same draft set.</summary>
public sealed class CmsMediaDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "cms.media", "name", "/umbraco/section/media/workspace/media/edit")
{
    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Media", "An image or file.", ElementShape.Of(
            new FieldDefinition("name", "Media name.", FieldType.String, Required: true),
            new FieldDefinition("altText", "Alternative text.", FieldType.String))));
}
