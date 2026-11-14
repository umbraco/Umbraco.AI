# Abilities, entities and change sets: proof of concept

> **Status:** Experiment. Not a product, not shipped, not wired into the monorepo build. It exists to
> test the design in the "Abilities, entities and change sets" doc before anything is built for real.

It tests one claim: if packages declare **abilities** and **entity definitions**, and Write calls are
recorded into **change sets**, then "create an entity from a description" needs no per-product code. That
covers a discount with a rule tree, a page with nested blocks, or a workflow whose steps form a graph.

**Reviewing it?** Start with [REVIEWING.md](REVIEWING.md): what to read, in what order, and how the pieces connect. The
design is in [docs/plans/abilities-entities-change-sets/ARCHITECTURE.md](../../docs/plans/abilities-entities-change-sets/ARCHITECTURE.md).

## Run it

```bash
cd experiments/abilities-poc
dotnet test
```

The folder has its own `Directory.Build.props`, `Directory.Packages.props` and `NuGet.config`, so the
monorepo's central packages and versioning don't apply. Only nuget.org is needed. The CMS tests start a real
Umbraco 18 on SQLite through `Umbraco.Cms.Tests.Integration`, and take about 15 seconds.

## What is here

| Project | Plays the part of | Depends on |
| --- | --- | --- |
| `Umbraco.Abilities` | The contract and runtime: abilities, entity definitions, the path editor, change sets, the invoker | Nothing |
| `Umbraco.Abilities.AI` | Umbraco.AI's adapter: every ability becomes an M.E.AI `AIFunction` | Contract, `Microsoft.Extensions.AI.Abstractions` |
| `Umbraco.Abilities.Samples` | Product packages: CMS pages with nested blocks, Commerce collections and discounts, a third-party rule type, an email package, Automate workflows | Contract only |
| `Umbraco.Abilities.Cms` | The real entity definition for CMS documents, on `IContentEditingService`, and the adapter that stages content changes into a CMS draft set | Contract, Umbraco 18, the draft sets stand-in |
| `Umbraco.Cms.DraftSets` | A stand-in for a Draft Sets feature in CMS core: per-property content changes held apart from live, read-through, rebase and conflicts, apply in one scope | Umbraco 18 only: no Abilities |
| `Umbraco.Abilities.Tests` | A scripted model calling the AI tools with JSON arguments | Contract, AI adapter, samples |
| `Umbraco.Abilities.Cms.Tests` | The same, against a real Umbraco on SQLite | Contract, AI adapter, CMS definition, Umbraco test host |

Each design layer and where it lives:

| Layer | Code |
| --- | --- |
| 1. Abilities | `Abilities/Ability.cs`: `[Ability]` with `Kind` and `Reversible`, typed `AbilityBase`, failures with categories and paths, `EntityRef` |
| 2. Entity definitions | `Entities/`: fields (values, or items: typed elements each with its own key), entity and item references, an item type registry, a typed entity document (`EntityDocument`, `EntityElement`, `EntityItem`), a path editor generalised from Umbraco.AI's block path engine, and the generic entity abilities. `Entities/Models/`: the typed layer package developers write against (`EntityDefinitionBase<TModel>`, model attributes, `ModelEditAbilityBase`) |
| 3. Change sets | `ChangeSets/`: a recorded unit of "any Write ability call", working state worked out in memory, apply product by product (each all or nothing) and resumable, rebasing onto changes saved elsewhere meanwhile. Generalises the CMS Draft Sets PoC (`Umbraco.Cms.DraftSets`) |

## Writing a definition

A package describes an **editing contract**: C# classes that say what an AI, Automate or MCP can set on an entity.
The contract is not the product's domain model, and nothing goes on the domain model. The definition maps between
the two in load and save. The framework works out everything else from the contract.

```
Commerce's domain (DiscountReadOnly, AsWritable, provider settings)
        ↑  LoadModelAsync / SaveModelAsync: the mapping the package writes
DiscountEditModel (the editing contract)
        ↓  derived by the framework
structure, JSON Schema, paths, validation, edit abilities, change sets, rebasing
```

There are two ways to describe a contract, and fluent configuration wins where both are used. Attributes cover
everything; fluent configuration covers what request models have needed so far (key, version, name, ignore, fields with
descriptions, required and rules, items fields), and gets an option when a model needs one.

**Attributes**, for a class the package writes for the purpose (`Samples/Commerce/Commerce.cs`):

```csharp
[Entity("umbraco-commerce-discount", "Discount")]
public sealed class DiscountEditModel
{
    [Key] public Guid Key { get; set; }
    [EntityVersion] public long? Version { get; set; }
    [EntityName, Required] public string? Name { get; set; }
    public DateOnly? EndDate { get; set; }
    [EntityKey("document")] public Guid? LandingPage { get; set; }
    [Items(RuleGroup, Min = 1)] public List<DynamicItem> Rules { get; set; } = [];  // a plugin seam: see below
}

[ItemType("hero", "Hero")]          // an item type the package owns can be a class too (Samples/Cms/CmsPage.cs)
public sealed class Hero : PageBlock
{
    [Required] public Varying<string> Heading { get; set; } = [];
}
```

**Fluent configuration**, for a class the package doesn't own or won't annotate, such as a Management API request
model in another assembly. It needs no base class either: items name their own key property
(`Samples/Cms/CmsPage.cs`).

```csharp
ModelConfiguration.Configure<MediaRequestModel>(media => media
    .IsEntity("media", "Media")
    .HasKey(x => x.Id)
    .HasVersion(x => x.Version)
    .Ignore(x => x.UpdateDate)
    .Field(x => x.Name).IsName().Required()
    .Items(x => x.Crops, CropGroup));

ModelConfiguration.Configure<MediaCropRequestModel>(crop => crop
    .IsItemType("crop", "Crop")
    .HasKey(x => x.Id)
    .Field(x => x.Width).Required().Rule(new RangeAttribute(1, 10000)));
```

**The definition** maps to the domain and adds the rules the contract can't express:

