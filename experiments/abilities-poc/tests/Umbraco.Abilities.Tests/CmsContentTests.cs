using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>
/// Checks the structure language carries the CMS content model: culture variance, blocks with culture-variant
/// values and settings, Block Grid placement and areas, culture-variant block properties, and pickers.
/// </summary>
public class CmsContentTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public CmsContentTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task Names_and_values_vary_by_culture_while_invariant_values_are_shared()
    {
        await _agent.StartDraftSetAsync("Summer page");
        var page = await CreatePageAsync("Summer", "en-US");

        await SetAsync(page, "", "title", "\"Sommer\"", "da-DK");
        await SetAsync(page, "", "urlSegment", "\"summer\"", "en-US");
        await SetAsync(page, "", "tags", """["sale", "summer"]""", "da-DK"); // tags are invariant: the culture is ignored

        var working = await GetAsync(page);
        Assert.Equal("Summer", working.GetProperty("variants").GetProperty("en-US").GetProperty("fields").GetProperty("title").GetString());
        Assert.Equal("Sommer", working.GetProperty("variants").GetProperty("da-DK").GetProperty("fields").GetProperty("title").GetString());
        Assert.Equal(2, working.GetProperty("fields").GetProperty("tags").GetArrayLength());
        Assert.False(working.GetProperty("variants").GetProperty("da-DK").GetProperty("fields").TryGetProperty("tags", out _));

        await AssertValidAsync();
        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.Equal("Summer", _site.Pages.ToRef((await _site.Pages.LoadAsync(page))!).Name);
    }

    [Fact]
    public async Task A_value_that_varies_by_culture_needs_a_known_culture()
    {
        await _agent.StartDraftSetAsync("Cultures");

        var missing = await _agent.FailAsync("entity_create", """{ "entityType": "cms.page", "fields": { "title": "No culture" } }""");
        Assert.True(missing.GetProperty("correctable").GetBoolean());
        Assert.Equal("title", missing.GetProperty("problems")[0].GetProperty("path").GetString());
        Assert.Contains("en-US, da-DK", missing.GetProperty("problems")[0].GetProperty("message").GetString());

        var unknown = await _agent.FailAsync("entity_create", """{ "entityType": "cms.page", "culture": "fr-FR", "fields": { "title": "Bonjour" } }""");
        Assert.Contains("Unknown culture 'fr-FR'", unknown.GetProperty("problems")[0].GetProperty("message").GetString());

        // A page with no culture at all can be started, but not applied.
        await _agent.OkAsync("entity_create", """{ "entityType": "cms.page" }""");
        var validation = await ValidateAsync();
        Assert.Contains("no culture yet", validation.GetProperty("entities")[0].GetProperty("problems")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Each_culture_is_validated_separately_and_problems_say_which()
    {
        await _agent.StartDraftSetAsync("Danish page");
        var page = await CreatePageAsync("Summer", "en-US");
        await SetAsync(page, "", "urlSegment", "\"sommer\"", "da-DK"); // starts a Danish variant with no name

        var problem = (await ValidateAsync()).GetProperty("entities")[0].GetProperty("problems")[0];
        Assert.Equal("title", problem.GetProperty("path").GetString());
        Assert.Equal("da-DK", problem.GetProperty("culture").GetString());

        await SetAsync(page, "", "title", "\"Sommer\"", "da-DK");
        await AssertValidAsync();
    }

    [Fact]
    public async Task Block_values_vary_per_culture_inside_an_invariant_block_list_and_settings_are_their_own_element()
    {
        await _agent.StartDraftSetAsync("Hero");
        var page = await CreatePageAsync("Summer", "en-US");
        await SetAsync(page, "", "title", "\"Sommer\"", "da-DK");

        var hero = (await AddItemAsync(page, "blocks", "hero", """{ "heading": "Summer sale" }""", "en-US")).GetProperty("path").GetString()!;
        await SetAsync(page, hero, "heading", "\"Sommerudsalg\"", "da-DK");

        // The settings element is required to have a theme, and is checked on its own path.
        var validation = await ValidateAsync();
        Assert.Equal([$"{hero}/@settings/theme"], JsonAssert.ProblemPaths(validation));

        var wrong = await _agent.FailAsync("entity_setValue", SetJson(page, $"{hero}/@settings", "theme", "\"neon\""));
        Assert.Equal($"{hero}/@settings/theme", wrong.GetProperty("problems")[0].GetProperty("path").GetString());
        await SetAsync(page, $"{hero}/@settings", "theme", "\"dark\"");
        await AssertValidAsync();

        var item = (await GetAsync(page)).GetProperty("slots").GetProperty("blocks")[0];
        Assert.Equal("Summer sale", item.GetProperty("variants").GetProperty("en-US").GetProperty("fields").GetProperty("heading").GetString());
        Assert.Equal("Sommerudsalg", item.GetProperty("variants").GetProperty("da-DK").GetProperty("fields").GetProperty("heading").GetString());
        Assert.Equal("dark", item.GetProperty("settings").GetProperty("fields").GetProperty("theme").GetString());

        // A Block List has no placement values.
        var noLayout = await _agent.FailAsync("entity_setValue", SetJson(page, $"{hero}/@layout", "columnSpan", "6"));
        Assert.Contains("has no layout values", noLayout.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Grid_blocks_have_placement_values_and_areas_that_are_slots()
    {
        await _agent.StartDraftSetAsync("Grid");
        var page = await CreatePageAsync("Grid page", "en-US");

        var row = (await AddItemAsync(page, "grid", "twoColumn", null)).GetProperty("path").GetString()!;
        await SetAsync(page, $"{row}/@layout", "columnSpan", "13");

        // Area contents: only types allowed in areas, up to the area's maximum.
        await AddItemAsync(page, $"{row}/left", "richText", """{ "text": "One" }""", "en-US");
        await AddItemAsync(page, $"{row}/left", "richText", """{ "text": "Two" }""", "en-US");
        var full = await _agent.FailAsync("entity_addItem", AddJson(page, $"{row}/left", "richText", """{ "text": "Three" }""", "en-US"));
        Assert.Contains("at most 2", full.GetProperty("message").GetString());
        var notAllowed = await _agent.FailAsync("entity_addItem", AddJson(page, $"{row}/right", "twoColumn", null));
        Assert.Contains("not allowed in this slot", notAllowed.GetProperty("message").GetString());

        // richText is allowed at the root too, and every root block needs a column span.
        var text = (await AddItemAsync(page, "grid", "richText", """{ "text": "Root" }""", "en-US")).GetProperty("path").GetString()!;

        var paths = JsonAssert.ProblemPaths(await ValidateAsync());
        Assert.Contains($"{text}/@layout/columnSpan", paths);  // required, missing
        Assert.Contains($"{row}/@layout/columnSpan", paths);   // 13 is wider than the grid

        await SetAsync(page, $"{row}/@layout", "columnSpan", "12");
        await SetAsync(page, $"{text}/@layout", "columnSpan", "6");
        await AssertValidAsync();
    }

    [Fact]
    public async Task A_culture_variant_block_property_holds_a_list_per_culture()
    {
        await _agent.StartDraftSetAsync("Promos");
        var page = await CreatePageAsync("Shop", "en-US");
        await SetAsync(page, "", "title", "\"Butik\"", "da-DK");

        await AddItemAsync(page, "promos", "promo", """{ "text": "Free shipping" }""", "en-US");
        await AddItemAsync(page, "promos", "promo", """{ "text": "Gratis fragt" }""", "da-DK");
        await AddItemAsync(page, "promos", "promo", """{ "text": "Ny kollektion" }""", "da-DK");

        var variants = (await GetAsync(page)).GetProperty("variants");
        Assert.Equal(1, variants.GetProperty("en-US").GetProperty("slots").GetProperty("promos").GetArrayLength());
        Assert.Equal(2, variants.GetProperty("da-DK").GetProperty("slots").GetProperty("promos").GetArrayLength());

        var noCulture = await _agent.FailAsync("entity_addItem", AddJson(page, "promos", "promo", """{ "text": "?" }""", null));
        Assert.Equal("promos", noCulture.GetProperty("problems")[0].GetProperty("path").GetString());
        await AssertValidAsync();
    }

    [Fact]
    public async Task Pickers_refer_to_media_and_pages_created_in_the_same_set()
    {
        await _agent.StartDraftSetAsync("Media");
        var media = Guid.NewGuid();
        await _agent.OkAsync("entity_create", $$"""{ "entityType": "cms.media", "key": "{{media}}", "fields": { "name": "Beach.jpg" } }""");

        var page = await CreatePageAsync("Beach", "en-US");
        await AddItemAsync(page, "image", "mediaEntry", $$"""{ "media": "{{media}}", "focalPoint": { "left": 0.5, "top": 0.3 } }""");

        var missingPage = Guid.NewGuid();
        await SetAsync(page, "", "related", $$"""[{ "entityType": "cms.media", "key": "{{media}}" }, { "entityType": "cms.page", "key": "{{missingPage}}" }]""");
        var problem = (await ValidateAsync()).GetProperty("entities")[0].GetProperty("problems")[0];
        Assert.Equal("related", problem.GetProperty("path").GetString());
        Assert.Contains(missingPage.ToString(), problem.GetProperty("message").GetString());

        var wrongType = await _agent.FailAsync("entity_setValue", SetJson(page, "", "related", $$"""[{ "entityType": "commerce.discount", "key": "{{media}}" }]"""));
        Assert.True(wrongType.GetProperty("correctable").GetBoolean());

        await SetAsync(page, "", "related", $$"""[{ "entityType": "cms.media", "key": "{{media}}" }]""");
        await AssertValidAsync();
        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");

        var live = (await _site.Pages.LoadAsync(page))!;
        Assert.Equal(media.ToString(), Element.Fields((JsonObject)Element.Slot(live.Root, "image")[0]!)["media"]!.GetValue<string>());
        Assert.NotNull(await _site.Media.LoadAsync(media));
    }

    private async Task<Guid> CreatePageAsync(string title, string culture)
        => (await _agent.OkAsync("entity_create", $$"""{ "entityType": "cms.page", "culture": "{{culture}}", "fields": { "title": "{{title}}" } }""")).GetProperty("key").GetGuid();

    private Task<JsonElement> SetAsync(Guid page, string elementPath, string field, string valueJson, string? culture = null)
        => _agent.OkAsync("entity_setValue", SetJson(page, elementPath, field, valueJson, culture));

    private Task<JsonElement> AddItemAsync(Guid page, string slotPath, string itemType, string? fieldsJson, string? culture = null)
        => _agent.OkAsync("entity_addItem", AddJson(page, slotPath, itemType, fieldsJson, culture));

    private Task<JsonElement> GetAsync(Guid page) => _agent.OkAsync("entity_get", $$"""{ "entityType": "cms.page", "key": "{{page}}" }""");

    private Task<JsonElement> ValidateAsync() => _agent.OkAsync("draftSet_validate", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");

    private async Task AssertValidAsync()
    {
        var validation = await ValidateAsync();
        Assert.True(validation.GetProperty("valid").GetBoolean(), validation.ToString());
    }

    private static string SetJson(Guid page, string elementPath, string field, string valueJson, string? culture = null)
        => $$"""{ "entityType": "cms.page", "key": "{{page}}", "elementPath": "{{elementPath}}", "field": "{{field}}", "value": {{valueJson}}{{Culture(culture)}} }""";

    private static string AddJson(Guid page, string slotPath, string itemType, string? fieldsJson, string? culture = null)
        => $$"""{ "entityType": "cms.page", "key": "{{page}}", "slotPath": "{{slotPath}}", "itemType": "{{itemType}}"{{(fieldsJson is null ? "" : $", \"fields\": {fieldsJson}")}}{{Culture(culture)}} }""";

    private static string Culture(string? culture) => culture is null ? "" : $", \"culture\": \"{culture}\"";
}
