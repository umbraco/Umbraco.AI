using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>An agent acts for a user and gets their permissions, checked by each product's own permission model.</summary>
public class AuthorizationTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public AuthorizationTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task A_change_the_user_may_not_make_is_refused_when_recorded()
    {
        _agent.UserKey = SampleAccess.ContentEditor;
        await _agent.StartChangeSetAsync("Sale");

        await _agent.OkAsync("entity_create", """{ "entityType": "document", "culture": "en-US", "fields": { "title": "Launch" } }""");
        var refused = await _agent.FailAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Sale" } }""");

        Assert.Equal("AccessDenied", refused.GetProperty("status").GetString());
        Assert.False(refused.GetProperty("correctable").GetBoolean());
        Assert.Equal("You don't have access to umbraco-commerce-discount entities.", refused.GetProperty("message").GetString());
        Assert.Single(_site.ChangeSets.Get(_agent.ChangeSetId!.Value).Changes); // only the page
    }

    [Fact]
    public async Task Applying_checks_the_user_who_applies_and_applies_nothing_if_any_change_is_refused()
    {
        // Built by an editor who may use everything...
        await _agent.StartChangeSetAsync("Campaign");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "document", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Sale" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 50 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");

        // ...applied by someone who may not change discounts.
        _agent.UserKey = SampleAccess.ContentEditor;
        var refused = await _agent.FailAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal("AccessDenied", refused.GetProperty("status").GetString());
        Assert.Equal([$"umbraco-commerce-discount:{discount}"], refused.GetProperty("problems").EnumerateArray().Select(p => p.GetProperty("path").GetString()));
        Assert.Null(await _site.Pages.LoadAsync(page)); // not even the page they may change

        _agent.UserKey = SampleAccess.Editor;
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.NotNull(await _site.Pages.LoadAsync(page));
    }

    [Fact]
    public async Task Reads_show_only_what_the_user_may_read()
    {
        _agent.UserKey = SampleAccess.ContentEditor;
        var found = await _agent.OkAsync("entity_find", """{ "entityType": "umbraco-commerce-collection", "query": "summer" }""");
        Assert.Equal(0, found.GetArrayLength());

        var refused = await _agent.FailAsync("entity_get", $$"""{ "entityType": "umbraco-commerce-collection", "key": "{{_site.SummerCollection}}" }""");
        Assert.Equal("AccessDenied", refused.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_product_ability_run_directly_checks_the_user_too()
    {
        await _agent.StartChangeSetAsync("Seed");
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Sale" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 50 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "freeShipping" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        _agent.ChangeSetId = null;

        _agent.UserKey = SampleAccess.ContentEditor;
        var refused = await _agent.FailAsync("commerce_activateDiscount", $$"""{ "discount": "{{discount}}" }""");
        Assert.Equal("AccessDenied", refused.GetProperty("status").GetString());
        Assert.False((await _site.Discounts.LoadModelAsync(discount))!.Active);
    }
}
