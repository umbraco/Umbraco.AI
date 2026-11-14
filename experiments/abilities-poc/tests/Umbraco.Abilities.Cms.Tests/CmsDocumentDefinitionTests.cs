using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Abilities.AI;
using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using Element = Umbraco.Abilities.Entities.Element;

namespace Umbraco.Abilities.Cms.Tests;

/// <summary>
/// The CMS document definition against a real Umbraco (SQLite): culture-variant documents with a Block List
/// (blocks with culture-variant values and a settings element) and a Block Grid (placement and areas), built
/// through the AI tools in a change set, validated by Umbraco, and saved through IContentEditingService.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class CmsDocumentDefinitionTests : UmbracoIntegrationTest
{
    private IContentType _page = null!;
    private Agent _agent = null!;
    private CmsDocumentDefinition _documents = null!;
    private ChangeSetService _changeSets = null!;

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

    // A third-party handler that vetoes saving a page called "Refused", to make a save fail after validation.
    protected override void CustomTestSetup(IUmbracoBuilder builder)
        => builder.AddNotificationHandler<ContentSavingNotification, RefuseSavingHandler>();

    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "4",
            ["Tests:Database:SchemaDatabaseCount"] = "4",
            ["Tests:Database:EmptyDatabasesCount"] = "2",
        });
    }

    [SetUp]
    public async Task SetUpSiteAsync()
    {
        await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), Constants.Security.SuperUserKey);

        // Blocks: a hero whose heading varies by culture and that has a settings element; a text block;
        // and a two-column grid row with areas.
        var hero = await CreateElementTypeAsync("hero", ContentVariation.Culture, ("heading", ContentVariation.Culture, true));
        var heroSettings = await CreateElementTypeAsync("heroSettings", ContentVariation.Nothing, ("theme", ContentVariation.Nothing, false));
        var text = await CreateElementTypeAsync("textBlock", ContentVariation.Culture, ("text", ContentVariation.Culture, false));
        var row = await CreateElementTypeAsync("twoColumns", ContentVariation.Nothing, ("label", ContentVariation.Nothing, false));

        var blockList = await CreateDataTypeAsync(Constants.PropertyEditors.Aliases.BlockList, "Page blocks", new BlockListConfiguration.BlockConfiguration[]
        {
            new() { ContentElementTypeKey = hero.Key, SettingsElementTypeKey = heroSettings.Key },
            new() { ContentElementTypeKey = text.Key },
        });
        var blockGrid = await CreateDataTypeAsync(Constants.PropertyEditors.Aliases.BlockGrid, "Page grid", new BlockGridConfiguration.BlockGridBlockConfiguration[]
        {
            new()
            {
                ContentElementTypeKey = row.Key,
                AllowAtRoot = true,
                AreaGridColumns = 12,
                Areas =
                [
                    new() { Key = Guid.NewGuid(), Alias = "left", ColumnSpan = 6, RowSpan = 1, MaxAllowed = 2 },
                    new() { Key = Guid.NewGuid(), Alias = "right", ColumnSpan = 6, RowSpan = 1 },
                ],
            },
            new() { ContentElementTypeKey = text.Key, AllowAtRoot = true, AllowInAreas = true },
        });

        _page = new ContentTypeBuilder()
            .WithAlias("landingPage")
            .WithName("Landing page")
            .WithContentVariation(ContentVariation.Culture)
            .AddPropertyType()
                .WithAlias("subtitle").WithName("Subtitle")
                .WithDataTypeId(Constants.DataTypes.Textbox).WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithValueStorageType(ValueStorageType.Nvarchar).WithVariations(ContentVariation.Culture).WithMandatory(true)
                .Done()
            .AddPropertyType()
                .WithAlias("blocks").WithName("Blocks").WithDataTypeId(blockList.Id).WithVariations(ContentVariation.Nothing)
                .Done()
            .AddPropertyType()
                .WithAlias("grid").WithName("Grid").WithDataTypeId(blockGrid.Id).WithVariations(ContentVariation.Nothing)
                .Done()
            .Build();
        _page.AllowedAsRoot = true;
        await ContentTypeService.CreateAsync(_page, Constants.Security.SuperUserKey);

        // The framework: generic abilities, the change set service applying inside an Umbraco scope, and the
        // CMS document definition.
        var abilities = new AbilityRegistry();
        var definitions = new EntityDefinitionRegistry();
        var itemTypes = new ItemTypeRegistry();
        _changeSets = new ChangeSetService(new InMemoryChangeSetStore(), abilities, definitions, itemTypes);
        _documents = new CmsDocumentDefinition(
            ContentTypeService,
            GetRequiredService<IDataTypeService>(),
            GetRequiredService<ILanguageService>(),
            ContentEditingService,
            GetRequiredService<IEntityService>(),
            GetRequiredService<PropertyEditorCollection>(),
            GetRequiredService<IJsonSerializer>(),
            itemTypes,
            new CmsScopeUnitOfWorkProvider(GetRequiredService<ICoreScopeProvider>()),
            () => Constants.Security.SuperUserKey);
        definitions.Add(_documents);

        abilities
            .Add(new GetEntitySchemaAbility(definitions, itemTypes))
            .Add(new FindEntityAbility(definitions, _changeSets))
            .Add(new GetEntityAbility(definitions, _changeSets))
            .Add(new CreateEntityAbility())
            .Add(new SetEntityValueAbility())
            .Add(new AddEntityItemAbility())
            .Add(new RemoveEntityItemAbility())
            .Add(new MoveEntityItemAbility())
            .Add(new CreateChangeSetAbility(_changeSets))
            .Add(new ValidateChangeSetAbility(_changeSets))
            .Add(new SummarizeChangeSetAbility(_changeSets))
            .Add(new ApplyChangeSetAbility(_changeSets))
            .Add(new DiscardChangeSetAbility(_changeSets))
            .Add(new ResolveConflictAbility(_changeSets))
            .Add(new FailingOperationAbility());

        _agent = new Agent(abilities, new AbilityInvoker(abilities, _changeSets));
    }

    [Test]
    public async Task The_schema_comes_from_the_document_type_and_its_block_editors()
    {
        var subtypes = await _agent.OkAsync("entity_getSchema", """{ "entityType": "cms.document" }""");
        Assert.That(subtypes.GetProperty("subtypes").EnumerateArray().Select(s => s.GetProperty("alias").GetString()), Does.Contain("landingPage"));

        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "cms.document", "subtype": "landingPage" }""");
        Assert.That(schema.GetProperty("cultures").EnumerateArray().Select(c => c.GetString()), Is.EquivalentTo(new[] { "en-US", "da-DK" }));
        Assert.That(schema.GetProperty("fields").GetProperty("properties").GetProperty("subtitle").GetProperty("x-varies").GetString(), Is.EqualTo("Culture"));

        var slots = schema.GetProperty("slots").EnumerateArray().ToDictionary(s => s.GetProperty("alias").GetString()!);
        Assert.That(TypesIn(slots["blocks"]), Is.EquivalentTo(new[] { "hero", "textBlock" }));
        Assert.That(TypesIn(slots["grid"]), Is.EquivalentTo(new[] { "twoColumns", "textBlock" }));
        Assert.That(slots["grid"].GetProperty("itemLayout").GetProperty("required")[0].GetString(), Is.EqualTo("columnSpan"));

        var hero = await _agent.OkAsync("entity_getSchema", """{ "entityType": "cms.document", "subtype": "landingPage", "itemTypes": ["hero", "twoColumns"] }""");
        Assert.That(hero.GetProperty("itemTypes").GetProperty("hero").GetProperty("settings").GetProperty("properties").TryGetProperty("theme", out _), Is.True);
        var areas = hero.GetProperty("itemTypes").GetProperty("twoColumns").GetProperty("slots").EnumerateArray().Select(s => s.GetProperty("alias").GetString());
        Assert.That(areas, Is.EquivalentTo(new[] { "left", "right" }));
    }

    [Test]
    public async Task A_page_built_from_text_is_saved_by_Umbraco_with_its_blocks_settings_grid_and_cultures()
    {
        await _agent.StartChangeSetAsync("Summer landing page");
        var page = await BuildSummerPageAsync();

        var validation = await _agent.OkAsync("changeSet_validate", SetJson());
        Assert.That(validation.GetProperty("valid").GetBoolean(), Is.True, validation.ToString());
        Assert.That(ContentService.GetById(page.Key), Is.Null, "nothing is saved before apply");

        await _agent.OkAsync("changeSet_apply", SetJson());

        // What Umbraco stored.
        var content = ContentService.GetById(page.Key)!;
        Assert.Multiple(() =>
        {
            Assert.That(content.GetCultureName("en-US"), Is.EqualTo("Summer"));
            Assert.That(content.GetCultureName("da-DK"), Is.EqualTo("Sommer"));
            Assert.That(content.GetValue<string>("subtitle", "da-DK"), Is.EqualTo("Sol og strand"));
        });

        var blocks = JsonNode.Parse(content.GetValue<string>("blocks")!)!;
        var heroData = blocks["contentData"]!.AsArray().Single(c => c!["key"]!.ToString() == page.Hero.ToString())!;
        Assert.That(heroData["values"]!.AsArray().Select(v => $"{v!["culture"]}:{v["value"]}"), Is.EquivalentTo(new[] { "en-US:Summer sale", "da-DK:Sommerudsalg" }));
        Assert.That(blocks["settingsData"]![0]!["values"]![0]!["value"]!.ToString(), Is.EqualTo("dark"));

        var grid = JsonNode.Parse(content.GetValue<string>("grid")!)!;
        var rowLayout = grid["layout"]![Constants.PropertyEditors.Aliases.BlockGrid]![0]!;
        Assert.That(rowLayout["columnSpan"]!.GetValue<int>(), Is.EqualTo(12));
        Assert.That(rowLayout["areas"]!.AsArray().Single(a => a!["items"]!.AsArray().Count > 0)!["items"]![0]!["contentKey"]!.ToString(), Is.EqualTo(page.AreaText.ToString()));

        // And loading it back through the definition gives the same working copy shape.
        var loaded = (await _documents.LoadAsync(page.Key))!;
        var hero = (JsonObject)Element.Slot(loaded.Root, "blocks").Single(b => Element.ItemKey(b!) == page.Hero)!;
        Assert.Multiple(() =>
        {
            Assert.That(Element.VariantPartOrNull(hero, new VariantId("da-DK", null))!["fields"]!["heading"]!.ToString(), Is.EqualTo("Sommerudsalg"));
            Assert.That(hero["settings"]!["fields"]!["theme"]!.ToString(), Is.EqualTo("dark"));
            var row = (JsonObject)Element.Slot(loaded.Root, "grid")[0]!;
            Assert.That(row["layout"]!["fields"]!["columnSpan"]!.GetValue<int>(), Is.EqualTo(12));
            Assert.That(Element.ItemKey(Element.Slot(row, "left")[0]!), Is.EqualTo(page.AreaText));
        });
    }

    [Test]
    public async Task Umbraco_validation_problems_come_back_on_the_entity_paths_and_cultures()
    {
        await _agent.StartChangeSetAsync("Missing Danish");
        var page = await CreatePageAsync();
        await SetAsync(page, "", "name", "\"Sommer\"", "da-DK");
        await SetAsync(page, "", "subtitle", "\"Sun and sand\"", "en-US");

        // A hero with English values only: Umbraco treats it as not existing in Danish, so it is valid there.
        await AddItemAsync(page, "blocks", "hero", """{ "heading": "Summer sale" }""", "en-US");

        // A hero with no values yet exists in both cultures, and its mandatory heading is missing in both.
        var empty = await AddItemAsync(page, "blocks", "hero", "{}");
        await SetAsync(page, $"{empty}/@settings", "theme", "\"light\"");

        // Ask Umbraco directly, skipping the generic structure check, to see its own errors mapped back.
        var working = (await _changeSets.GetWorkingStateAsync(_changeSets.Get(_agent.ChangeSetId!.Value), "cms.document", page))!;
        var problems = (await _documents.ValidateAsync(working)).Select(p => $"{p.Path}|{p.Culture}").ToList();

        Assert.That(problems, Is.EquivalentTo(new[] { "subtitle|da-DK", $"{empty}/heading|en-US", $"{empty}/heading|da-DK" }), string.Join(", ", problems));

        // The generic structure check agrees with Umbraco, including about the English-only hero.
        var generic = await _agent.OkAsync("changeSet_validate", SetJson());
        Assert.That(generic.GetProperty("entities")[0].GetProperty("problems").EnumerateArray().Select(p => $"{p.GetProperty("path").GetString()}|{p.GetProperty("culture").GetString()}"),
            Is.EquivalentTo(new[] { "subtitle|da-DK", $"{empty}/heading|en-US", $"{empty}/heading|da-DK" }));
    }

    [Test]
    public async Task Editing_an_existing_page_keeps_everything_the_edit_did_not_touch()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await SetAsync(page.Key, $"blocks/{page.Hero}", "heading", "\"Big summer sale\"", "en-US");
        await AddItemAsync(page.Key, "blocks", "textBlock", """{ "text": "Ends Sunday" }""", "en-US");
        await _agent.OkAsync("changeSet_apply", SetJson());

        var content = ContentService.GetById(page.Key)!;
        var blocks = JsonNode.Parse(content.GetValue<string>("blocks")!)!;
        var heroValues = blocks["contentData"]!.AsArray().Single(c => c!["key"]!.ToString() == page.Hero.ToString())!["values"]!.AsArray();
        Assert.Multiple(() =>
        {
            Assert.That(heroValues.Select(v => $"{v!["culture"]}:{v["value"]}"), Is.EquivalentTo(new[] { "en-US:Big summer sale", "da-DK:Sommerudsalg" }));
            Assert.That(blocks["contentData"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(blocks["settingsData"]![0]!["values"]![0]!["value"]!.ToString(), Is.EqualTo("dark"), "the hero's settings survive the edit");
            Assert.That(content.GetValue<string>("subtitle", "da-DK"), Is.EqualTo("Sol og strand"));
            Assert.That(JsonNode.Parse(content.GetValue<string>("grid")!)!["contentData"]!.AsArray(), Has.Count.EqualTo(3), "the grid survives the edit");
        });
    }

    [Test]
    public async Task A_failed_save_rolls_back_every_document_in_the_Umbraco_scope()
    {
        await _agent.StartChangeSetAsync("Two pages");
        var page = await BuildSummerPageAsync();
        var refused = (await _agent.OkAsync("entity_create", """{ "entityType": "cms.document", "subtype": "landingPage", "culture": "en-US", "fields": { "name": "Refused", "subtitle": "No" } }"""))
            .GetProperty("key").GetGuid();

        var failure = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.That(failure.GetProperty("message").GetString(), Does.Contain("Nothing was applied").And.Contain("Content failed and was rolled back"));
        Assert.That(ContentService.GetById(page.Key), Is.Null, "the page saved before the refused one was rolled back");
        Assert.That(ContentService.GetById(refused), Is.Null);
    }

    [Test]
    public async Task A_failed_side_effect_after_the_content_leaves_the_content_applied_and_the_set_resumable()
    {
        await _agent.StartChangeSetAsync("Content, then an operation");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("test_fail", "{}");

        FailingOperationAbility.Fail = true;
        var failure = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.That(failure.GetProperty("message").GetString(), Does.Contain("Partly applied"));
        Assert.That(ContentService.GetById(page.Key), Is.Not.Null, "Content's step committed before the operation failed");
        var version = ContentService.GetById(page.Key)!.VersionId;

        FailingOperationAbility.Fail = false;
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(ContentService.GetById(page.Key)!.VersionId, Is.EqualTo(version), "resuming does not save the page again");
    }

    [Test]
    public async Task Changes_saved_in_the_backoffice_meanwhile_are_rebased_onto_when_they_do_not_overlap()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await SetAsync(page.Key, $"blocks/{page.Hero}", "heading", "\"Big summer sale\"", "en-US");

        // Someone changes the Danish subtitle in the backoffice meanwhile.
        await Task.Delay(20);
        var content = ContentService.GetById(page.Key)!;
        content.SetValue("subtitle", "Sol, strand og hav", "da-DK");
        ContentService.Save(content);

        var summary = await _agent.OkAsync("changeSet_summarize", SetJson());
        Assert.That(summary.GetProperty("rebased")[0].GetProperty("changedSince").EnumerateArray().Select(c => c.GetString()), Does.Contain("subtitle (da-DK)"));

        await _agent.OkAsync("changeSet_apply", SetJson());
        content = ContentService.GetById(page.Key)!;
        var heroValues = JsonNode.Parse(content.GetValue<string>("blocks")!)!["contentData"]!.AsArray()
            .Single(c => c!["key"]!.ToString() == page.Hero.ToString())!["values"]!.AsArray();
        Assert.Multiple(() =>
        {
            Assert.That(content.GetValue<string>("subtitle", "da-DK"), Is.EqualTo("Sol, strand og hav"), "the backoffice change is kept");
            Assert.That(heroValues.Select(v => $"{v!["culture"]}:{v["value"]}"), Does.Contain("en-US:Big summer sale"), "the set's change is applied on top");
        });
    }

    [Test]
    public async Task A_value_changed_in_both_the_set_and_the_backoffice_is_a_conflict_to_resolve()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await SetAsync(page.Key, "", "subtitle", "\"Sun, sand and sea\"", "en-US");

        await Task.Delay(20);
        var content = ContentService.GetById(page.Key)!;
        content.SetValue("subtitle", "Changed in the backoffice", "en-US");
        ContentService.Save(content);

        var refused = await _agent.FailAsync("changeSet_apply", SetJson());
        var problem = refused.GetProperty("problems")[0];
        Assert.Multiple(() =>
        {
            Assert.That(problem.GetProperty("path").GetString(), Does.EndWith("/subtitle"));
            Assert.That(problem.GetProperty("culture").GetString(), Is.EqualTo("en-US"));
        });

        await _agent.OkAsync("changeSet_resolveConflict", $$"""
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "cms.document", "key": "{{page.Key}}", "path": "subtitle", "culture": "en-US", "keep": "theirs" }
            """);
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(ContentService.GetById(page.Key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Changed in the backoffice"));
    }

    private sealed record SummerPage(Guid Key, Guid Hero, Guid AreaText);

    private async Task<SummerPage> BuildSummerPageAsync()
    {
        var page = await CreatePageAsync();
        await SetAsync(page, "", "name", "\"Sommer\"", "da-DK");
        await SetAsync(page, "", "subtitle", "\"Sun and sand\"", "en-US");
        await SetAsync(page, "", "subtitle", "\"Sol og strand\"", "da-DK");

        var hero = await AddItemAsync(page, "blocks", "hero", """{ "heading": "Summer sale" }""", "en-US");
        await SetAsync(page, hero, "heading", "\"Sommerudsalg\"", "da-DK");
        await SetAsync(page, $"{hero}/@settings", "theme", "\"dark\"");

        var row = await AddItemAsync(page, "grid", "twoColumns", """{ "label": "Intro row" }""");
        await SetAsync(page, $"{row}/@layout", "columnSpan", "12");
        var areaText = await AddItemAsync(page, $"{row}/left", "textBlock", """{ "text": "Left column" }""", "en-US");
        await SetAsync(page, $"{areaText}/@layout", "columnSpan", "6");
        var rootText = await AddItemAsync(page, "grid", "textBlock", """{ "text": "Below the row" }""", "en-US");
        await SetAsync(page, $"{rootText}/@layout", "columnSpan", "12");

        return new SummerPage(page, Guid.Parse(hero.Split('/')[^1]), Guid.Parse(areaText.Split('/')[^1]));
    }

    private async Task<Guid> CreatePageAsync()
        => (await _agent.OkAsync("entity_create", """{ "entityType": "cms.document", "subtype": "landingPage", "culture": "en-US", "fields": { "name": "Summer" } }"""))
            .GetProperty("key").GetGuid();

    private Task<JsonElement> SetAsync(Guid page, string elementPath, string field, string valueJson, string? culture = null)
        => _agent.OkAsync("entity_setValue", $$"""{ "entityType": "cms.document", "key": "{{page}}", "elementPath": "{{elementPath}}", "field": "{{field}}", "value": {{valueJson}}{{Culture(culture)}} }""");

    private async Task<string> AddItemAsync(Guid page, string slotPath, string itemType, string fieldsJson, string? culture = null)
        => (await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "cms.document", "key": "{{page}}", "slotPath": "{{slotPath}}", "itemType": "{{itemType}}", "fields": {{fieldsJson}}{{Culture(culture)}} }"""))
            .GetProperty("path").GetString()!;

    private string SetJson() => $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""";

    private static string Culture(string? culture) => culture is null ? string.Empty : $", \"culture\": \"{culture}\"";

    private static IEnumerable<string?> TypesIn(JsonElement slot) => slot.GetProperty("allowedItemTypes").EnumerateArray().Select(t => t.GetProperty("alias").GetString());

    private async Task<IContentType> CreateElementTypeAsync(string alias, ContentVariation variation, params (string Alias, ContentVariation Variation, bool Mandatory)[] properties)
    {
        var builder = new ContentTypeBuilder().WithAlias(alias).WithName(alias).WithIsElement(true).WithContentVariation(variation);
        foreach (var property in properties)
        {
            builder.AddPropertyType()
                .WithAlias(property.Alias).WithName(property.Alias)
                .WithDataTypeId(Constants.DataTypes.Textbox).WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithValueStorageType(ValueStorageType.Nvarchar).WithVariations(property.Variation).WithMandatory(property.Mandatory)
                .Done();
        }

        var elementType = builder.Build();
        await ContentTypeService.CreateAsync(elementType, Constants.Security.SuperUserKey);
        return elementType;
    }

    private async Task<IDataType> CreateDataTypeAsync<T>(string editorAlias, string name, T blocks)
    {
        var dataType = new DataType(GetRequiredService<PropertyEditorCollection>()[editorAlias], GetRequiredService<IConfigurationEditorJsonSerializer>())
        {
            ConfigurationData = new Dictionary<string, object> { { "blocks", blocks! } },
            Name = name,
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow,
        };

        await GetRequiredService<IDataTypeService>().CreateAsync(dataType, Constants.Security.SuperUserKey);
        return dataType;
    }

    /// <summary>A recorded operation that fails when the set is applied while <see cref="Fail"/> is set.</summary>
    [Ability("test.fail", "Fail", Kind = AbilityKind.Write, Description = "Fails when run while failing is switched on.")]
    private sealed class FailingOperationAbility : AbilityBase<JsonObject, object>
    {
        public static bool Fail { get; set; }

        protected override Task<AbilityResult> ExecuteAsync(JsonObject input, AbilityContext context, CancellationToken cancellationToken)
            => Fail ? throw new InvalidOperationException("The operation failed on purpose.") : Task.FromResult(Success(new { ran = true }));
    }

    private sealed class RefuseSavingHandler : INotificationHandler<ContentSavingNotification>
    {
        public void Handle(ContentSavingNotification notification)
        {
            if (notification.SavedEntities.Any(c => c.Name == "Refused" || c.GetCultureName("en-US") == "Refused"))
            {
                notification.CancelOperation(new EventMessage("Test", "Pages called Refused can't be saved.", EventMessageType.Error));
            }
        }
    }

    /// <summary>Calls the AI tools by name with JSON arguments, as a model would.</summary>
    private sealed class Agent
    {
        private readonly Dictionary<string, AIFunction> _tools;

        public Agent(AbilityRegistry abilities, AbilityInvoker invoker)
            => _tools = AbilityAITools.Create(abilities, invoker, () => new AbilityContext("admin", "ai", ChangeSetId)).ToDictionary(t => t.Name);

        public Guid? ChangeSetId { get; private set; }

        public async Task StartChangeSetAsync(string name)
        {
            ChangeSetId = null;
            ChangeSetId = (await OkAsync("changeSet_create", $$"""{ "name": "{{name}}" }""")).GetProperty("changeSetId").GetGuid();
        }

        public async Task<JsonElement> OkAsync(string tool, string json)
        {
            var result = await CallAsync(tool, json);
            Assert.That(result.GetProperty("success").GetBoolean(), Is.True, $"{tool} failed: {result}");
            return result.GetProperty("output");
        }

        public async Task<JsonElement> FailAsync(string tool, string json)
        {
            var result = await CallAsync(tool, json);
            Assert.That(result.GetProperty("success").GetBoolean(), Is.False, $"{tool} should have failed: {result}");
            return result;
        }

        private async Task<JsonElement> CallAsync(string tool, string json)
        {
            using var document = JsonDocument.Parse(json);
            var arguments = new AIFunctionArguments(document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone()));
            return (JsonElement)(await _tools[tool].InvokeAsync(arguments))!;
        }
    }
}