```csharp
public sealed class DiscountDefinition(DiscountService discounts, ...) : EntityDefinitionBase<DiscountEditModel>(...)
{
    public override Task<DiscountEditModel?> LoadModelAsync(Guid key, CancellationToken ct) => /* domain → contract */;
    public override Task SaveModelAsync(DiscountEditModel model, CancellationToken ct)
    {
        var discount = discounts.GetDiscount(model.Key)?.AsWritable() ?? discounts.CreateDiscount(model.Key, model.Name!);
        discount.SetName(model.Name!).SetDates(...).SetRules(model.Rules.Select(ToRuleConfig));
        discounts.SaveDiscount(discount);
        return Task.CompletedTask;
    }
    protected override Task<IReadOnlyList<Problem>> ValidateModelAsync(DiscountEditModel d, CancellationToken ct)
        => /* ProblemAt(x => x.EndDate, "Must be on or after startDate.") */;
}
```

What the package no longer writes:

| Was hand-written | Now comes from |
| --- | --- |
| The structure: fields, types, required, allowed values, references, items fields and their limits | Property types, plus attributes or fluent configuration |
| JSON Schema for structured values (a focal point) | `JsonSchemaExporter` over the property type |
| Culture variation | `Varying<T>` (a value per culture), on value and items fields |
| Block settings and grid placement shapes | `[ItemSettings]`, `[ItemLayout]`, an items field's layout type |
| Turning the contract into the working copy and back | Done both ways, keeping item keys and giving each item its path |
| Range and format checks reported on entity paths (`rewards/{key}/percent`) | DataAnnotations, as attributes or fluent `Rule(...)`, per item, settings and placement |
| A path for a domain problem | `ProblemAt(x => x.EndDate, ...)`, or `ProblemAt(item, r => r.Percent, ...)` for an item |
| An ability that edits an entity, both directly and inside a change set | `ModelEditAbilityBase<TInput, TModel>`: one `Edit(model, input)` method |

`[Required]` and `[AllowedValues]` become part of the structure, so the generic validator checks them per culture.
Every other rule runs on the contract. Each problem is reported once. A renamed property is a compile error, not a
wrong string key at run time.

### What the mapping costs

The Commerce sample now has a domain shaped like the real one (`Samples/Commerce/Domain.cs`). Its read-only entity
has internal constructors and is changed through `AsWritable()`. Dates are UTC `DateTime`s, and rules and rewards
are a provider alias plus string settings, with no ids.

| Part | Statement lines |
| --- | --- |
| Editing contract | 11 |
| Mapping for the discount: load, save, dates, keys | about 35 |
| Domain rules (dates in order, unique code) and item registration | about 18 |
| Item type source: Commerce's providers as item types. Once per seam, for every provider, third parties' included | 17 |
| Provider mapper: an item ↔ provider alias plus string settings, with children for groups. Once for every provider | 20 |

The mapping is ordinary typed C#, roughly the size of a Management API mapper. It is the only per-entity code the
package writes besides the contract.

**One limit the mapping exposed:** Commerce's rules have no ids, so the definition derives each item's key from the
discount, the position and the provider alias. A rule's key is stable across loads. But a rule the agent added gets
a new key once saved, and moving a rule changes its key, so rebasing sees a move as a remove and an add. Stable ids
on Commerce's rule nodes would fix this. It is a change to suggest to the Commerce team, not something a mapping can
paper over.

### Entities whose structure is data

A CMS document type is defined in the backoffice. A workflow's step types are other packages' abilities. Neither has
a class to describe, so `CmsDocumentDefinition` and `WorkflowDefinition` implement `IEntityDefinition` over the typed
`EntityDocument`. There is still no `JsonObject` digging: they use `item.Settings` or `item.Layout`, not
`item["settings"]["fields"]`. The CMS definition builds Umbraco's own `BlockListValue`, `BlockGridValue`,
`BlockItemData`, `BlockPropertyValue`, `BlockGridLayoutItem` and `BlockItemVariation` instead of hand-built
envelopes, and Umbraco's serializer reads and writes them. Moving to the typed document took it from 621 lines to 599.

## Value types and handlers

A field's value is opaque to the framework unless its value type has a **handler**. That's how CMS properties already
work: a property can hold anything, and only its editor defines the model inside. Rich Text is markup plus blocks, with
the markup placing each block by key, and a Block Grid's layout is the Block Grid's own business. The framework doesn't
model those. It routes paths that go into a field to the field's handler (`IValueHandler`), which owns:

- the **paths** inside the value (`intro/markup`, `intro/blocks/{key}/text`);
- what **setting, adding, removing and moving** mean there, through the same tools (`entity.setValue`,
  `entity.addItem` …), so a model doesn't learn anything new;
- the **rules between its parts**, such as "every block tag in the markup has a block, and every block has a tag";
- its **problems**, reported on paths inside the value.

A value without a handler is set whole, described by its schema. For CMS properties that's the editor's own value
schema, from Umbraco's `IPropertyEditorSchemaService`, and Umbraco checks it on save.

| Value type | Who owns the model inside | Example |
| --- | --- | --- |
| A scalar or a reference | Nobody: there's nothing inside | Name, landing page |
| JSON with a schema | Nobody: it's set whole, and checked against the schema and on save | An eyedropper colour, a date (`date-time`) |
| JSON with a handler | The handler | Rich Text (`Samples/Cms/RichTextValue.cs`) |
| Keyed items | The framework's built-in handler | Discount rules, workflow steps, the sample page's blocks |

**Keyed items are the built-in value type** for products with fixed models. A discount is a fixed model whose `rules`
and `rewards` are items, with item types from Commerce's providers and child rules nested in group rules. They keep
everything the framework does for items: paths, problems, rebasing and references per item, and the item types offered
per user.

A typed model declares a handler with `[ValueHandler(typeof(...))]`. The CMS definition takes handlers by editor alias,
so a third party's editor can bring its own.

**What a handled value gives up**, until handlers add the capability:

- **Rebasing is per field.** Two people editing different blocks in the same Rich Text conflict. A handler could offer
  a merge.
- **References inside the value aren't seen**, so they aren't checked before save and give Deploy no dependencies. A
  handler could list them.

