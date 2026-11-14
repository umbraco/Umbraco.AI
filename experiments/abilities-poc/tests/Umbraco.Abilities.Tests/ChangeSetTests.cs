using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;
using Umbraco.Abilities.Samples.Cms;
using Umbraco.Abilities.Samples.Commerce;
using Umbraco.Abilities.Samples.Commerce.Domain;

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

        var livePage = (await _site.Pages.LoadModelAsync(page))!;
        Assert.Equal("20% off everything", livePage.Blocks.OfType<CardGrid>().Single().Cards[0].Title);

        var liveDiscount = (await _site.Discounts.LoadModelAsync(discount))!;
        Assert.Equal(page, liveDiscount.LandingPage);
        Assert.True(liveDiscount.Active); // saved as part of the discount
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
    public async Task Changes_saved_elsewhere_that_do_not_overlap_are_rebased_onto_automatically()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartChangeSetAsync("Rename");
        await SetDiscountAsync(discount, "", "name", "\"Renamed in set\"");

        // Someone sets a code on the same discount while the set is open.
        await SaveLiveAsync(discount, d => d.SetCode("SAVE10"));

        var summary = await _agent.OkAsync("changeSet_summarize", SetJson());
        var rebased = summary.GetProperty("rebased")[0];
        Assert.Equal(["code"], rebased.GetProperty("changedSince").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(0, rebased.GetProperty("conflicts").GetArrayLength());

        await _agent.OkAsync("changeSet_apply", SetJson());
        var live = (await _site.Discounts.LoadModelAsync(discount))!;
        Assert.Equal("Renamed in set", live.Name);
        Assert.Equal("SAVE10", live.Code); // theirs kept, not overwritten by the set
    }

    [Fact]
    public async Task A_value_both_changed_is_a_conflict_until_one_is_kept()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartChangeSetAsync("Rename");
        await SetDiscountAsync(discount, "", "name", "\"Renamed in set\"");
        await SaveLiveAsync(discount, d => d.SetName("Changed live"));

        var refused = await _agent.FailAsync("changeSet_apply", SetJson());
        var problem = refused.GetProperty("problems")[0];
        Assert.Equal($"commerce.discount:{discount}/name", problem.GetProperty("path").GetString());
        Assert.Contains("Renamed in set", problem.GetProperty("message").GetString());
        Assert.Contains("Changed live", problem.GetProperty("message").GetString());
        Assert.True(refused.GetProperty("correctable").GetBoolean());

        await _agent.OkAsync("changeSet_resolveConflict", $$"""
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "commerce.discount", "key": "{{discount}}", "path": "name", "keep": "theirs" }
            """);
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.Equal("Changed live", (await _site.Discounts.LoadModelAsync(discount))!.Name);
    }

    [Fact]
    public async Task Keeping_yours_overwrites_their_value()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartChangeSetAsync("Rename");
        await SetDiscountAsync(discount, "", "name", "\"Renamed in set\"");
        await SaveLiveAsync(discount, d => d.SetName("Changed live"));

        await _agent.OkAsync("changeSet_resolveConflict", $$"""
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "commerce.discount", "key": "{{discount}}", "path": "name", "keep": "yours" }
            """);
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.Equal("Renamed in set", (await _site.Discounts.LoadModelAsync(discount))!.Name);
    }

    [Fact]
    public async Task Editing_an_item_someone_else_removed_is_a_conflict_that_can_only_keep_theirs()
    {
        var discount = await CreateLiveDiscountAsync("Original");
        var rule = (await _site.Discounts.LoadModelAsync(discount))!.Rules[0].Key;

        await _agent.StartChangeSetAsync("Raise minimum");
        await SetDiscountAsync(discount, $"rules/{rule}", "minTotal", "100");
        await SetDiscountAsync(discount, "", "name", "\"Raised\"");

        // Someone replaces the rule while the set is open.
        await SaveLiveAsync(discount, d => d.SetRules([new DiscountRuleConfig("customerGroup", new Dictionary<string, string> { ["Group"] = "VIP" })]));

        var validation = await _agent.OkAsync("changeSet_validate", SetJson());
        Assert.Contains($"rules/{rule}", JsonAssert.ProblemPaths(validation));

        var keepYours = await _agent.FailAsync("changeSet_resolveConflict", $$"""
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "commerce.discount", "key": "{{discount}}", "path": "rules/{{rule}}", "keep": "yours" }
            """);
        Assert.Contains("removed this item", keepYours.GetProperty("message").GetString());

        await _agent.OkAsync("changeSet_resolveConflict", $$"""
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "commerce.discount", "key": "{{discount}}", "path": "rules/{{rule}}", "keep": "theirs" }
            """);
        await _agent.OkAsync("changeSet_apply", SetJson());

        var live = (await _site.Discounts.LoadModelAsync(discount))!;
        Assert.Equal("Raised", live.Name); // the set's other edit still applies
        Assert.IsType<CustomerGroupRule>(Assert.Single(live.Rules));
    }

    [Fact]
    public async Task The_same_change_made_on_both_sides_is_not_a_conflict()
    {
        var discount = await CreateLiveDiscountAsync("Original");

        await _agent.StartChangeSetAsync("Rename");
        await SetDiscountAsync(discount, "", "name", "\"Summer sale\"");
        await SaveLiveAsync(discount, d => d.SetName("Summer sale"));

        await _agent.OkAsync("changeSet_apply", SetJson());
    }

    [Fact]
    public async Task An_entity_with_version_history_is_rebased_from_its_history_without_a_snapshot()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await _agent.OkAsync("entity_setValue", $$"""{ "entityType": "cms.page", "key": "{{page}}", "elementPath": "", "field": "title", "value": "Big launch", "culture": "en-US" }""");

        // Pages keep version history, so the set remembers only the version it started from.
        var set = _site.ChangeSets.Get(_agent.ChangeSetId!.Value);
        Assert.Empty(set.Bases);
        Assert.Equal(1, set.BaseVersions[$"cms.page:{page}"]);

        // Someone sets the URL segment meanwhile: rebased automatically, both kept.
        var live = (await _site.Pages.LoadModelAsync(page))!;
        live.UrlSegment["en-US"] = "launch";
        await _site.Pages.SaveModelAsync(live);

        await _agent.OkAsync("changeSet_apply", SetJson());
        var applied = (await _site.Pages.LoadModelAsync(page))!;
        Assert.Equal("Big launch", applied.Title["en-US"]);
        Assert.Equal("launch", applied.UrlSegment["en-US"]);
    }

    [Fact]
    public async Task If_the_starting_version_is_gone_from_the_history_the_entity_cant_be_rebased()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.page", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await _agent.OkAsync("entity_setValue", $$"""{ "entityType": "cms.page", "key": "{{page}}", "elementPath": "", "field": "title", "value": "Big launch", "culture": "en-US" }""");

        var live = (await _site.Pages.LoadAsync(page))!;
        await _site.Pages.SaveAsync(live);
        _site.ContentDatabase.ForgetHistory(); // e.g. a version clean-up

        var refused = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.Contains("version the set started from is gone", refused.GetProperty("problems")[0].GetProperty("message").GetString());
    }

    private Task<JsonElement> SetDiscountAsync(Guid discount, string elementPath, string field, string valueJson)
        => _agent.OkAsync("entity_setValue", $$"""{ "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "{{elementPath}}", "field": "{{field}}", "value": {{valueJson}} }""");

    /// <summary>Someone saves the discount in the backoffice, through Commerce's own service.</summary>
    private Task SaveLiveAsync(Guid discount, Action<Discount> change)
    {
        var live = _site.DiscountService.GetDiscount(discount)!.AsWritable();
        change(live);
        _site.DiscountService.SaveDiscount(live);
        return Task.CompletedTask;
    }

    private string SetJson() => $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""";

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
        var blocks = (await _site.ChangeSets.GetWorkingStateAsync(set, "cms.page", page))!.Root.Slot("blocks");
        Assert.Equal(hero.GetProperty("itemKey").GetGuid(), blocks[0].Key);
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
