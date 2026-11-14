using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples;
using Umbraco.Cms.Core;

namespace Umbraco.Abilities.Tests;

/// <summary>Entity identity is a UDI, as in Deploy and the CMS. Tools keep dashed keys, and accept UDIs for references.</summary>
public class UdiTests
{
    private readonly SampleSite _site = SampleSite.Create();
    private readonly ScriptedAgent _agent;

    public UdiTests() => _agent = new ScriptedAgent(_site);

    [Fact]
    public void Entity_types_are_UDI_entity_types_and_package_types_are_registered()
    {
        var discount = Guid.NewGuid();
        Assert.True(UdiParser.TryParse($"umb://umbraco-commerce-discount/{discount:N}", out GuidUdi? udi));
        Assert.Equal(discount, udi!.Guid);
        Assert.Equal("umb://document/" + discount.ToString("N"), new EntityRef("document", discount, "Page").ToUdi().ToString());
    }

    [Fact]
    public async Task A_reference_can_be_given_as_a_UDI_and_is_stored_in_the_fields_own_form()
    {
        await _agent.StartChangeSetAsync("Campaign");
        var page = (await _agent.OkAsync("entity_create", """{ "entityType": "document", "culture": "en-US", "fields": { "title": "Launch" } }""")).GetProperty("key").GetGuid();
        var media = (await _agent.OkAsync("entity_create", """{ "entityType": "media", "fields": { "name": "Hero" } }""")).GetProperty("key").GetGuid();
        var discount = (await _agent.OkAsync("entity_create", """{ "entityType": "umbraco-commerce-discount", "fields": { "name": "Sale" } }""")).GetProperty("key").GetGuid();

        // A single-type reference stores a dashed key; a multi-type one stores { entityType, key }.
        await _agent.OkAsync("entity_setValue", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "elementPath": "", "field": "landingPage", "value": "umb://document/{{page:N}}" }""");
        await _agent.OkAsync("entity_setValue", $$"""{ "entityType": "document", "key": "{{page}}", "elementPath": "", "field": "related", "value": ["umb://media/{{media:N}}"] }""");

        var set = _site.ChangeSets.Get(_agent.ChangeSetId!.Value);
        var discountState = (await _site.ChangeSets.GetWorkingStateAsync(set, "umbraco-commerce-discount", discount))!;
        var pageState = (await _site.ChangeSets.GetWorkingStateAsync(set, "document", page))!;
        Assert.Equal(page.ToString(), discountState.Root.Fields["landingPage"]!.GetValue<string>());
        Assert.Equal("media", pageState.Root.Fields["related"]![0]!["entityType"]!.GetValue<string>());
        Assert.Equal(media.ToString(), pageState.Root.Fields["related"]![0]!["key"]!.GetValue<string>());

        // A UDI of the wrong type says which type was expected.
        var wrong = await _agent.FailAsync("entity_setValue", $$"""{ "entityType": "umbraco-commerce-discount", "key": "{{discount}}", "elementPath": "", "field": "landingPage", "value": "umb://media/{{media:N}}" }""");
        Assert.Equal("Expected a document, not a media.", wrong.GetProperty("problems")[0].GetProperty("errorMessages")[0].GetString());
    }
}
