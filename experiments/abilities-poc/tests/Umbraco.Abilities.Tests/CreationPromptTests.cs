using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.AI;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>"10% off the Summer collection for VIP customers in December", built the way an agent would.</summary>
public class CreationPromptTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public CreationPromptTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task Discount_from_text_is_built_in_a_draft_set_and_only_goes_live_when_applied()
    {
        await _agent.StartDraftSetAsync("VIP summer sale");

        // 1. Discover the structure, in two steps.
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "commerce.discount" }""");
        var ruleTypes = schema.GetProperty("slots").EnumerateArray()
            .Single(s => s.GetProperty("alias").GetString() == "rules")
            .GetProperty("allowedItemTypes").EnumerateArray().Select(t => t.GetProperty("alias").GetString()).ToList();
        Assert.Contains("productCollection", ruleTypes);
        Assert.Contains("acmeDayOfWeek", ruleTypes); // a third party's rule type shows up with no change to Commerce

        var details = await _agent.OkAsync("entity_getSchema", """{ "entityType": "commerce.discount", "itemTypes": ["productCollection", "customerGroup", "percentageOff"] }""");
        var collectionField = details.GetProperty("itemTypes").GetProperty("productCollection").GetProperty("fields").GetProperty("properties").GetProperty("collection");
        Assert.Equal("commerce.collection", collectionField.GetProperty("x-references").GetString());

        // 2. Turn the name the person used into a key.
        var found = await _agent.OkAsync("entity_find", """{ "entityType": "commerce.collection", "query": "summer" }""");
        var collection = found[0].GetProperty("key").GetGuid();
        Assert.Equal(_site.SummerCollection, collection);

        // 3. Build it step by step.
        var created = await _agent.OkAsync("entity_create", """
            { "entityType": "commerce.discount", "fields": { "name": "VIP summer sale", "startDate": "2026-12-01", "endDate": "2026-12-31" } }
            """);
        var discount = created.GetProperty("key").GetGuid();

        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "productCollection", "fields": { "collection": "{{collection}}" } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "customerGroup", "fields": { "group": "VIP" } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "percentageOff", "fields": { "percent": 10 } }
            """);

        // 4. Check, review, apply.
        var validation = await _agent.OkAsync("draftSet_validate", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.True(validation.GetProperty("valid").GetBoolean(), validation.ToString());
        Assert.Empty(_site.CommerceDatabase.All("commerce.discount"));

        var summary = await _agent.OkAsync("draftSet_summarize", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        var change = summary.GetProperty("entities")[0];
        Assert.Equal("created", change.GetProperty("change").GetString());
        Assert.Equal("VIP summer sale", change.GetProperty("entity").GetProperty("name").GetString());

        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");

        var live = (await _site.Discounts.LoadAsync(discount))!;
        Assert.Equal(1, live.LiveVersion);
        Assert.Equal(["productCollection", "customerGroup"], Element.Slot(live.Root, "rules").Select(r => Element.ItemType(r!)));
        Assert.Equal(10, Element.Fields((JsonObject)Element.Slot(live.Root, "rewards")[0]!)["percent"]!.GetValue<double>());
    }

    [Fact]
    public async Task Validation_reports_problems_by_path_and_the_agent_fixes_them_with_the_same_paths()
    {
        await _agent.StartDraftSetAsync("Draft with problems");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "commerce.discount" }""")).GetProperty("key").GetGuid();
        var reward = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "percentageOff", "fields": { "percent": 150 } }
            """);
        var rewardPath = reward.GetProperty("path").GetString();

        var validation = await _agent.OkAsync("draftSet_validate", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.False(validation.GetProperty("valid").GetBoolean());
        Assert.Equal(["name", "rules", $"{rewardPath}/percent"], JsonAssert.ProblemPaths(validation));

        // Applying now fails, and the failure says it can be corrected.
        var refused = await _agent.FailAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.Equal("Input", refused.GetProperty("category").GetString());
        Assert.True(refused.GetProperty("correctable").GetBoolean());

        // Each problem path is an element path plus a field, so it maps straight onto entity_setValue.
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "", "field": "name", "value": "Fixed" }
            """);
        await _agent.OkAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "{{rewardPath}}", "field": "percent", "value": 15 }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 100 } }
            """);

        validation = await _agent.OkAsync("draftSet_validate", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");
        Assert.True(validation.GetProperty("valid").GetBoolean(), validation.ToString());
    }

    [Fact]
    public async Task Bad_edits_are_correctable_input_failures_with_paths()
    {
        await _agent.StartDraftSetAsync("Bad edits");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "commerce.discount", "fields": { "name": "x" } }""")).GetProperty("key").GetGuid();

        var wrongType = await _agent.FailAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "freeShipping" }
            """);
        Assert.True(wrongType.GetProperty("correctable").GetBoolean());
        Assert.Equal("rules", wrongType.GetProperty("problems")[0].GetProperty("path").GetString());
        Assert.Contains("customerGroup", wrongType.GetProperty("message").GetString());

        var reward = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "percentageOff" }
            """);
        var wrongValue = await _agent.FailAsync("entity_setValue", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "elementPath": "{{reward.GetProperty("path").GetString()}}", "field": "percent", "value": "ten" }
            """);
        Assert.Equal($"{reward.GetProperty("path").GetString()}/percent", wrongValue.GetProperty("problems")[0].GetProperty("path").GetString());

        // Edits never run outside a draft set.
        _agent.DraftSetId = null;
        var outside = await _agent.FailAsync("entity_create", """{ "entityType": "commerce.discount" }""");
        Assert.Contains("draftSet_create", outside.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_third_party_rule_type_is_creatable_with_no_change_to_commerce()
    {
        await _agent.StartDraftSetAsync("Weekend deal");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "commerce.discount", "fields": { "name": "Weekend deal" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "acmeDayOfWeek", "fields": { "days": "weekends" } }
            """);
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "commerce.discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }
            """);

        await _agent.OkAsync("draftSet_apply", $$"""{ "draftSetId": "{{_agent.DraftSetId}}" }""");

        var live = (await _site.Discounts.LoadAsync(discount))!;
        Assert.Equal("acmeDayOfWeek", Element.ItemType(Element.Slot(live.Root, "rules")[0]!));
    }

    [Fact]
    public async Task Every_ability_is_an_AI_tool_and_only_irreversible_writes_need_approval()
    {
        Assert.Equal(_site.Abilities.All.Count, _agent.Tools.Count);

#pragma warning disable MEAI001
        var needApproval = _agent.Tools.Values.OfType<ApprovalRequiredAIFunction>().Select(t => t.Name).Order().ToList();
#pragma warning restore MEAI001
        Assert.Equal(["commerce_activateDiscount", "draftSet_apply", "email_send"], needApproval);

        var addItem = _agent.Tools["entity_addItem"].JsonSchema;
        var properties = addItem.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("slotPath", properties);
        Assert.Contains("itemType", properties);
        Assert.Contains("draft set", _agent.Tools["entity_addItem"].Description);
    }
}
