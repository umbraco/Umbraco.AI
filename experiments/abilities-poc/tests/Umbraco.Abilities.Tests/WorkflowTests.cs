using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>An Automate workflow is an entity whose steps are abilities, linked as a graph by item references.</summary>
public class WorkflowTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public WorkflowTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public async Task Step_types_are_derived_from_abilities()
    {
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "umbraco-automate-workflow", "itemTypes": ["email.send"] }""");
        var stepTypes = schema.GetProperty("slots").EnumerateArray()
            .Single(s => s.GetProperty("alias").GetString() == "steps")
            .GetProperty("allowedItemTypes").EnumerateArray().Select(t => t.GetProperty("alias").GetString()).ToList();
        Assert.Equal(["email.send", "commerce.activateDiscount"], stepTypes);

        var email = schema.GetProperty("itemTypes").GetProperty("email.send").GetProperty("fields");
        Assert.Equal(["to", "subject", "body", "next"], email.GetProperty("properties").EnumerateObject().Select(p => p.Name));
        Assert.Equal(["to", "subject"], email.GetProperty("required").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task A_workflow_from_text_links_its_steps_and_can_refer_to_a_discount_in_the_same_set()
    {
        // "When someone signs up for the newsletter, email them, then switch on the welcome discount."
        await _agent.StartChangeSetAsync("Welcome flow");

        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Welcome 10%" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rules", "itemType": "orderTotal", "fields": { "minTotal": 0 } }""");
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "slotPath": "rewards", "itemType": "percentageOff", "fields": { "percent": 10 } }""");

        var workflow = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-automate-workflow", "fields": { "name": "Welcome flow", "enabled": true } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "slotPath": "trigger", "itemType": "formSubmitted", "fields": { "form": "Newsletter" } }
            """);

        var emailStep = Guid.NewGuid();
        var activateStep = Guid.NewGuid();
        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "slotPath": "steps", "itemType": "email.send", "itemKey": "{{emailStep}}",
              "fields": { "to": "{{"{{"}}email{{"}}"}}", "subject": "Welcome", "next": "{{activateStep}}" } }
            """);

        // The edge points at a step that does not exist yet.
        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal([$"steps/{emailStep}/next"], JsonAssert.ProblemPaths(validation));

        await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "slotPath": "steps", "itemType": "commerce.activateDiscount", "itemKey": "{{activateStep}}",
              "fields": { "discount": "{{discount}}" } }
            """);

        validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.True(validation.GetProperty("valid").GetBoolean(), validation.ToString());

        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        var live = (await _site.Workflows.LoadAsync(workflow))!;
        Assert.Equal(["email.send", "commerce.activateDiscount"], live.Root.Slot("steps").Select(s => s.Type));
    }

    [Fact]
    public async Task A_step_referring_to_a_missing_discount_is_a_problem()
    {
        await _agent.StartChangeSetAsync("Broken flow");
        var workflow = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-automate-workflow", "fields": { "name": "Broken" } }""")).GetProperty("key").GetGuid();
        await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "slotPath": "trigger", "itemType": "schedule", "fields": { "cron": "0 9 * * 1" } }""");
        var step = await _agent.OkAsync("entity_addItem", $$"""
            { "entityType": "umbraco-automate-workflow", "key": "{{workflow}}", "slotPath": "steps", "itemType": "commerce.activateDiscount", "fields": { "discount": "{{Guid.NewGuid()}}" } }
            """);

        var validation = await _agent.OkAsync("changeSet_validate", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        Assert.Equal([$"{step.GetProperty("path").GetString()}/discount"], JsonAssert.ProblemPaths(validation));
    }
}
