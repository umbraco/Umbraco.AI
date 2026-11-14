using System.Text.Json;

using Umbraco.Abilities.Samples;
using Umbraco.Abilities.Samples.Commerce;
using Umbraco.Abilities.Samples.Commerce.Domain;

namespace Umbraco.Abilities.Tests;

/// <summary>
/// Item types for plugin seams come from each product's own registry: Commerce's discount rule and reward providers,
/// Automate's triggers and actions. Nothing is written per type, and plugins write no Abilities code.
/// </summary>
public class PluginItemTypeTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public PluginItemTypeTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task Rule_and_reward_types_come_from_Commerces_provider_collections()
    {
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "umbraco-commerce-discount", "itemTypes": ["orderTotal", "productCollection"] }""");
        Assert.Equal(["orderTotal", "customerGroup", "productCollection", "groupDiscountRule", "acmeDayOfWeek"], AllowedIn(schema, "rules"));

        // A provider that leaves its label as a localization key gets its alias, and a note to the model.
        var orderTotal = schema.GetProperty("itemTypes").GetProperty("orderTotal");
        Assert.Equal("orderTotal", orderTotal.GetProperty("name").GetString());
        Assert.Equal(["minTotal"], orderTotal.GetProperty("fields").GetProperty("required").EnumerateArray().Select(r => r.GetString()));

        // A setting edited with a picker Commerce maps is a reference to what it picks.
        var collection = schema.GetProperty("itemTypes").GetProperty("productCollection").GetProperty("fields").GetProperty("properties").GetProperty("collection");
        Assert.Equal("umbraco-commerce-collection", collection.GetProperty("x-references").GetString());
    }

    [Fact]
    public async Task A_provider_registered_later_shows_up_with_no_change_anywhere_else()
    {
        _site.RuleProviders.Add(new FirstOrderRuleProvider());

        await _agent.StartChangeSetAsync("Welcome");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Welcome" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "rules", "itemType": "firstOrder" }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "rewards", "itemType": "freeShipping" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        Assert.Equal("firstOrder", _site.DiscountService.GetDiscount(discount)!.Rules.Single().RuleProviderAlias);
    }

    [Fact]
    public async Task A_group_rule_holds_child_rules_and_is_stored_as_Commerces_rule_tree()
    {
        await _agent.StartChangeSetAsync("VIP or big orders");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "VIP or big orders" } }""")).GetProperty("key").GetGuid();
        var group = (await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "rules", "itemType": "groupDiscountRule", "fields": { "match": "any" } }""")).GetProperty("path").GetString();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "rewards", "itemType": "percentageOff", "fields": { "percent": 15 } }""");

        // A group needs at least one rule.
        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal([$"{group}/children"], JsonAssert.ProblemPaths(validation));

        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "{{group}}/children", "itemType": "customerGroup", "fields": { "group": "VIP" } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "fieldPath": "{{group}}/children", "itemType": "orderTotal", "fields": { "minTotal": 500 } }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        var stored = _site.DiscountService.GetDiscount(discount)!.Rules.Single();
        Assert.Equal(("groupDiscountRule", "any"), (stored.RuleProviderAlias, stored.Settings["match"]));
        Assert.Equal(
            ["customerGroup group=VIP", "orderTotal minTotal=500"],
            stored.Children!.Select(c => $"{c.RuleProviderAlias} {string.Join(",", c.Settings.Select(s => $"{s.Key}={s.Value}"))}"));
    }

    [Fact]
    public async Task Automates_triggers_and_actions_come_from_its_collections_and_settings_can_be_bindings()
    {
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "umbraco-automate-workflow", "itemTypes": ["umbracoCommerce.orderCreated", "umbracoCommerce.changeOrderStatus"] }""");
        Assert.Equal(["formSubmitted", "schedule", "umbracoCommerce.orderCreated"], AllowedIn(schema, "trigger"));

        var orderId = schema.GetProperty("itemTypes").GetProperty("umbracoCommerce.changeOrderStatus").GetProperty("fields").GetProperty("properties").GetProperty("orderId");
        Assert.True(orderId.GetProperty("x-bindings").GetBoolean());

        // An editor no table maps stays a plain value, and the description names it.
        var store = schema.GetProperty("itemTypes").GetProperty("umbracoCommerce.orderCreated").GetProperty("fields").GetProperty("properties").GetProperty("storeId");
        Assert.Contains("Uc.PropertyEditorUi.StorePicker", store.GetProperty("description").GetString());

        await _agent.StartChangeSetAsync("Mark new orders");
        var workflow = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-automate-workflow", "fields": { "name": "Mark new orders" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "fieldPath": "trigger", "itemType": "umbracoCommerce.orderCreated" }""");
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "fieldPath": "steps", "itemType": "umbracoCommerce.changeOrderStatus",
              "fields": { "orderId": "{{"{{"}}trigger.orderId{{"}}"}}", "orderStatusId": "{{Guid.NewGuid()}}" } }
            """);

        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.True(validation.GetProperty("valid").GetBoolean(), validation.ToString());
    }

    [Fact]
    public async Task A_user_is_only_offered_plugin_types_they_may_use()
    {
        // Can build workflows, but has no Commerce section.
        _agent.UserKey = SampleAccess.AutomateEditor;
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "umbraco-automate-workflow" }""");
        Assert.Equal(["formSubmitted", "schedule"], AllowedIn(schema, "trigger"));
        Assert.DoesNotContain("umbracoCommerce.changeOrderStatus", AllowedIn(schema, "steps"));

        await _agent.StartChangeSetAsync("Mark new orders");
        var workflow = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-automate-workflow", "fields": { "name": "Mark new orders" } }""")).GetProperty("key").GetGuid();
        var refused = await _agent.FailAsync("entity_addItem", $$"""{ "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "fieldPath": "trigger", "itemType": "umbracoCommerce.orderCreated" }""");
        Assert.Equal("AccessDenied", refused.GetProperty("status").GetString());
        Assert.Equal("You may not use umbracoCommerce.orderCreated.", refused.GetProperty("message").GetString());
    }

    private static List<string?> AllowedIn(JsonElement schema, string field)
        => schema.GetProperty("fields").GetProperty("properties").GetProperty(field).GetProperty("x-item-types").EnumerateArray().Select(t => t.GetProperty("alias").GetString()).ToList();

    [DiscountRuleProvider("firstOrder", Label = "First order", Description = "The customer's first order.")]
    private sealed class FirstOrderRuleProvider : DiscountRuleProviderBase<NoSettings>;
}
