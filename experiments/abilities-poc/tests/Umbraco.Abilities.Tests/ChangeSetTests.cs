using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>Change sets that span products: a page, a discount, a workflow and recorded operations, applied as one.</summary>
public class ChangeSetTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public ChangeSetTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task A_campaign_spanning_content_commerce_and_a_side_effect_is_applied_together()
    {
        await _agent.StartChangeSetAsync("Black Friday");
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

        // Activating the discount is a change to it, not a side effect: it shows in the discount's working state
        // before anything is applied, and is saved with the discount in Commerce's step.
        await _agent.OkAsync("commerce_activateDiscount", $$"""{ "discount": "{{discount}}" }""");
        var working = await _agent.OkAsync("entity_get", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}" }""");
        Assert.True(working.GetProperty("fields").GetProperty("active").GetBoolean());

        // A side effect is kept apart, to run once every change is applied.
        var email = await _agent.OkAsync("email_send", """{ "to": "list@example.com", "subject": "Black Friday is live" }""");
        Assert.True(email.GetProperty("runsAfterApply").GetBoolean());

        var summary = await _agent.OkAsync("changeSet_summarize", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal(2, summary.GetProperty("entities").GetArrayLength());
        Assert.Equal(["email.send"], summary.GetProperty("afterApply").EnumerateArray().Select(a => a.GetString()));
        Assert.Empty(_site.ContentDatabase.All("cms.page"));
        Assert.Empty(_site.Outbox.Sent);

        // The plan says how it will apply: each product in its own transaction, then the side effects.
        Assert.Equal(["Content", "Commerce", "email.send"], summary.GetProperty("applyPlan").EnumerateArray().Select(s => s.GetProperty("name").GetString()));

        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        var livePage = (await _site.Pages.LoadAsync(page))!;
        var liveGrid = (JsonObject)Element.Slot(livePage.Root, "blocks")[1]!;
        Assert.Equal("20% off everything", Element.Fields((JsonObject)Element.Slot(liveGrid, "cards")[0]!)["title"]!.GetValue<string>());

        var liveDiscount = (await _site.Discounts.LoadAsync(discount))!;
        Assert.Equal(page.ToString(), Element.Fields(liveDiscount.Root)["landingPage"]!.GetValue<string>());
        Assert.True(Element.Fields(liveDiscount.Root)["active"]!.GetValue<bool>()); // saved as part of the discount
        Assert.Single(_site.Outbox.Sent); // the side effect ran after the changes
    }

    [Fact]
    public async Task A_failed_step_leaves_earlier_steps_applied_and_applying_again_resumes()
    {
        await _agent.StartChangeSetAsync("Launch");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("email_send", """{ "to": "list@example.com", "subject": "We launched" }""");

        _site.Outbox.ServerDown = true;
        var failure = await _agent.FailAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal("Runtime", failure.GetProperty("category").GetString());
        Assert.Contains("Partly applied", failure.GetProperty("message").GetString());
        Assert.Equal(
            ["Content: Applied.", "email.send: Failed: The mail server is not responding."],
            failure.GetProperty("problems").EnumerateArray().Select(p => $"{p.GetProperty("path").GetString()}: {p.GetProperty("message").GetString()}"));

        // The page is applied; the set is kept, and its applied part can no longer be edited in it.
        Assert.Equal(1, (await _site.Pages.LoadAsync(page))!.LiveVersion);
        var locked = await _agent.FailAsync("entity_setValue", $$"""
            { "entityType": "cms.page", "key": "{{page}}", "elementPath": "", "field": "title", "value": "Changed", "culture": "en-US" }
            """);
        Assert.Contains("already applied", locked.GetProperty("message").GetString());

        // Applying again resumes at the failed step, and does not save the page twice.
        _site.Outbox.ServerDown = false;
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Single(_site.Outbox.Sent);
        Assert.Equal(1, (await _site.Pages.LoadAsync(page))!.LiveVersion);
    }

    [Fact]
    public async Task A_product_whose_save_fails_rolls_back_all_of_its_changes_and_can_be_fixed_and_resumed()
    {
        await _agent.StartChangeSetAsync("Two discounts");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Deals" } }""")).GetProperty("key").GetGuid();
        var first = await CreateDiscountAsync("First", "DEAL");
        var second = await CreateDiscountAsync("Second", "DEAL"); // passes validation (only live codes are checked), fails on save

        var failure = await _agent.FailAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Contains("Commerce failed and was rolled back", failure.GetProperty("message").GetString());

        // Content applied; Commerce rolled back as a whole, including the first discount, which saved fine.
        Assert.NotNull(await _site.Pages.LoadAsync(page));
        Assert.Null(await _site.Discounts.LoadAsync(first));
        Assert.Null(await _site.Discounts.LoadAsync(second));

        // Commerce isn't applied, so it can still be fixed in the set; applying again finishes the job.
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{second}}", "elementPath": "", "field": "code", "value": "DEAL2" }
            """);
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.NotNull(await _site.Discounts.LoadAsync(first));
        Assert.NotNull(await _site.Discounts.LoadAsync(second));
    }

    private async Task<Guid> CreateDiscountAsync(string name, string code)
    {
        var discount = (await _agent.OkAsync("entity_create", $$"""{ "entityType": "commerce.discount", "fields": { "name": "{{name}}", "code": "{{code}}" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 0 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");
        return discount;
    }

    [Fact]
    public async Task Applying_is_refused_when_live_data_changed_underneath_the_set()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartChangeSetAsync("Rename");
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "", "field": "name", "value": "Renamed in set" }
            """);

        // Someone saves the discount directly while the set is open.
        var meanwhile = (await _site.Discounts.LoadAsync(discount))!;
        Element.Fields(meanwhile.Root)["name"] = "Changed live";
        await _site.Discounts.SaveAsync(meanwhile);

        var failure = await _agent.FailAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal($"commerce.discount:{discount}", failure.GetProperty("problems")[0].GetProperty("path").GetString());
        Assert.Equal("Changed live", _site.Discounts.ToRef((await _site.Discounts.LoadAsync(discount))!).Name);
    }

    [Fact]
    public async Task Reads_inside_a_set_show_its_edits_and_reads_outside_show_live_data()
    {
        var discount = await CreateLiveDiscountAsync("Live name");

        var set = await _agent.StartChangeSetAsync("Rename");
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "", "field": "name", "value": "Draft name" }
            """);
        var inSet = await _agent.OkAsync("entity_get", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}" }""");
        Assert.Equal("Draft name", inSet.GetProperty("fields").GetProperty("name").GetString());
        Assert.Single((await _agent.OkAsync("entity_find", """{ "entityType": "commerce.discount", "query": "Draft" }""")).EnumerateArray());

        _agent.ChangeSetId = null;
        var live = await _agent.OkAsync("entity_get", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}" }""");
        Assert.Equal("Live name", live.GetProperty("fields").GetProperty("name").GetString());

        await _agent.OkAsync("changeSet_discard", $$"""{ "changeSetId": "{{set}}" }""");
        await _agent.FailAsync("changeSet_validate", $$"""{ "changeSetId": "{{set}}" }""");
    }

    [Fact]
    public async Task Replaying_a_set_gives_the_same_entity_every_time()
    {
        await _agent.StartChangeSetAsync("Replay");
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

        var set = _site.ChangeSets.Get(_agent.ChangeSetId!.Value);
        var first = (await _site.ChangeSets.GetWorkingStateAsync(set, "cms.page", page))!.ToJson().ToJsonString();
        var second = (await _site.ChangeSets.GetWorkingStateAsync(set, "cms.page", page))!.ToJson().ToJsonString();
        Assert.Equal(first, second);

        // The key the agent was given is the key that is stored, so later calls can refer to it.
        var blocks = Element.Slot((await _site.ChangeSets.GetWorkingStateAsync(set, "cms.page", page))!.Root, "blocks");
        Assert.Equal(hero.GetProperty("itemKey").GetGuid(), Element.ItemKey(blocks[0]!));
    }

    private async Task<Guid> CreateLiveDiscountAsync(string name)
    {
        var set = await _agent.StartChangeSetAsync("Seed");
        var discount = (await _agent.OkAsync("entity_create", $$"""{ "entityType": "commerce.discount", "fields": { "name": "{{name}}" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 50 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{set}}" }""");
        _agent.ChangeSetId = null;
        return discount;
    }
}