**Not done yet.** The CMS definition still maps Block List and Block Grid itself, to the built-in items. The next step
is to make those editors' handlers the ones Umbraco.AI already has (`IAIPropertyValueHandler`), so Abilities and
Umbraco.AI's tools share one handler model and the CMS definition loses its block code.

## Item types for plugin seams

Discount rules and rewards, and Automate triggers and actions, are plugin seams. Products and third parties register
them, and no one controls them all. So nobody writes an item type per plugin. Each product describes its **registry**
once, with an `IItemTypeSource` per seam, and the item types follow from whatever is registered when they're needed. A
plugin installed later just shows up, and plugins write no Abilities code.

Both registries already carry what an item type needs. The PoC's stand-ins are shaped like the real ones
(Umbraco.Commerce.Core's providers, Umbraco.Commerce.Automate's triggers and actions):

| Item type needs | Commerce rule and reward providers (`Samples/Commerce/Providers.cs`) | Automate triggers and actions (`Samples/Automate/Plugins.cs`) |
| --- | --- | --- |
| Alias, name, description | `[DiscountRuleProvider(alias)]`, with Label and Description | `[Trigger]` / `[Action]`, with name and Description |
| Fields and their types | Setting definitions discovered from the provider's settings class, keeping each CLR type | The settings class's `[Field]` properties |
| Required and other rules | The setting's DataAnnotations (`ValidationRules`) | The setting's DataAnnotations |
| What a field picks | The setting's editor UI alias | The field's editor UI alias |
| Bindings | — | `[Field(SupportsBindings = true)]` |
| Who may use it | The Commerce section | `RequiredSections` |
| Nesting | A group rule holds child rules | — |

How the framework uses them:

- **Fields** come from `ItemTypeFields.For`, with the same type mapping as model classes. `[Required]` and
  `[AllowedValues]` become part of the structure. Other rules (`[Range]`) go on the field (`FieldDefinition.Rules`) and
  are checked on its path, since there's no class to carry them.
- **Editor UI aliases** map to references through a small table per product, for example Commerce's collection picker
  becomes a reference to `umbraco-commerce-collection`. An editor no table maps stays a plain value, and its
  description names the editor, so it still works with less guidance.
- **Labels** that are localization keys (`#ucDiscountRuleProviders_…Label`, Commerce's default) are treated as
  missing. The type is named by its alias, and its description tells the model so.
- **Bindings.** A setting that supports bindings accepts `{{trigger.orderId}}` in place of a value. The schema marks it
  with `x-bindings`, and type and reference checks wait until it resolves.
- **Availability.** An item type can say who may use it (`AvailableTo`, here from required sections). The schema offers
  a user only those, and adding one they may not use is refused when it's recorded.
- **In a typed contract** the field is `List<DynamicItem>`: items with a type, JSON fields and nested items. One mapper
  per seam turns them into the product's stored form (`ProviderItems`: provider alias plus string settings, children
  for groups), using each setting's CLR type.
- **Abilities** can be item types too (`AbilityItemTypes`), so workflow steps are abilities and Automate's actions side by side.

Classes per item type are still an option for types a package owns and wants to describe richly (the CMS page's
blocks), but they are no longer the way to do plugin seams.

What it found:

- **The registries already have enough.** Commerce keeps the CLR type, validation rules, editor alias and sort order
  per setting, and Automate keeps the same on its fields. Nothing had to be added to either model.
- **Localization-key labels cost the model guidance.** Commerce's provider labels and descriptions default to keys only
  the backoffice resolves. Real text on the attributes, or server-side text for those keys, would help.
- **Editor tables are upkeep.** Each product keeps one, and an editor it misses loses its meaning (a store picker
  becomes a plain string).
- **Rule keys still need stable ids** (above). The store picker is left unmapped on purpose, to show the fallback: the
  PoC has no store definition for it to point at.

## Identity: UDIs

An entity's identity is a UDI, as in Deploy and the CMS: `umb://document/{key}`. Entity types are UDI entity types
(`document`, `media`, `umbraco-commerce-discount`), and adding a definition registers its type with `UdiParser` unless
the CMS or a package already has. Change sets key their snapshots, versions and resolutions by UDI, and the CMS draft
set addresses what it changes by UDI.

Tools still take and return `{ "entityType": "document", "key": "<dashed guid>" }`. A UDI writes its key without
dashes, and the same key in two forms invites copy errors. They also accept a UDI wherever they take a reference, and
store it in the field's own form:

| Field | Given | Stored |
| --- | --- | --- |
| A reference to one type (`[EntityKey("document")]`) | `"umb://document/3f2a…"` | `"3f2a…-…"`, a dashed key |
| A reference to one of several types | `"umb://media/3f2a…"` | `{ "entityType": "media", "key": "3f2a…-…" }` |
| Either, with the wrong type | `"umb://media/3f2a…"` for a document | Refused: "Expected a document, not a media." |

The type names follow each product's Deploy package. Commerce Deploy defines `umbraco-commerce-discount`, but doesn't
register it or deploy discounts today, so the definition registers it. The sample's collection has no Commerce
equivalent, and `umbraco-automate-workflow` follows the convention but isn't checked against Automate.

Moving to UDIs found a bug: conflict resolutions were keyed by their own `type:key` string rather than the set's
shared identity. They matched only by coincidence, and stopped matching once identity changed. There is now one
identity function.

## What the CMS already gives us

Before building something, check whether the CMS has it. This is where the PoC landed:

