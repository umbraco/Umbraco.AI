using Umbraco.Abilities.Samples;
using Umbraco.Abilities.Samples.Cms;

namespace Umbraco.Abilities.Tests;

/// <summary>
/// A value with behaviour its schema can't say: Rich Text's markup plus blocks. Paths reach both like any other value;
/// its handler keeps the markup's block tags in step with the blocks, and checks they match.
/// </summary>
public class ValueHandlerTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public ValueHandlerTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task The_same_tools_edit_inside_a_value_and_its_handler_keeps_the_rest_in_step()
    {
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "document" }""");
        var intro = schema.GetProperty("fields").GetProperty("properties").GetProperty("intro");
        Assert.Contains("places its tag in the markup", intro.GetProperty("x-edit").GetString());
        Assert.Equal("array", intro.GetProperty("properties").GetProperty("blocks").GetProperty("type").GetString());

        await _agent.StartChangeSetAsync("Intro");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "document", "variant": { "culture": "en-US" }, "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();

        await SetAsync(page, "intro", "markup", "\"<p>Big news.</p>\"");
        var callout = await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "document", "key": "{{page}}", "fieldPath": "intro/blocks", "itemType": "callout", "fields": { "text": "20% off" } }""");
        var quote = await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "document", "key": "{{page}}", "fieldPath": "intro/blocks", "itemType": "quote", "position": 0 }""");
        Assert.Equal($"intro/blocks/{callout.GetProperty("itemKey").GetGuid()}", callout.GetProperty("path").GetString());

        // A block's own fields are set like any item's, and its problems come back on its path.
        var quotePath = quote.GetProperty("path").GetString()!;
        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal([$"{quotePath}/text"], JsonAssert.ProblemPaths(validation));
        await SetAsync(page, quotePath, "text", "\"Best launch ever.\"");

        // The markup places both blocks, the quote first, as its position asked.
        var markup = (await IntroAsync(page))["markup"]!.GetValue<string>();
        Assert.True(markup.IndexOf(quote.GetProperty("itemKey").GetString()!) < markup.IndexOf(callout.GetProperty("itemKey").GetString()!));

        // Removing a block removes its tag.
        await _agent.OkAsync("entity_removeItem", $$"""{ "entityType": "document", "key": "{{page}}", "fieldPath": "intro/blocks", "itemKey": "{{callout.GetProperty("itemKey").GetString()}}" }""");
        Assert.DoesNotContain(callout.GetProperty("itemKey").GetString()!, (await IntroAsync(page))["markup"]!.GetValue<string>());

        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal("Best launch ever.", Assert.IsType<Quote>(Assert.Single((await _site.Pages.LoadModelAsync(page))!.Intro!.Blocks)).Text);
    }

    [Fact]
    public async Task The_handler_checks_the_rules_between_its_parts()
    {
        await _agent.StartChangeSetAsync("Broken intro");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "document", "variant": { "culture": "en-US" }, "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        var missing = Guid.NewGuid();
        await SetAsync(page, "intro", "markup", $$"""
            "<p>Hi</p><umb-block data-key=\"{{missing}}\"></umb-block>"
            """.Trim());

        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal(["intro/markup"], JsonAssert.ProblemPaths(validation));

        var wrongType = await _agent.FailAsync("entity_addItem", $$"""{ "entityType": "document", "key": "{{page}}", "fieldPath": "intro/blocks", "itemType": "hero" }""");
        Assert.Equal("Item type 'hero' is not allowed in this field. Allowed: callout, quote.", wrongType.GetProperty("message").GetString());
    }

    private Task<System.Text.Json.JsonElement> SetAsync(Guid page, string elementPath, string field, string valueJson)
        => _agent.OkAsync("entity_setValue", $$"""{ "entityType": "document", "key": "{{page}}", "path": "{{elementPath}}/{{field}}", "value": {{valueJson}}, "variant": { "culture": "en-US" } }""");

    private async Task<System.Text.Json.Nodes.JsonObject> IntroAsync(Guid page)
    {
        var set = _site.ChangeSets.Get(_agent.ChangeSetId!.Value);
        return (System.Text.Json.Nodes.JsonObject)(await _site.ChangeSets.GetWorkingStateAsync(set, "document", page))!.Root["intro"]!;
    }
}
