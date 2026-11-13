using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples.Cms;

/// <summary>
/// A CMS page with blocks. In a real build this definition would sit on IContentEditingService and the
/// document type; nested blocks are slots inside items, as Block List and Block Grid are today.
/// </summary>
public sealed class CmsPageDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "cms.page", "title", "/umbraco/section/content/workspace/document/edit")
{
    public const string BlockGroup = "cms.block";
    public const string CardGroup = "cms.card";

    public static void RegisterItemTypes(ItemTypeRegistry itemTypes)
    {
        itemTypes.Add(new ItemTypeDefinition(BlockGroup, "hero", "Hero", "A large heading at the top of a page.", ElementShape.Of(
            new FieldDefinition("heading", "Main heading.", FieldType.String, Required: true),
            new FieldDefinition("subheading", "Line under the heading.", FieldType.String))));

        itemTypes.Add(new ItemTypeDefinition(BlockGroup, "cardGrid", "Card grid", "A grid of cards.", ElementShape
            .Of(new FieldDefinition("heading", "Heading above the grid.", FieldType.String))
            .WithSlots(new SlotDefinition("cards", "The cards in the grid.", CardGroup) { Min = 1 })));

        itemTypes.Add(new ItemTypeDefinition(CardGroup, "card", "Card", "A card with a title and text.", ElementShape.Of(
            new FieldDefinition("title", "Card title.", FieldType.String, Required: true),
            new FieldDefinition("text", "Card text.", FieldType.String))));
    }

    public override Task<EntityStructure> DescribeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Page", "A content page.", ElementShape
            .Of(
                new FieldDefinition("title", "Page title.", FieldType.String, Required: true),
                new FieldDefinition("urlSegment", "URL segment, lower case with hyphens.", FieldType.String))
            .WithSlots(new SlotDefinition("blocks", "The page's content blocks.", BlockGroup))));
}