| Need | Uses | Notes |
| --- | --- | --- |
| Entity identity | UDIs (`GuidUdi`, `UdiParser`) | See "Identity: UDIs" |
| What a value varies by | `ContentVariation` | Replaced the PoC's own `Variation` enum, which had the same four values |
| Block editor values | `BlockListValue`, `BlockGridValue`, `BlockItemData`, … | Read and written with Umbraco's serializer |
| A product's transaction, for content | `ICoreScopeProvider` | Behind `IUnitOfWorkProvider`, which stays because Commerce and Automate have their own connections |
| Packages reacting to, or vetoing, an apply | Notifications through `IEventAggregator` | `ChangeSetApplyingNotification` and `DraftSetApplyingNotification` are cancelable, like `ContentSavingNotification`. Then applied and discarded |
| A record of who applied what | The audit log | The draft set writes an entry per document it applies, inside its scope |
| Work after a transaction commits | Scopes: notifications published through `scope.Notifications`, and `Context.Enlist` actions | Both run only when the outermost scope commits. The draft set's applied notification and its "applied" flag use them, so neither happens when an outer scope rolls back |
| Who may do what | The CMS's own permission model, through `IContentPermissionService` | See "Authorization" |
| Side effects that need retries | `IBackgroundTaskQueue`, run by `QueuedHostedService`, with Polly | See "Retries". Webhooks weren't used, as they're going away |
| Results and problems | `Attempt<TResult, TStatus>` and `PropertyValidationError`, mirrored | `AbilityResult` has a status (`AbilityOperationStatus`: Success, InvalidInput, AccessDenied, Failed) and a result. `Problem` has `ErrorMessages`, culture and segment. The CMS definition passes Umbraco's messages straight through |

Checked, and not usable:

| CMS feature | Why not |
| --- | --- |
| Content versions, as the version a set started from | A save updates the current version in place; only publishing creates a new one. Tested against Umbraco 18: three saves left one version holding the last value. So the draft set keeps its own base values |
| Relations and tracked references, for references between entities | A relation's parent and child are node ids plus a CMS object type, so only entities with a CMS node can take part. A Commerce discount can't |
| The audit log, for non-CMS entities | Its object id is a node id. A Commerce entity can only be logged against no node, with its UDI in the comment |

Left for later:

- **Storage.** Change sets and draft sets are in memory, which doesn't work across load-balanced servers.

## Authorization

An agent acts for a user and gets that user's permissions, no more. `AbilityContext` carries the user's key, as the CMS's
services take it. Abilities never build their own permission model: a definition that has one implements
`IAuthorizingEntityDefinition`, and is asked whether a user may read, create or update an entity, in the cultures a
change touches. It answers with a reason when not.

| When | Who is checked |
| --- | --- |
| An edit is recorded into a change set | The user making it |
| A change set is applied | The user applying it, for every change, before validating. If any is refused, nothing is applied: the user applying may not be who made the changes, and permissions can change while a set is open |
| An entity is found | The user: results are only what they may read |
| An entity is read | The user: a refusal is an `AccessDenied` result |
| A product ability is run directly | The user, the same as inside a set |

The CMS definition asks the CMS. The user must have the Content section, then the document permission (browse, create
or update) on the document. For a new document that's its parent, or the root. Then they need access to the cultures
the change touches. Refusals are not correctable: retrying won't help, so an agent should stop and tell the user. The
in-memory samples stand in for each product's model with `SampleAccess`, where a content editor may not touch Commerce.

## Retries

A side effect that may fail for a while declares `[Ability(..., Retries = 3)]`. Applied in a change set, it runs on
Umbraco's `IBackgroundTaskQueue` once every change is applied, so a slow mail server doesn't hold up the apply. It is
retried with Polly (`Polly.Core` 8.5.0, the version an Umbraco 18 site already resolves through NPoco): exponential
backoff with jitter. Only `Failed` results are retried, never `InvalidInput` or `AccessDenied`. The outcome is a
`ChangeSetSideEffectCompletedNotification` with the result and the number of attempts, since the set is applied by then.
A side effect without retries runs inline, as before.

Two limits:

- **The queue is in memory.** Work still queued when the site restarts is lost. Durable side effects would need stored
  work, which ties into storage above.
- **Umbraco's `QueuedHostedService` processes the queue.** A site registers it (in `AddBackgroundJobs`). The integration
  test host doesn't, so the test starts Umbraco's own processor itself.

## CMS Draft Sets as a participant

The CMS is building Draft Sets for content. It shouldn't depend on Abilities, and Abilities shouldn't keep and replay
content edits itself when the CMS has its own drafts. So a product that keeps its own drafts implements
`IDraftingEntityDefinition`. The change set then holds one of the product's drafts and hands it that product's changes:

```
Change set (Abilities)
├── Content step  → a CMS draft set  (Umbraco.Cms.DraftSets: references Umbraco only)
├── Commerce step → the change set's own recorded changes, replayed
└── Side effects  → email.send, …
```

The draft set is the source of truth for content. Changes an AI makes are a CMS draft set the backoffice can review
and preview. The change set keeps only the draft set's id and the fact that it touched an entity. Products with no
drafts of their own (the in-memory samples) work as before. Both kinds mix in one set.

What the adapter (`CmsDocumentDefinition`) does:

| Change set needs | CMS draft set provides |
| --- | --- |
| Record an edit | `StageNew` for new content (whole), `StageChangesAsync` for existing content: only the property values, per culture and segment, that the edit changed. The adapter works them out by diffing the entity document before and after |
| The working state | `GetAsync`: live content with the set's changes over it, or the set's new content, in editor format |
| Rebase and conflicts | `GetRebaseAsync`: what others changed since, and the values both changed. Conflicts are per property value, not per block |
| Resolve | `ResolveAsync`: keep the draft's value or the live one |
| Apply its step | `ApplyAsync`: every document in one scope, all or nothing |
| Discard | `Discard` |

Because the draft set records per-property changes over live content, rebasing is automatic: someone else's change to
another property is simply kept.

**Found while building it**

1. **A one-language document failed validation and save** on a mandatory property in the other language.
   `ValidateUpdateAsync` checks every culture unless `ValidateContentUpdateModel.Cultures` says otherwise.
   `UpdateAsync` saves anyway but returns `PropertyValidationError` with problems in every culture. The draft set and
   the definition now validate only the cultures the document has, and treat "saved with problems" as decided by those
   problems alone. The definition had the same bug, hidden because every earlier test page existed in both languages.
2. **Conflicts are coarser than the generic diff.** A block property is one value, so two people editing different
   blocks in the same Block List conflict. Item-level merging needs block-level change tracking in the draft set.

### If core owned the change set

The other option is for CMS core's Draft Sets to *be* the change set. Every product already depends on CMS core, but
not on Abilities, so a core coordinator would reach all of them:

