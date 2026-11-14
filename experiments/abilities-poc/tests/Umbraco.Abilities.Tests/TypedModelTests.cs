using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;
using Umbraco.Abilities.Samples;
using Umbraco.Abilities.Samples.Cms;
using Umbraco.Abilities.Samples.Commerce;
using Umbraco.Abilities.Samples.Commerce.Domain;

using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
namespace Umbraco.Abilities.Tests;

/// <summary>What the framework works out from a package's model classes, so the package doesn't write it.</summary>
public class TypedModelTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public TypedModelTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task The_structure_comes_from_the_model_class_and_its_attributes()
    {
        var discount = await _site.Discounts.DescribeAsync(null);
        var fields = discount.Shape.Fields.ToDictionary(f => f.Alias);

        Assert.True(fields["name"].Required);
        Assert.Equal(FieldType.Date, fields["startDate"].Type);
        Assert.Equal("document", fields["landingPage"].ReferencesEntityType);
        Assert.Equal(1, discount.Shape.Slots.Single(s => s.Alias == "rules").Min);
        Assert.Equal(3, discount.Shape.Slots.Single(s => s.Alias == "rewards").Max);
        Assert.DoesNotContain("key", fields.Keys);
        Assert.DoesNotContain("version", fields.Keys);

        var customerGroup = _site.ItemTypes.Find(DiscountDefinition.RuleGroup, "customerGroup")!;
        Assert.Equal(["VIP", "Wholesale", "Retail"], customerGroup.Shape.Fields.Single().Allowed);
        Assert.Null(customerGroup.ClrType); // from Commerce's provider collection, not a class
    }

    [Fact]
    public async Task Varying_values_settings_layout_and_json_values_are_described_too()
    {
        var page = await _site.Pages.DescribeAsync(null);
        var fields = page.Shape.Fields.ToDictionary(f => f.Alias);

        Assert.Equal(ContentVariation.Culture, page.Variations);
        Assert.Equal(["en-US", "da-DK"], page.Cultures);
        Assert.Equal(ContentVariation.Culture, fields["title"].Variations);
        Assert.True(fields["tags"].Many);
        Assert.Equal(["document", "media"], fields["related"].ReferencesAnyOf);
        Assert.Equal(ContentVariation.Culture, page.Shape.Slots.Single(s => s.Alias == "promos").Variations);
        Assert.Equal("columnSpan", page.Shape.Slots.Single(s => s.Alias == "grid").ItemLayout!.Fields[0].Alias);

        var hero = _site.ItemTypes.Find(CmsPageDefinition.BlockGroup, "hero")!;
        Assert.Equal(["light", "dark"], hero.SettingsShape!.Fields.Single(f => f.Alias == "theme").Allowed);

        var focalPoint = _site.ItemTypes.Find(CmsPageDefinition.MediaEntryGroup, "mediaEntry")!.Shape.Fields.Single(f => f.Alias == "focalPoint");
        Assert.Equal(FieldType.Json, focalPoint.Type);
        Assert.NotNull(focalPoint.Schema!["properties"]!["left"]);
    }

    [Fact]
    public async Task A_model_round_trips_through_its_document_with_item_keys_and_paths()
    {
        var page = new Page
        {
            Key = Guid.NewGuid(),
            Title = { ["en-US"] = "Launch", ["da-DK"] = "Lancering" },
            Blocks = [new Hero { Heading = { ["en-US"] = "Hello" }, Settings = new HeroSettings { Theme = "dark" } }],
            Grid = [new RichText { Text = { ["en-US"] = "Body" }, Placement = new GridPlacement { ColumnSpan = 6 } }],
            Promos = { ["da-DK"] = [new Promo { Text = "Udsalg" }] },
            Image = [new MediaEntry { Media = Guid.NewGuid(), FocalPoint = new FocalPoint(0.5, 0.25) }],
        };

        var document = _site.Pages.ToDocument(page);
        var copy = _site.Pages.ToModel(document);

        Assert.Equal("Lancering", copy.Title["da-DK"]);
        var hero = Assert.IsType<Hero>(Assert.Single(copy.Blocks));
        Assert.Equal(page.Blocks[0].Key, hero.Key); // a key was assigned on the way in and kept
        Assert.Equal($"blocks/{hero.Key}", hero.Path);
        Assert.Equal("dark", hero.Settings!.Theme);
        Assert.Equal(6, copy.Grid[0].Placement!.ColumnSpan);
        Assert.Equal("Udsalg", copy.Promos["da-DK"][0].Text);
        Assert.Equal(new FocalPoint(0.5, 0.25), copy.Image[0].FocalPoint);
    }

    [Fact]
    public async Task Each_problem_is_reported_once_whether_it_comes_from_the_structure_or_an_attribute()
    {
        var discount = new DiscountEditModel
        {
            Key = Guid.NewGuid(),
            Rules = [new DynamicItem { Type = "orderTotal" }],
            Rewards = [new DynamicItem { Type = "percentageOff", Fields = { ["percent"] = 150 } }],
            StartDate = new DateOnly(2026, 12, 31),
            EndDate = new DateOnly(2026, 12, 1),
        };
        var document = _site.Discounts.ToDocument(discount);
        var structure = await _site.Discounts.DescribeAsync(null);

        IReadOnlyList<Problem> problems =
        [
            .. await StructureValidator.ValidateAsync(document, structure, _site.ItemTypes, (_, _) => Task.FromResult(true)),
            .. await _site.Discounts.ValidateAsync(document),
        ];

        Assert.Equal(
            ["endDate", "name", $"rewards/{discount.Rewards[0].Key}/percent", $"rules/{discount.Rules[0].Key}/minTotal"],
            problems.Select(p => p.Path).Order());
    }

    [Fact]
    public async Task A_class_with_no_attributes_or_base_class_is_described_fluently()
    {
        var media = await _site.Media.DescribeAsync(null);
        var fields = media.Shape.Fields.ToDictionary(f => f.Alias);

        Assert.Equal("media", media.EntityType);
        Assert.True(fields["name"].Required);
        Assert.Equal(["name", "altText"], fields.Keys); // the key, version and server-side UpdateDate are not fields
        Assert.Equal(CmsMediaDefinition.CropGroup, Assert.Single(media.Shape.Slots).ItemTypeGroup);

        var crop = _site.ItemTypes.Find(CmsMediaDefinition.CropGroup, "crop")!;
        Assert.Equal(typeof(MediaCropRequestModel), crop.ClrType);
        Assert.All(crop.Shape.Fields, f => Assert.True(f.Required));

        // Items keep their own key property and get a path without deriving from EntityItemModel.
        var request = new MediaRequestModel { Id = Guid.NewGuid(), Name = "Hero", Crops = [new MediaCropRequestModel { Alias = "wide", Width = 0, Height = 600 }] };
        var copy = _site.Media.ToModel(_site.Media.ToDocument(request));
        var key = request.Crops[0].Id;
        Assert.NotEqual(Guid.Empty, key);
        Assert.Equal(key, copy.Crops[0].Id);
        Assert.Equal($"crops/{key}", ModelShapes.PathOf(copy.Crops[0]));

        // A fluent rule is reported on the item's path, like an attribute.
        var problems = await _site.Media.ValidateAsync(_site.Media.ToDocument(copy));
        Assert.Equal([$"crops/{key}/width"], problems.Select(p => p.Path));
    }

    [Fact]
    public async Task A_fluently_described_entity_is_created_from_text_like_any_other()
    {
        await _agent.StartChangeSetAsync("Media");
        var media = (await _agent.OkAsync("entity_create", """{ "entityType": "media", "fields": { "name": "Hero image" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "media", "key": "{{media}}", "slotPath": "crops", "itemType": "crop", "fields": { "alias": "wide", "width": 1600, "height": 600 } }
            """);
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        var live = (await _site.Media.LoadModelAsync(media))!;
        Assert.Equal("Hero image", live.Name);
        Assert.Equal(1600, Assert.Single(live.Crops).Width);
    }

    [Fact]
    public async Task Commerce_keeps_its_own_model_and_the_definition_maps_to_it()
    {
        await _agent.StartChangeSetAsync("Sale");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Sale", "endDate": "2026-12-31" } }""")).GetProperty("key").GetGuid();
        var rule = (await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 50 } }""")).GetProperty("itemKey").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        // Stored in Commerce's own shape: a provider alias and string settings, UTC dates.
        var stored = _site.DiscountService.GetDiscount(discount)!;
        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), stored.ExpiryDate);
        Assert.Equal("orderTotal", stored.Rules[0].RuleProviderAlias);
        Assert.Equal("50", stored.Rules[0].Settings["minTotal"]);

        // Commerce's rules have no ids, so keys are derived: stable across loads, but not the key the agent was given.
        var first = (await _site.Discounts.LoadModelAsync(discount))!.Rules[0].Key;
        var second = (await _site.Discounts.LoadModelAsync(discount))!.Rules[0].Key;
        Assert.Equal(first, second);
        Assert.NotEqual(rule, first);
    }
}
