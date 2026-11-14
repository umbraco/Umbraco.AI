using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;
using Umbraco.Abilities.Samples;
using Umbraco.Abilities.Samples.Cms;
using Umbraco.Abilities.Samples.Commerce;

namespace Umbraco.Abilities.Tests;

/// <summary>What the framework works out from a package's model classes, so the package doesn't write it.</summary>
public class TypedModelTests
{
    private readonly SampleSite _site = SampleSite.Create();

    [Fact]
    public async Task The_structure_comes_from_the_model_class_and_its_attributes()
    {
        var discount = await _site.Discounts.DescribeAsync(null);
        var fields = discount.Shape.Fields.ToDictionary(f => f.Alias);

        Assert.True(fields["name"].Required);
        Assert.Equal(FieldType.Date, fields["startDate"].Type);
        Assert.Equal("cms.page", fields["landingPage"].ReferencesEntityType);
        Assert.Equal(1, discount.Shape.Slots.Single(s => s.Alias == "rules").Min);
        Assert.Equal(3, discount.Shape.Slots.Single(s => s.Alias == "rewards").Max);
        Assert.DoesNotContain("key", fields.Keys);
        Assert.DoesNotContain("version", fields.Keys);

        var customerGroup = _site.ItemTypes.Find(DiscountDefinition.RuleGroup, "customerGroup")!;
        Assert.Equal(["VIP", "Wholesale", "Retail"], customerGroup.Shape.Fields.Single().Allowed);
        Assert.Equal(typeof(CustomerGroupRule), customerGroup.ClrType);
    }

    [Fact]
    public async Task Varying_values_settings_layout_and_json_values_are_described_too()
    {
        var page = await _site.Pages.DescribeAsync(null);
        var fields = page.Shape.Fields.ToDictionary(f => f.Alias);

        Assert.Equal(Variation.Culture, page.Variations);
        Assert.Equal(["en-US", "da-DK"], page.Cultures);
        Assert.Equal(Variation.Culture, fields["title"].Variations);
        Assert.True(fields["tags"].Many);
        Assert.Equal(["cms.page", "cms.media"], fields["related"].ReferencesAnyOf);
        Assert.Equal(Variation.Culture, page.Shape.Slots.Single(s => s.Alias == "promos").Variations);
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
        var discount = new Discount
        {
            Key = Guid.NewGuid(),
            Rules = [new OrderTotalRule()],
            Rewards = [new PercentageOffReward { Percent = 150 }],
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
}