```
CMS core: Draft Sets = the change set
├── coordinator: one step per product, all or nothing per product, resumable, rebase and conflicts
├── IDraftSetParticipant, public: the seam IDraftingEntityDefinition is here
├── content participant, built by the CMS team
└── Commerce, Forms, … implement their own participant

Abilities, on top
├── structure, paths, generic edit tools, item types
├── records ability calls by staging them into a draft set
└── side effects after apply, approval
```

We chose the Abilities-owned coordinator for the PoC, because it moves faster. Moving the coordinator into core later
needs nothing in the participant contract to change, as long as core's Draft Sets is built with the points below in
mind.

### What core's Draft Sets must address to become the generic change set

| # | Must | Why |
| --- | --- | --- |
| 1 | Address entities by UDI (`umb://document/{key}`), even if v1 does only documents | Media created in the same set, then other products' entities, without a redesign |
| 2 | Record per-property changes (alias, culture, segment) over a base, not whole update models | An update model replaces every value, so it can only overwrite or refuse; per-property changes rebase |
| 3 | Keep a base version (and base values) per entity from its first change | To show what others changed since, and find conflicts. Content versions can't serve as the base: a save updates the current version in place, and only publishing creates a new one |
| 4 | Report conflicts in a neutral form: entity, property alias, culture, segment, base, draft and live values | So a coordinator, the backoffice or an agent can show and resolve them |
| 5 | Let the caller choose keys for new content | So other changes in the set can refer to content before it exists |
| 6 | Make reads draft-aware: get by key, and find, through a draft set | So whoever builds the set sees their own changes |
| 7 | Validate a whole set, per entity, only in the cultures each document has | Before anyone approves it; see finding 1 |
| 8 | Apply all or nothing in one scope, record that it applied, and refuse to apply twice | A coordinator applies products one after another and resumes after a failure |
| 9 | Raise CMS notifications: a cancelable applying, then applied and discarded | So packages can veto applying a set as they veto saving a document, and audit, caches and integrations can react. The stand-in does this |
| 10 | Make the draft set's id, create, apply and discard public, with no assumption about who started it | A coordinator, the backoffice or Automate can drive it |
| 11 | Shape the coordinator and the participant contract generically from day one, with content as its first participant | The step from content-only to the generic change set is then adding participants, not redesigning |
| 12 | Leave side effects, approval and the AI-facing structure language out of core | Core has no "operation to run afterwards". Approval is per caller. The structure language is a large, fast-moving API |

Recommended, not required: block-level change tracking inside block properties (finding 2), and capturing
`IContentEditingService` calls into an ambient draft set, as the original Draft Sets PoC does, so existing code is
drafted without a new API.

## What the tests show

