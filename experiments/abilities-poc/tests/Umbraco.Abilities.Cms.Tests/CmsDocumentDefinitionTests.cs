using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Abilities.AI;
using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Entities;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.DraftSets;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Extensions;
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
    private DraftSetService _draftSets = null!;

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

    // A third-party handler that vetoes saving a page called "Refused", to make a save fail after validation.
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentSavingNotification, RefuseSavingHandler>();

        // A package's handlers: veto sets with certain names, and record what was applied.
        builder.AddNotificationHandler<ChangeSetApplyingNotification, VetoHandler>();
        builder.AddNotificationHandler<DraftSetApplyingNotification, VetoHandler>();
        builder.AddNotificationHandler<ChangeSetAppliedNotification, VetoHandler>();
        builder.AddNotificationHandler<DraftSetAppliedNotification, VetoHandler>();
        builder.AddNotificationHandler<ChangeSetSideEffectCompletedNotification, VetoHandler>();
    }

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

        // A block with a block property of its own: a group of cards, each with a mandatory title.
        var card = await CreateElementTypeAsync("card", ContentVariation.Nothing, ("title", ContentVariation.Nothing, true));
        var cards = await CreateDataTypeAsync(Constants.PropertyEditors.Aliases.BlockList, "Cards", new BlockListConfiguration.BlockConfiguration[] { new() { ContentElementTypeKey = card.Key } });
        var cardGroup = new ContentTypeBuilder().WithAlias("cardGroup").WithName("cardGroup").WithIsElement(true).WithContentVariation(ContentVariation.Nothing)
            .AddPropertyType().WithAlias("cards").WithName("cards").WithDataTypeId(cards.Id).WithVariations(ContentVariation.Nothing).Done()
            .Build();
        await ContentTypeService.CreateAsync(cardGroup, Constants.Security.SuperUserKey);

        var blockList = await CreateDataTypeAsync(Constants.PropertyEditors.Aliases.BlockList, "Page blocks", new BlockListConfiguration.BlockConfiguration[]
        {
            new() { ContentElementTypeKey = hero.Key, SettingsElementTypeKey = heroSettings.Key },
            new() { ContentElementTypeKey = text.Key },
            new() { ContentElementTypeKey = cardGroup.Key },
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

        // Editors the mapper has no rule for. Umbraco describes each one's value with a schema.
        var accent = await CreateEditorDataTypeAsync(Constants.PropertyEditors.Aliases.ColorPickerEyeDropper, "Accent colour");

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
            .AddPropertyType()
                .WithAlias("accent").WithName("Accent").WithDataTypeId(accent.Id)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.ColorPickerEyeDropper).WithVariations(ContentVariation.Nothing)
                .Done()
            .AddPropertyType()
                .WithAlias("launchDate").WithName("Launch date").WithDataTypeId(Constants.DataTypes.DateTime)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime).WithVariations(ContentVariation.Nothing)
                .Done()
            .AddPropertyType()
                .WithAlias("size").WithName("Size").WithDataTypeId(Constants.DataTypes.DropDownSingle)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DropDownListFlexible).WithVariations(ContentVariation.Nothing)
                .Done()
            .Build();
        _page.AllowedAsRoot = true;
        await ContentTypeService.CreateAsync(_page, Constants.Security.SuperUserKey);

        // The framework: generic abilities, the change set service applying inside an Umbraco scope, and the
        // CMS document definition.
        var abilities = new AbilityRegistry();
        var definitions = new EntityDefinitionRegistry();
        var itemTypes = new ItemTypeRegistry();
        VetoHandler.Applied.Clear();
        VetoHandler.SideEffects.Clear();
        _changeSets = new ChangeSetService(new InMemoryChangeSetStore(), abilities, definitions, itemTypes, GetRequiredService<IEventAggregator>(), GetRequiredService<IBackgroundTaskQueue>(), TimeSpan.Zero);
        _draftSets = new DraftSetService(
            ContentEditingService,
            GetRequiredService<PropertyEditorCollection>(),
            GetRequiredService<IJsonSerializer>(),
            GetRequiredService<ICoreScopeProvider>(),
            GetRequiredService<IEventAggregator>(),
            GetRequiredService<IAuditService>());
        _documents = new CmsDocumentDefinition(
            ContentTypeService,
            GetRequiredService<IDataTypeService>(),
            GetRequiredService<ILanguageService>(),
            ContentEditingService,
            GetRequiredService<IEntityService>(),
            _draftSets,
            GetRequiredService<IUserService>(),
            GetRequiredService<IContentPermissionService>(),
            GetRequiredService<IJsonSerializer>(),
            itemTypes,
            new CmsScopeUnitOfWorkProvider(GetRequiredService<ICoreScopeProvider>()),
            () => Constants.Security.SuperUserKey,
            GetRequiredService<IPropertyEditorSchemaService>());
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
            .Add(new FailingOperationAbility())
            .Add(new FlakyOperationAbility());

        _agent = new Agent(abilities, new AbilityInvoker(abilities, _changeSets));
    }

    [Test]
    public async Task The_schema_comes_from_the_document_type_and_its_block_editors()
    {
        var subtypes = await _agent.OkAsync("entity_getSchema", """{ "entityType": "document" }""");
        Assert.That(subtypes.GetProperty("subtypes").EnumerateArray().Select(s => s.GetProperty("alias").GetString()), Does.Contain("landingPage"));

        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "document", "subtype": "landingPage" }""");
        Assert.That(schema.GetProperty("cultures").EnumerateArray().Select(c => c.GetString()), Is.EquivalentTo(new[] { "en-US", "da-DK" }));
        Assert.That(schema.GetProperty("fields").GetProperty("properties").GetProperty("subtitle").GetProperty("x-variations").GetString(), Is.EqualTo("Culture"));

        var fields = schema.GetProperty("fields").GetProperty("properties");
        Assert.That(TypesIn(fields.GetProperty("blocks")), Is.EquivalentTo(new[] { "hero", "textBlock", "cardGroup" }));
        Assert.That(TypesIn(fields.GetProperty("grid")), Is.EquivalentTo(new[] { "twoColumns", "textBlock" }));
        Assert.That(fields.GetProperty("grid").GetProperty("x-item-layout").GetProperty("required")[0].GetString(), Is.EqualTo("columnSpan"));

        var hero = await _agent.OkAsync("entity_getSchema", """{ "entityType": "document", "subtype": "landingPage", "itemTypes": ["hero", "twoColumns"] }""");
        Assert.That(hero.GetProperty("itemTypes").GetProperty("hero").GetProperty("settings").GetProperty("properties").TryGetProperty("theme", out _), Is.True);
        var areas = hero.GetProperty("itemTypes").GetProperty("twoColumns").GetProperty("fields").GetProperty("properties").EnumerateObject()
            .Where(p => p.Value.TryGetProperty("x-item-types", out _)).Select(p => p.Name);
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
        var hero = loaded.Root.ItemsOf("blocks").Single(b => b.Key == page.Hero);
        Assert.Multiple(() =>
        {
            Assert.That(hero.VariantOrNull(new VariantId("da-DK", null))!.Fields["heading"]!.ToString(), Is.EqualTo("Sommerudsalg"));
            Assert.That(hero.Settings!.Fields["theme"]!.ToString(), Is.EqualTo("dark"));
            var row = loaded.Root.ItemsOf("grid")[0];
            Assert.That(row.Layout!.Fields["columnSpan"]!.GetValue<int>(), Is.EqualTo(12));
            Assert.That(row.ItemsOf("left")[0].Key, Is.EqualTo(page.AreaText));
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
        var working = (await _changeSets.GetWorkingStateAsync(_changeSets.Get(_agent.ChangeSetId!.Value), "document", page))!;
        var problems = (await _documents.ValidateAsync(working)).Select(p => $"{p.Path}|{p.Culture}").ToList();

        Assert.That(problems, Is.EquivalentTo(new[] { "subtitle|da-DK", $"{empty}/heading|en-US", $"{empty}/heading|da-DK" }), string.Join(", ", problems));

        // The generic structure check agrees with Umbraco, including about the English-only hero.
        var generic = await _agent.OkAsync("changeSet_validate", SetJson());
        Assert.That(generic.GetProperty("entities")[0].GetProperty("problems").EnumerateArray().Select(p => $"{p.GetProperty("path").GetString()}|{p.GetProperty("culture").GetString()}"),
            Is.EquivalentTo(new[] { "subtitle|da-DK", $"{empty}/heading|en-US", $"{empty}/heading|da-DK" }));
    }

    [Test]
    public async Task Umbraco_validation_problems_inside_a_block_in_a_block_come_back_on_its_path()
    {
        await _agent.StartChangeSetAsync("Cards");
        var page = await CreatePageAsync();
        await SetAsync(page, "", "subtitle", "\"Sun and sand\"", "en-US");
        var group = await AddItemAsync(page, "blocks", "cardGroup", "{}");
        await AddItemAsync(page, $"{group}/cards", "card", """{ "title": "Fine" }""");
        var empty = await AddItemAsync(page, $"{group}/cards", "card", "{}");

        var working = (await _changeSets.GetWorkingStateAsync(_changeSets.Get(_agent.ChangeSetId!.Value), "document", page))!;
        var problems = (await _documents.ValidateAsync(working)).Select(p => p.Path).ToList();

        Assert.That(problems, Is.EquivalentTo(new[] { $"{empty}/title" }), string.Join(", ", problems));
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
        var refused = (await _agent.OkAsync("entity_create", """{ "entityType": "document", "subtype": "landingPage", "culture": "en-US", "fields": { "name": "Refused", "subtitle": "No" } }"""))
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
    public async Task A_page_deleted_in_the_backoffice_while_a_set_edits_it_is_still_summarized_and_refused_on_apply()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await SetAsync(page.Key, "", "subtitle", "\"Sun, sand and sea\"", "en-US");
        ContentService.Delete(ContentService.GetById(page.Key)!);

        var summary = await _agent.OkAsync("changeSet_summarize", SetJson());
        Assert.That(summary.GetProperty("entities")[0].GetProperty("entity").GetProperty("key").GetGuid(), Is.EqualTo(page.Key));
        await _agent.FailAsync("changeSet_apply", SetJson());
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
            { "changeSetId": "{{_agent.ChangeSetId}}", "entityType": "document", "key": "{{page.Key}}", "path": "subtitle", "culture": "en-US", "keep": "theirs" }
            """);
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(ContentService.GetById(page.Key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Changed in the backoffice"));
    }

    [Test]
    public async Task Content_changes_in_a_change_set_are_a_CMS_draft_set_that_records_only_what_changed()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        await _agent.StartChangeSetAsync("Edit");
        await SetAsync(page.Key, "", "subtitle", "\"Sun, sand and sea\"", "en-US");

        // The change set holds a CMS draft set for content, which the backoffice can read and preview.
        var draftId = _changeSets.Get(_agent.ChangeSetId!.Value).Drafts["Content"];
        var draft = _draftSets.Get(draftId)!;
        var inDraft = (await _draftSets.GetAsync(draftId, new GuidUdi(Constants.UdiEntityType.Document, page.Key)))!;
        Assert.Multiple(() =>
        {
            Assert.That(draft.Name, Is.EqualTo("Edit"));
            Assert.That(draft.Entities.Select(e => e.ToString()), Is.EqualTo(new[] { $"umb://document/{page.Key:N}" }));
            Assert.That(inDraft.Values.Single(v => v.Alias == "subtitle" && v.Culture == "en-US").Value!.ToString(), Is.EqualTo("Sun, sand and sea"));
            Assert.That(ContentService.GetById(page.Key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Sun and sand"), "live is untouched");
        });

        // Setting it back is no longer a change.
        await SetAsync(page.Key, "", "subtitle", "\"Sun and sand\"", "en-US");
        await Task.Delay(20);
        var content = ContentService.GetById(page.Key)!;
        content.SetValue("subtitle", "Changed in the backoffice", "en-US");
        ContentService.Save(content);
        var summary = await _agent.OkAsync("changeSet_summarize", SetJson());
        Assert.That(summary.GetProperty("rebased")[0].GetProperty("conflicts").GetArrayLength(), Is.Zero, "the draft set no longer changes the subtitle");

        // Discarding the change set discards the draft set.
        await _agent.OkAsync("changeSet_discard", SetJson());
        Assert.That(_draftSets.Get(draftId), Is.Null);
    }

    [Test]
    public async Task A_draft_set_works_on_its_own_with_no_abilities()
    {
        // Everything here is CMS only: content editing and the draft set, as the backoffice would use them.
        var key = Guid.NewGuid();
        var created = await ContentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = _page.Key,
                Variants = [new VariantModel { Culture = "en-US", Name = "Plain" }],
                Properties = [new PropertyValueModel { Alias = "subtitle", Culture = "en-US", Value = "One" }],
            },
            Constants.Security.SuperUserKey);
        Assert.That(created.Success, Is.True);

        var udi = new GuidUdi(Constants.UdiEntityType.Document, key);
        var draft = _draftSets.Create("Rename");
        await _draftSets.StageChangesAsync(draft.Id, udi,
        [
            new DraftValue(DraftSetService.NameAlias, "en-US", null, JsonValue.Create("Renamed")),
            new DraftValue("subtitle", "en-US", null, JsonValue.Create("Two")),
        ]);
        Assert.That((await _draftSets.GetAsync(draft.Id, udi))!.Names["en-US"], Is.EqualTo("Renamed"));

        // Someone changes the subtitle meanwhile: a conflict. They keep the live value.
        await Task.Delay(20);
        var content = ContentService.GetById(key)!;
        content.SetValue("subtitle", "Three", "en-US");
        ContentService.Save(content);
        var conflict = (await _draftSets.GetRebaseAsync(draft.Id)).Single().Conflicts.Single();
        Assert.That((conflict.Alias, conflict.Draft!.ToString(), conflict.Live!.ToString()), Is.EqualTo(("subtitle", "Two", "Three")));
        await _draftSets.ResolveAsync(draft.Id, udi, "subtitle", "en-US", null, DraftKeep.Live);

        var errors = (await _draftSets.ValidateAsync(draft.Id, Constants.Security.SuperUserKey))[udi];
        Assert.That(errors.Select(e => $"{e.Alias}|{e.Culture}|{e.JsonPath}|{string.Join(" ", e.ErrorMessages)}"), Is.Empty);
        await _draftSets.ApplyAsync(draft.Id, Constants.Security.SuperUserKey);
        content = ContentService.GetById(key)!;
        Assert.Multiple(() =>
        {
            Assert.That(content.GetCultureName("en-US"), Is.EqualTo("Renamed"));
            Assert.That(content.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Three"));
            Assert.That(draft.Applied, Is.True);
        });

        // Applying wrote to the CMS audit log, and raised the applied notification.
        var audit = await GetRequiredService<IAuditService>().GetItemsByKeyAsync(key, UmbracoObjectTypes.Document, 0, 10, Direction.Descending, null, [AuditType.Custom]);
        Assert.That(audit.Items.Select(i => i.Comment), Does.Contain("Applied with draft set 'Rename'"));
        Assert.That(VetoHandler.Applied, Does.Contain("draft set: Rename"));
    }

    [Test]
    public async Task A_package_can_veto_applying_a_change_set_or_its_draft_set_with_a_CMS_notification()
    {
        // Vetoed at the change set: nothing runs.
        await _agent.StartChangeSetAsync("Not today");
        var page = await BuildSummerPageAsync();
        var refused = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.That(refused.GetProperty("message").GetString(), Is.EqualTo("Applying was refused. Change sets called 'Not today' can't be applied."));
        Assert.That(ContentService.GetById(page.Key), Is.Null);

        // Vetoed at the CMS draft set: the Content step fails and rolls back.
        await _agent.StartChangeSetAsync("Not this content");
        page = await BuildSummerPageAsync();
        refused = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.That(refused.GetProperty("message").GetString(), Does.Contain("Content failed and was rolled back").And.Contain("Draft sets called 'Not this content' can't be applied."));
        Assert.That(ContentService.GetById(page.Key), Is.Null);

        // Applied: both notifications are raised.
        await _agent.StartChangeSetAsync("Fine");
        await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(VetoHandler.Applied, Is.EquivalentTo(new[] { "draft set: Fine", "change set: Fine" }));
    }

    [Test]
    public async Task An_agent_gets_the_CMS_permissions_of_the_user_it_acts_for()
    {
        await _agent.StartChangeSetAsync("Create");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("changeSet_apply", SetJson());

        // A user who may browse content but not update it can read the page, and can't change it.
        var reader = await CreateUserAsync("Readers", ActionBrowse.ActionLetter);
        _agent.UserKey = reader;
        await _agent.StartChangeSetAsync("Edit");
        await _agent.OkAsync("entity_get", $$"""{ "entityType": "document", "key": "{{page.Key}}" }""");
        var refused = await _agent.FailAsync("entity_setValue", $$"""{ "entityType": "document", "key": "{{page.Key}}", "elementPath": "", "field": "subtitle", "value": "Changed", "culture": "en-US" }""");
        Assert.Multiple(() =>
        {
            Assert.That(refused.GetProperty("status").GetString(), Is.EqualTo("AccessDenied"));
            Assert.That(refused.GetProperty("message").GetString(), Is.EqualTo("You don't have permission to update this document."));
        });

        // A set made by someone who may change the page, applied by the reader: refused, nothing changes.
        _agent.UserKey = Constants.Security.SuperUserKey;
        await _agent.StartChangeSetAsync("Edit for the reader");
        await SetAsync(page.Key, "", "subtitle", "\"Changed\"", "en-US");
        _agent.UserKey = reader;
        refused = await _agent.FailAsync("changeSet_apply", SetJson());
        Assert.Multiple(() =>
        {
            Assert.That(refused.GetProperty("status").GetString(), Is.EqualTo("AccessDenied"));
            Assert.That(ContentService.GetById(page.Key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Sun and sand"));
        });
    }

    [Test]
    public async Task A_draft_set_applied_inside_a_scope_that_rolls_back_is_not_applied_and_raises_nothing()
    {
        var key = Guid.NewGuid();
        await ContentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = _page.Key,
                Variants = [new VariantModel { Culture = "en-US", Name = "Plain" }],
                Properties = [new PropertyValueModel { Alias = "subtitle", Culture = "en-US", Value = "One" }],
            },
            Constants.Security.SuperUserKey);
        var udi = new GuidUdi(Constants.UdiEntityType.Document, key);
        var draft = _draftSets.Create("Rolled back");
        await _draftSets.StageChangesAsync(draft.Id, udi, [new DraftValue("subtitle", "en-US", null, JsonValue.Create("Two"))]);

        // The draft set's own scope completes, but the outer one doesn't.
        using (GetRequiredService<ICoreScopeProvider>().CreateCoreScope())
        {
            await _draftSets.ApplyAsync(draft.Id, Constants.Security.SuperUserKey);
        }

        Assert.Multiple(() =>
        {
            Assert.That(ContentService.GetById(key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("One"));
            Assert.That(draft.Applied, Is.False, "it can still be applied");
            Assert.That(VetoHandler.Applied, Is.Empty, "no applied notification for a rollback");
        });

        await _draftSets.ApplyAsync(draft.Id, Constants.Security.SuperUserKey);
        Assert.That(ContentService.GetById(key)!.GetValue<string>("subtitle", "en-US"), Is.EqualTo("Two"));
        Assert.That(VetoHandler.Applied, Is.EqualTo(new[] { "draft set: Rolled back" }));
    }

    private async Task<Guid> CreateUserAsync(string name, params string[] permissions)
    {
        var group = new UserGroup(GetRequiredService<IShortStringHelper>())
        {
            Alias = name.ToLowerInvariant(),
            Name = name,
            Permissions = permissions.ToHashSet(),
            StartContentId = Constants.System.Root,
            HasAccessToAllLanguages = true,
        };
        group.AddAllowedSection(Constants.Applications.Content);
        Assert.That((await GetRequiredService<IUserGroupService>().CreateAsync(group, Constants.Security.SuperUserKey)).Success, Is.True);

        var created = await GetRequiredService<IUserService>().CreateAsync(
            Constants.Security.SuperUserKey,
            new UserCreateModel { Email = $"{group.Alias}@example.com", UserName = $"{group.Alias}@example.com", Name = name, UserGroupKeys = new HashSet<Guid> { group.Key } },
            approveUser: true);
        Assert.That(created.Success, Is.True, created.Status.ToString());
        return created.Result.CreatedUser!.Key;
    }

    [Test]
    public async Task A_side_effect_with_retries_runs_on_Umbracos_background_queue_after_the_content_commits()
    {
        FlakyOperationAbility.FailuresLeft = 1;
        await _agent.StartChangeSetAsync("Content, then a flaky operation");
        var page = await BuildSummerPageAsync();
        await _agent.OkAsync("test_flaky", "{}");
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(ContentService.GetById(page.Key), Is.Not.Null);

        // A site runs Umbraco's queued hosted service; the test host doesn't start it, so the test does.
        var processor = new QueuedHostedService(GetRequiredService<IBackgroundTaskQueue>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<QueuedHostedService>.Instance);
        await processor.StartAsync(CancellationToken.None);

        // Wait for the notification.
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (VetoHandler.SideEffects.IsEmpty && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        await processor.StopAsync(CancellationToken.None);
        Assert.That(VetoHandler.SideEffects.TryPeek(out var completed), Is.True, "the background queue never ran the side effect");
        Assert.Multiple(() =>
        {
            Assert.That(completed!.AbilityId, Is.EqualTo("test.flaky"));
            Assert.That(completed.Result.Success, Is.True);
            Assert.That(completed.Attempts, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Editors_the_definition_has_no_rule_for_are_fields_described_by_Umbracos_value_schemas()
    {
        var schema = await _agent.OkAsync("entity_getSchema", """{ "entityType": "document", "subtype": "landingPage" }""");
        var fields = schema.GetProperty("fields").GetProperty("properties");
        var accent = fields.GetProperty("accent");
        var launchDate = fields.GetProperty("launchDate");
        var size = fields.GetProperty("size");
        Assert.Multiple(() =>
        {
            Assert.That(accent.TryGetProperty("type", out _), Is.True, $"the eyedropper describes its value: {accent}");
            Assert.That(launchDate.GetProperty("format").GetString(), Is.EqualTo("date-time"));
            Assert.That(size.GetProperty("items").GetProperty("type").GetString(), Is.EqualTo("string"), size.ToString());
        });

        // Set whole, in the editor's own format, and saved by Umbraco.
        await _agent.StartChangeSetAsync("Accent");
        var page = await CreatePageAsync();
        await SetAsync(page, "", "subtitle", "\"Sun and sand\"", "en-US");
        await SetAsync(page, "", "accent", "\"#336699\"");
        await _agent.OkAsync("changeSet_apply", SetJson());
        Assert.That(ContentService.GetById(page)!.GetValue<string>("accent"), Is.EqualTo("#336699"));
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
        => (await _agent.OkAsync("entity_create", """{ "entityType": "document", "subtype": "landingPage", "culture": "en-US", "fields": { "name": "Summer" } }"""))
            .GetProperty("key").GetGuid();

    private Task<JsonElement> SetAsync(Guid page, string elementPath, string field, string valueJson, string? culture = null)
        => _agent.OkAsync("entity_setValue", $$"""{ "entityType": "document", "key": "{{page}}", "elementPath": "{{elementPath}}", "field": "{{field}}", "value": {{valueJson}}{{Culture(culture)}} }""");

    private async Task<string> AddItemAsync(Guid page, string fieldPath, string itemType, string fieldsJson, string? culture = null)
        => (await _agent.OkAsync("entity_addItem", $$"""{ "entityType": "document", "key": "{{page}}", "fieldPath": "{{fieldPath}}", "itemType": "{{itemType}}", "fields": {{fieldsJson}}{{Culture(culture)}} }"""))
            .GetProperty("path").GetString()!;

    private string SetJson() => $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""";

    private static string Culture(string? culture) => culture is null ? string.Empty : $", \"culture\": \"{culture}\"";

    private static IEnumerable<string?> TypesIn(JsonElement field) => field.GetProperty("x-item-types").EnumerateArray().Select(t => t.GetProperty("alias").GetString());

    private async Task<IContentType> CreateElementTypeAsync(string alias, ContentVariation variation, params (string Alias, ContentVariation ContentVariation, bool Mandatory)[] properties)
    {
        var builder = new ContentTypeBuilder().WithAlias(alias).WithName(alias).WithIsElement(true).WithContentVariation(variation);
        foreach (var property in properties)
        {
            builder.AddPropertyType()
                .WithAlias(property.Alias).WithName(property.Alias)
                .WithDataTypeId(Constants.DataTypes.Textbox).WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .WithValueStorageType(ValueStorageType.Nvarchar).WithVariations(property.ContentVariation).WithMandatory(property.Mandatory)
                .Done();
        }

        var elementType = builder.Build();
        await ContentTypeService.CreateAsync(elementType, Constants.Security.SuperUserKey);
        return elementType;
    }

    private async Task<IDataType> CreateEditorDataTypeAsync(string editorAlias, string name)
    {
        var dataType = new DataType(GetRequiredService<PropertyEditorCollection>()[editorAlias], GetRequiredService<IConfigurationEditorJsonSerializer>())
        {
            Name = name,
            DatabaseType = ValueStorageType.Nvarchar,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow,
        };

        await GetRequiredService<IDataTypeService>().CreateAsync(dataType, Constants.Security.SuperUserKey);
        return dataType;
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

    /// <summary>Fails its first <see cref="FailuresLeft"/> runs, then succeeds: a side effect worth retrying.</summary>
    [Ability("test.flaky", "Flaky", Kind = AbilityKind.Write, Retries = 2, Description = "Fails a few times, then works.")]
    private sealed class FlakyOperationAbility : AbilityBase<JsonObject, object>
    {
        public static int FailuresLeft { get; set; }

        protected override Task<AbilityResult> ExecuteAsync(JsonObject input, AbilityContext context, CancellationToken cancellationToken)
            => FailuresLeft-- > 0 ? throw new InvalidOperationException("Not yet.") : Task.FromResult(Success(new { ran = true }));
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

    private sealed class VetoHandler :
        INotificationHandler<ChangeSetApplyingNotification>,
        INotificationHandler<DraftSetApplyingNotification>,
        INotificationHandler<ChangeSetAppliedNotification>,
        INotificationHandler<DraftSetAppliedNotification>,
        INotificationHandler<ChangeSetSideEffectCompletedNotification>
    {
        public static System.Collections.Concurrent.ConcurrentQueue<ChangeSetSideEffectCompletedNotification> SideEffects { get; } = new();

        public void Handle(ChangeSetSideEffectCompletedNotification notification) => SideEffects.Enqueue(notification);

        public static List<string> Applied { get; } = [];

        public void Handle(ChangeSetApplyingNotification notification)
        {
            if (notification.ChangeSet.Name == "Not today")
            {
                notification.CancelOperation(new EventMessage("Test", "Change sets called 'Not today' can't be applied.", EventMessageType.Error));
            }
        }

        public void Handle(DraftSetApplyingNotification notification)
        {
            if (notification.DraftSet.Name == "Not this content")
            {
                notification.CancelOperation(new EventMessage("Test", "Draft sets called 'Not this content' can't be applied.", EventMessageType.Error));
            }
        }

        public void Handle(ChangeSetAppliedNotification notification) => Applied.Add($"change set: {notification.ChangeSet.Name}");

        public void Handle(DraftSetAppliedNotification notification) => Applied.Add($"draft set: {notification.DraftSet.Name}");
    }

    /// <summary>Calls the AI tools by name with JSON arguments, as a model would.</summary>
    private sealed class Agent
    {
        private readonly Dictionary<string, AIFunction> _tools;

        public Agent(AbilityRegistry abilities, AbilityInvoker invoker)
            => _tools = AbilityAITools.Create(abilities, invoker, () => new AbilityContext(UserKey, ChangeSetId)).ToDictionary(t => t.Name);

        public Guid? ChangeSetId { get; private set; }

        /// <summary>The user the agent acts for. It gets their permissions, no more.</summary>
        public Guid UserKey { get; set; } = Constants.Security.SuperUserKey;

        public async Task StartChangeSetAsync(string name)
        {
            ChangeSetId = null;
            ChangeSetId = (await OkAsync("changeSet_create", $$"""{ "name": "{{name}}" }""")).GetProperty("changeSetId").GetGuid();
        }

        public async Task<JsonElement> OkAsync(string tool, string json)
        {
            var result = await CallAsync(tool, json);
            Assert.That(result.GetProperty("success").GetBoolean(), Is.True, $"{tool} failed: {result}");
            return result.GetProperty("result");
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
