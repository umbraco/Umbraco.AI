using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>Draft sets that span products: a page, a discount, a workflow and recorded operations, applied as one.</summary>
public class DraftSetTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public DraftSetTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task A_campaign_spanning_content_commerce_and_operations_is_applied_together()
    {
        await _agent.StartDraftSetAsync("Black Friday");
        var page = Guid.NewGuid();
        var discount = Guid.NewGuid();

        // A page with nested blocks: a card grid holding a card is a slot inside an item.
        await _agent.OkAsync("entity_create", $$"""{ "entityType": "cms.page", "key": "{{page}}", "culture": "en-US", "fields": { "title": "Black Friday" } }""");
        var hero = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "blocks", "itemType": "hero", "culture": "en-US", "fields": { "heading": "Black Friday deals" } }
            """);
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "elementPath": "{{hero.GetProperty("path").GetString()}}/@settings", "field": "theme", "value": "dark" }
            """);
        var grid = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "blocks", "itemType": "cardGrid" }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "{{grid.GetProperty("path").GetString()}}/cards", "itemType": "card", "fields": { "title": "20% off everything" } }
            """);

        // A discount pointing at the page created in the same set, using the key the agent chose.
        await _agent.OkAsync("entity_create", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "fields": { "name": "Black Friday", "landingPage": "{{page}}" } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 0 } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "percentageOff", "fields": { "percent": 20 } }
            """);

        // A plain operation is recorded, not run.
        var recorded = await _agent.OkAsync("commerce_activateDiscount", $$"""{ "discount": "{{discount}}" }""");
        Assert.True(recorded.GetProperty("runsWhenApplied").GetBoolean());

        var summary = await _agent.OkAsync("draftSet_summarize", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.Equal(2, summary.GetProperty("entities").GetArrayLength());
        Assert.Equal("commerce.activateDiscount", summary.GetProperty("operationsOnApply")[0].GetString());
        Assert.Empty(_site.Database.All("cms.page"));

        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");

        var livePage = (await _site.Pages.LoadAsync(page))!;
        var liveGrid = (JsonObject)Element.Slot(livePage.Root, "blocks")[1]!;
        Assert.Equal("20% off everything", Element.Fields((JsonObject)Element.Slot(liveGrid, "cards")[0]!)["title"]!.GetValue<string>());

        var liveDiscount = (await _site.Discounts.LoadAsync(discount))!;
        Assert.Equal(page.ToString(), Element.Fields(liveDiscount.Root)["landingPage"]!.GetValue<string>());
        Assert.True(Element.Fields(liveDiscount.Root)["active"]!.GetValue<bool>()); // the recorded operation ran after the saves
    }

    [Fact]
    public async Task If_anything_fails_while_applying_nothing_is_applied()
    {
        await _agent.StartDraftSetAsync("Will fail");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("email_send", """{ "to": "list@bounce.test", "subject": "We launched" }""");

        var failure = await _agent.FailAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.Equal("Runtime", failure.GetProperty("category").GetString());
        Assert.Contains("Nothing was applied", failure.GetProperty("message").GetString());

        Assert.Null(await _site.Pages.LoadAsync(page));
        Assert.Empty(_site.Outbox.Sent);
    }

    [Fact]
    public async Task Applying_is_refused_when_live_data_changed_underneath_the_set()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartDraftSetAsync("Rename");
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "", "field": "name", "value": "Renamed in set" }
            """);

        // Someone saves the discount directly while the set is open.
        var meanwhile = (await _site.Discounts.LoadAsync(discount))!;
        Element.Fields(meanwhile.Root)["name"] = "Changed live";
        await _site.Discounts.SaveAsync(meanwhile);

        var failure = await _agent.FailAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.Equal($"commerce.discount:{discount}", failure.GetProperty("problems")[0].GetProperty("path").GetString());
        Assert.Equal("Changed live", _site.Discounts.ToRef((await _site.Discounts.LoadAsync(discount))!).Name);
    }

    [Fact]
    public async Task Reads_inside_a_set_show_its_edits_and_reads_outside_show_live_data()
    {
        var discount = await CreateLiveDiscountAsync("Live name");

        var set = await _agent.StartDraftSetAsync("Rename");
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "", "field": "name", "value": "Draft name" }
            """);
        var inSet = await _agent.OkAsync("entity_get", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}" }""");
        Assert.Equal("Draft name", inSet.GetProperty("fields").GetProperty("name").GetString());
        Assert.Single((await _agent.OkAsync("entity_find", """{ "entityType": "commerce.discount", "query": "Draft" }""")).EnumerateArray());

        _agent.DraftSetId = null;
        var live = await _agent.OkAsync("entity_get", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}" }""");
        Assert.Equal("Live name", live.GetProperty("fields").GetProperty("name").GetString());

        await _agent.OkAsync("draftSet_discard", $$"""{ "draftSetId": "{{set}}" }""");
        await _agent.FailAsync("draftSet_validate", $$"""{ "draftSetId": "{{set}}" }""");
    }

    [Fact]
    public async Task Replaying_a_set_gives_the_same_entity_every_time()
    {
        await _agent.StartDraftSetAsync("Replay");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Replay" } }""")).GetProperty("key").GetGuid();
        var hero = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "blocks", "itemType": "hero", "culture": "en-US", "fields": { "heading": "Hi" } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "blocks", "itemType": "hero", "culture": "en-US", "fields": { "heading": "First" }, "position": 0 }
            """);
        await _agent.OkAsync("entity_moveItem", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "slotPath": "blocks", "itemKey": "{{hero.GetProperty("itemKey").GetGuid()}}", "position": 0 }
            """);

        var set = _site.DraftSets.Get(_agent.DraftSetId!.Value);
        var first = (await _site.DraftSets.GetWorkingStateAsync(set, "cms.page", page))!.ToJson().ToJsonString();
        var second = (await _site.DraftSets.GetWorkingStateAsync(set, "cms.page", page))!.ToJson().ToJsonString();
        Assert.Equal(first, second);

        // The key the agent was given is the key that is stored, so later calls can refer to it.
        var blocks = Element.Slot((await _site.DraftSets.GetWorkingStateAsync(set, "cms.page", page))!.Root, "blocks");
        Assert.Equal(hero.GetProperty("itemKey").GetGuid(), Element.ItemKey(blocks[0]!));
    }

    private async Task<Guid> CreateLiveDiscountAsync(string name)
    {
        var set = await _agent.StartDraftSetAsync("Seed");
        var discount = (await _agent.OkAsync("entity_create", $$"""{ "entityType": "commerce.discount", "fields": { "name": "{{name}}" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 50 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");
        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{set}}" }""");
        _agent.DraftSetId = null;
        return discount;
    }
}