| Claim | Test |
| --- | --- |
| A discount is built from text step by step, nothing is live until apply, and the result is stored | `CreationPromptTests.Discount_from_text_is_built_in_a_draft_set_and_only_goes_live_when_applied` |
| Problem paths are the same paths the edit abilities take, so a model can fix its own work | `Validation_reports_problems_by_path_and_the_agent_fixes_them_with_the_same_paths` |
| Bad edits are correctable Input failures with a path; edits never run outside a set | `Bad_edits_are_correctable_input_failures_with_paths` |
| A third party's rule type, written as a plain Commerce provider with no Abilities code, is creatable from text | `A_third_party_rule_type_is_creatable_with_no_change_to_commerce` |
| Every ability becomes an AI tool; only irreversible Writes are wrapped for approval | `Every_ability_is_an_AI_tool_and_only_irreversible_writes_need_approval` |
| One set holds a page with nested blocks, a discount referring to that new page and switched on, and an email to send afterwards; activating the discount is a change shown in its working state, the email runs after every change | `ChangeSetTests.A_campaign_spanning_content_commerce_and_a_side_effect_is_applied_together` |
| A failed step leaves earlier products' changes applied; applying again resumes at the failed step and applies nothing twice | `A_failed_step_leaves_earlier_steps_applied_and_applying_again_resumes` |
| A product whose save fails rolls back all of its changes in the set, which can then be fixed and resumed | `A_product_whose_save_fails_rolls_back_all_of_its_changes_and_can_be_fixed_and_resumed` |
| Changes saved elsewhere meanwhile that don't overlap the set's are rebased onto automatically, and both kept | `Changes_saved_elsewhere_that_do_not_overlap_are_rebased_onto_automatically` |
| A value both changed is a conflict, with both values shown, until one is kept | `A_value_both_changed_is_a_conflict_until_one_is_kept`, `Keeping_yours_overwrites_their_value` |
| Editing an item someone else removed is a conflict that can only keep theirs; the set's other edits still apply | `Editing_an_item_someone_else_removed_is_a_conflict_that_can_only_keep_theirs` |
| The same change made on both sides is not a conflict | `The_same_change_made_on_both_sides_is_not_a_conflict` |
| An entity whose product keeps version history is rebased from that history, with no snapshot in the set | `An_entity_with_version_history_is_rebased_from_its_history_without_a_snapshot` |
| If the starting version is gone from the history, the entity can't be rebased, and says why | `If_the_starting_version_is_gone_from_the_history_the_entity_cant_be_rebased` |
| Reads inside a set show its edits; reads outside show live data | `Reads_inside_a_set_show_its_edits_and_reads_outside_show_live_data` |
| Replay is deterministic, and keys handed to the model are the stored keys (for products that store item ids; see Commerce's derived keys) | `Replaying_a_set_gives_the_same_entity_every_time` |
| A class with no attributes and no base class is described fluently, and created from text like any other | `TypedModelTests.A_class_with_no_attributes_or_base_class_is_described_fluently`, `A_fluently_described_entity_is_created_from_text_like_any_other` |
| Commerce keeps its own model; the definition maps to it, and rule keys are derived because Commerce rules have no ids | `Commerce_keeps_its_own_model_and_the_definition_maps_to_it` |
| The structure (types, required, allowed values, references, items fields, variation, settings, placement, JSON Schema) comes from model classes and attributes | `TypedModelTests.The_structure_comes_from_the_model_class_and_its_attributes`, `Varying_values_settings_layout_and_json_values_are_described_too` |
| A model round-trips through its working copy, keeping item keys and giving each item its path | `A_model_round_trips_through_its_document_with_item_keys_and_paths` |
| Each problem is reported once, whether it comes from the structure, an attribute or a domain rule | `Each_problem_is_reported_once_whether_it_comes_from_the_structure_or_an_attribute` |
| Content changes in a change set are a CMS draft set: the backoffice can read it, it records only what changed, and discarding the set discards it | `CmsDocumentDefinitionTests.Content_changes_in_a_change_set_are_a_CMS_draft_set_that_records_only_what_changed` |
| The CMS draft set works on its own, with no abilities: stage, read through, conflict, resolve, validate, apply | `A_draft_set_works_on_its_own_with_no_abilities` |
| Entity types are UDI entity types, package types are registered, and references can be given as UDIs, stored in the field's own form, with the wrong type refused | `UdiTests` |
| A package can veto applying a change set or its CMS draft set with a cancelable notification, and both raise applied notifications; applying writes to the CMS audit log | `CmsDocumentDefinitionTests.A_package_can_veto_applying_a_change_set_or_its_draft_set_with_a_CMS_notification`, `A_draft_set_works_on_its_own_with_no_abilities` |
| An agent gets the permissions of the user it acts for: recording, applying (checked for the applying user, nothing applied if any change is refused), finding, reading and direct runs; with the CMS's real document permissions | `AuthorizationTests`, `CmsDocumentDefinitionTests.An_agent_gets_the_CMS_permissions_of_the_user_it_acts_for` |
| A draft set applied in a scope that then rolls back isn't applied and raises no applied notification | `A_draft_set_applied_inside_a_scope_that_rolls_back_is_not_applied_and_raises_nothing` |
| A side effect with retries is queued after apply and retried with backoff; it gives up after its limit; correctable input isn't retried; on real Umbraco it runs on the background queue | `SideEffectRetryTests`, `A_side_effect_with_retries_runs_on_Umbracos_background_queue_after_the_content_commits` |
| Rule and reward types come from Commerce's provider collections, with the label fallback and editor references; a provider registered later shows up; a group rule nests children and is stored as Commerce's rule tree | `PluginItemTypeTests` |
| Automate's triggers and actions come from its collections, settings can be bindings, and users are only offered the types they may use | `PluginItemTypeTests.Automates_triggers_and_actions_come_from_its_collections_and_settings_can_be_bindings`, `A_user_is_only_offered_plugin_types_they_may_use` |
| A value whose model is its editor's (Rich Text's markup plus blocks) is edited inside through its handler, with the same tools; the handler keeps markup and blocks in step and reports its problems on paths inside the value | `ValueHandlerTests` |
| Editors the CMS definition has no rule for are fields described by Umbraco's own value schemas | `CmsDocumentDefinitionTests.Editors_the_definition_has_no_rule_for_are_fields_described_by_Umbracos_value_schemas` |
| Workflow step types are derived from abilities | `WorkflowTests.Step_types_are_derived_from_abilities` |
| A workflow graph (edges as item references) is validated, and can refer to a discount created in the same set | `A_workflow_from_text_links_its_steps_and_can_refer_to_a_discount_in_the_same_set` |
| Names and values vary by culture; invariant values are shared across cultures | `CmsContentTests.Names_and_values_vary_by_culture_while_invariant_values_are_shared` |
| A value that varies by culture needs a known culture; an entity with no culture can't be applied | `A_value_that_varies_by_culture_needs_a_known_culture` |
| Each culture is validated separately, and problems say which culture | `Each_culture_is_validated_separately_and_problems_say_which` |
| Blocks in an invariant Block List hold per-culture values, and a block's settings element is edited and validated at `{item}/@settings` | `Block_values_vary_per_culture_inside_an_invariant_block_list_and_settings_are_their_own_element` |
| Block Grid: column span is a placement value at `{item}/@layout`, areas are items fields with limits, and a type can be allowed at the root and in areas | `Grid_blocks_have_placement_values_and_areas_that_are_items_fields` |
| A culture-variant block property holds a separate list per culture | `A_culture_variant_block_property_holds_a_list_per_culture` |
| A media picker and a multi-type picker refer to media and pages created in the same set | `Pickers_refer_to_media_and_pages_created_in_the_same_set` |

## What building it taught us

These feed back into the design doc.

1. **Approval depends on the call, not just the tool.** `email.send` and `commerce.activateDiscount` need
   approval when run directly. Inside a change set they are only recorded, so asking then is pointless; the
   approval belongs on `changeSet.apply`. M.E.AI's `ApprovalRequiredAIFunction` wraps a tool once, so the
   adapter can't express "only outside a set". The contract needs approval decided per call, from the
   context.
2. **Side effects are not checked when recorded.** An `email.send` with a bad recipient is
   accepted into the set and only fails at apply. Abilities need an optional "check this input without
   running it" hook, so a set can be validated fully before anyone approves it.
3. **Every read must be draft-aware.** `entity.find` first missed an existing entity renamed in the set.
   The generic reads now use the working state, but a product's own Read ability (e.g. "list discounts")
   would still show live data inside a set. This is the generic form of the CMS PoC's read-overlay problem.
4. **Caller-chosen keys solve references between new items.** Writing generated keys into the recorded
   input made replay deterministic, and let a discount refer to a page created in the same set, and a
   workflow step refer to a later step. No key remapping was needed.
   That holds only where the product stores item ids. Commerce's rules have none, so their keys are derived
   on load and change once saved (see "What the mapping costs").
5. **Problem paths that equal edit paths matter.** Because `rewards/{key}/percent` is both what
   validation reports and what `entity.setValue` takes (`elementPath` + `field`), the fix loop needs no
   translation.
6. **Abilities as item types works.** Reflecting an ability's input type into fields made Automate's step
   types come for free, including an entity reference from `[EntityKey]`.
7. **Typed values found an untyped bug.** The validator checked whole numbers with
   `JsonValue.TryGetValue<long>`. That works on parsed JSON but fails on a value holding a CLR `int`, which
   is what the CMS definition puts in once it reads Umbraco's typed `BlockGridLayoutItem.ColumnSpan`. Every
   grid page then failed validation. The check now reads the JSON text. A typed document must not assume its
   values came from parsing.
8. **Slots were fields all along.** The structure first had fields and, separately, slots: lists of typed items. But
   every existing model treats "a list of typed things" as a field with that value type: Umbraco's Block List is a
   property like any other, JSON Schema has an array of a `oneOf`, GraphQL a list of a union. So there is now one concept,
   a field, with value type `Items` (its item type group, min, max and placement values; a single block is `Max = 1`).
   What items add are properties of that value type: each item has a key, so paths (`rules/{key}/minTotal`), problems
   and rebasing work per item; the set of item types is open; and items are edited by adding, removing and moving.
   Paths didn't change. The schema has one `fields` object, where an items field lists its `x-item-types`; tools take
   a `fieldPath`; the typed layer has `[Items]` and fluent `.Items(...)`. It also removes a question that only existed
   because of the split: whether a property editor is a field or a slot. An editor declares its value type, and
   items are one value type.
9. **Values are their editor's model.** Items as a field type still assumed items sit directly in a field, and the
   Block Grid's placement values leaked into the language. But a CMS property can hold anything, and only its editor
   knows the model inside, which is how Umbraco.AI's update tools already work. So a value type can have a handler that
   owns its inner paths, operations, rules and problems, and the framework routes the same tools to it. Keyed items stay
   as the built-in value type for fixed models such as discounts. Umbraco 18 describes every core editor tried here
   with a value schema, including a date (`date-time`) and a dropdown (an array of strings). So the "no schema at all"
   fallback is mainly for third parties' editors.

## Applying a set

Products can each use their own connection string (Commerce, Automate and Umbraco.AI all can), so a draft
set never assumes one transaction across products. Each entity definition names its product's unit of
work, and applying a set runs in steps:

1. **One step per product**, in the order the set first touched them: all of that product's changes save
   in its own transaction, so they apply all or nothing.
2. **Side effects last**, one step each, once every change is applied.

A change set holds two kinds of thing, kept apart:

- **Changes** to entities, which can be reviewed, validated and saved per product. Generic edits are changes,
  and so is a product ability that changes an entity, if it says which entity it edits and how
  (`IEntityEditAbility`). `commerce.activateDiscount` does: inside a set it is recorded as a change to the
  discount and shows in its working state; run directly, it saves the discount as before.
- **Side effects**, such as `email.send`: any other Write ability. They change nothing that can be reviewed or
  undone, so they are listed separately as "after apply" and run only after every change is applied.

Progress is kept on the set after each step. If a product's step fails, it rolls back; if a side effect fails,
there is nothing to roll back. Either way earlier steps stay applied and the set is kept. Its applied products
can't be edited in it any more, the rest can be fixed, and applying again resumes at the failed step without
applying anything twice. `changeSet.summarize` lists the changes, the side effects and the plan, so a person
sees before approving which parts are all or nothing and what runs afterwards.

## Rebasing

A change set records edits by path, not copies of entities, so when someone else saves an entity the set is
editing, the set's edits are replayed onto the newer version instead of the apply being refused.

- The first time a set edits an existing entity, it remembers the version it started from. If the product keeps
  version history (`IVersionedEntityDefinition`), it remembers only the version number and loads that version from
  the history when needed. Otherwise it keeps its own snapshot.
- What someone else changed is the difference between that starting version and the live one; what the set changed
  is the difference between the starting version and the set's result (`EntityDiff`, the same for every product).
- Where they don't overlap, the set rebases automatically and `changeSet.summarize` lists what changed meanwhile.
- Where both changed the same value in the same variant, or one changed something inside an item the other removed,
  it's a conflict. `changeSet.validate` reports it with both values, apply is refused until it's resolved, and
  `changeSet.resolveConflict` keeps yours or theirs. Edits to an item someone else removed can only keep theirs.
- If both made the same change, it isn't a conflict. If the starting version is gone from the product's history
  (a version clean-up), the entity can't be rebased and validation says why.

The CMS document definition uses the snapshot for now: whether every draft save gets a new version that stays in
the history (version clean-up removes old ones) needs checking before it can rely on the history.

## Against a real Umbraco

`CmsDocumentDefinition` is a real definition for documents. One entity type, `document`, has a subtype per
document type. It builds the structure from the document type and its data types (Block List and Block Grid
configuration, settings element types, areas, variance), loads through `IContentEditingService` in the editor
format, writes block envelopes with layout, settings, grid areas and expose entries, validates with Umbraco's
own `ValidateCreateAsync` / `ValidateUpdateAsync`, and saves with `ContentCreateModel` / `ContentUpdateModel`
inside one Umbraco scope.

| Shown against Umbraco 18 (SQLite) | Test |
| --- | --- |
| The schema comes from the document type: cultures, varying fields, block types per items field, settings, grid areas and placement | `The_schema_comes_from_the_document_type_and_its_block_editors` |
| A culture-variant page with a Block List (per-culture block values and a settings element) and a Block Grid (column span, areas) built through the AI tools is stored by Umbraco as expected, and loads back to the same shape | `A_page_built_from_text_is_saved_by_Umbraco_with_its_blocks_settings_grid_and_cultures` |
| Umbraco's validation errors come back on the entity paths and cultures, and the generic check agrees with Umbraco | `Umbraco_validation_problems_come_back_on_the_entity_paths_and_cultures` |
| Editing an existing page keeps what the edit didn't touch (other cultures, settings, the grid) | `Editing_an_existing_page_keeps_everything_the_edit_did_not_touch` |
| A page refused on save (by a notification handler) rolls back the page saved before it in the same Umbraco scope | `A_failed_save_rolls_back_every_document_in_the_Umbraco_scope` |
| A side effect failing after Content's step leaves the content applied, and resuming doesn't save it again | `A_failed_side_effect_after_the_content_leaves_the_content_applied_and_the_set_resumable` |
| A backoffice change made meanwhile (Danish subtitle) is rebased onto; the set's block edit applies on top and both are kept | `Changes_saved_in_the_backoffice_meanwhile_are_rebased_onto_when_they_do_not_overlap` |
| A value changed in both the set and the backoffice is a conflict, with its culture, until one is kept | `A_value_changed_in_both_the_set_and_the_backoffice_is_a_conflict_to_resolve` |

What running against Umbraco taught us:

1. **Structure can depend on a subtype.** Documents are one entity type with a structure per document type,
   chosen at create. The contract gained `GetSubtypesAsync`, `DescribeAsync(subtype)` and `subtype` on create
   and schema. Media types and member types will need the same.
2. **An item exists only in the cultures it has values for.** Umbraco exposes a block in the cultures it has
   values in, and skips validating it elsewhere: an English-only hero is not part of the Danish page. The
   generic validator first flagged its missing Danish heading; it now follows the same rule (or every culture
   when an item has no values yet).
3. **Umbraco's block error paths need translating.** Errors inside an invariant block property carry no
   culture; it sits in a JSON path filter, `.contentData[1].values[?(@.alias == 'heading' && @.culture ==
   'en-US' ...)].value`, and `contentData[i]` counts only the blocks exposed in that culture. The definition
   maps these to `blocks/{key}/heading` with culture `en-US`.
4. **Umbraco saves invalid values and reports them afterwards.** The definition treats any reported error
   as a failed save, so the scope rolls back and nothing half-valid is applied.
5. **`UpdateDate` works to notice a change.** Umbraco has no check of its own on update; comparing `UpdateDate` tells a
   change set when to rebase.

Not covered yet: templates (an update would clear one), publishing, rich text with blocks, segments inside
blocks, culture-variant block properties against real Umbraco, error paths inside blocks nested in blocks,
a parent created in the same set, permissions (it runs as the super user), and editors other than text,
numbers, booleans, tags, content picker and blocks (they pass through as JSON, untested).

## Checked against the real models

The structure language was compared with the real code of Umbraco CMS (`main`, 18.5.0-rc),
Umbraco.Commerce (`dev`) and Umbraco.Automate (`v18/dev`). The CMS sample now follows the real content
model's shapes. What the contract gained from that:

| Added to the structure language | Why (real model) |
| --- | --- |
| `Variations` (the CMS's `ContentVariation`) on the entity and its fields, with `VariantId` narrowed per value | CMS property values carry a culture or segment only if their property varies that way, and Umbraco rejects values that don't match |
| Variant values stored per variant (`variants["en-US"].fields`), names as a culture-variant field | CMS names vary by culture; block values inside an invariant block property vary per culture |
| Culture-variant items fields (a list per culture) | A culture-variant block property holds one whole envelope per culture |
| `culture` / `segment` on `Problem` | The CMS's `PropertyValidationError` reports alias, culture and segment |
| `ItemTypeDefinition.SettingsShape`, edited at `{item}/@settings` | Blocks have a settings element type next to their content type |
| `FieldDefinition.ItemLayout` on an items field, edited at `{item}/@layout` | A Block Grid's column and row span belong to the block's place, not its values |
| `ItemTypeDefinition.AlsoIn` | Grid blocks can be allowed at the root and in areas |
| `Many`, `ReferencesAnyOf`, `FieldType.Json` | Tags and dropdowns are string arrays; a tree picker points at documents or media; focal points, crops and date-times are objects |

What still does not fit, and needs a decision rather than more code:

| Product | Gap | Consequence |
| --- | --- | --- |
| CMS | Blocks in rich text are placed by markup, not by item order | Adding a block to rich text needs an anchor in the markup; editing existing ones fits |
| CMS | Segments are not managed (no list to validate against); "expose" (which cultures a block exists in) is derived by the CMS on read | The CMS definition must produce expose entries and accept any segment |
| CMS | Update replaces all properties, and saves invalid values while returning errors | The definition must send the full working copy and treat returned errors as a failed apply |
| CMS | No concurrency token on update | Change sets rebase instead (see "Rebasing"); a change saved between validating and saving is still not caught |
| Commerce | A discount's rules are one recursive tree under a group rule (AND/OR is a setting); rule and reward nodes have no ids | Fits as an items field of one group rule with nested items fields, but the definition must assign item keys on load and drop them on save |
| Commerce | Validation is message-only and stops at the first error; rule and reward settings are not validated on write | Problems by path have to come from the definition, not from Commerce |
| Commerce | Saves use Commerce's own unit of work and connection, and can't join an Umbraco scope | No longer a gap: change sets don't assume a transaction across products (see "Applying a set") |
| Commerce | `Version` exists but isn't exposed, and conflicts are retried (last writer wins) | No change detection for discounts without a Commerce change |
| Automate | Steps are a flat list and edges are their own list carrying data (outcome, handle, filter); loops and parallel paths come from edges, not nesting | Item references on fields are not enough: the contract needs **edges as a first-class part of an entity** |
| Automate | Validation errors are strings with no path; unknown step types aren't caught at publish | Problems by path have to come from the definition |
| Automate | Bindings (`${ trigger.x }`) resolve to strings | Typed step inputs from bindings need an Automate change |

Two existing Umbraco.AI defects turned up on the way and were raised as issues:
[#746](https://github.com/umbraco/Umbraco.AI/issues/746) (Multi URL Picker links saved without a target)
and [#747](https://github.com/umbraco/Umbraco.AI/issues/747) (block settings use the content element type).

## What it does not prove

- **Real Commerce and Automate.** Only CMS documents have a real definition. The Commerce and Automate
  definitions are still stand-ins.
- **A real model.** The tests script the tool calls. No LLM chose them.
- **Persistence and scale.** Sets are in memory, and working state is recomputed by replaying every call
  on each read.
- **Access checks.** The `Access` failure category exists but nothing enforces permissions.
- **Opening a set in an editor.** Nothing loads a set's working state into a backoffice workspace.
- **Undo.** A failed apply is resumed, not undone. Nothing reverses a product's step that already applied.
- **Products sharing a database.** Products that do share a connection could apply in one transaction; the
  PoC always applies them one after another.
