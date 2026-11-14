# Abilities, entities and change sets: proof of concept

> **Status:** Experiment. Not a product, not shipped, not wired into the monorepo build. It exists to
> test the design in the "Abilities, entities and change sets" doc before anything is built for real.

It tests one claim: if packages declare **abilities** and **entity definitions**, and Write calls are
recorded into **change sets**, then "create an entity from a description" needs no per-product code. That
covers a discount with a rule tree, a page with nested blocks, or a workflow whose steps form a graph.

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
| 2. Entity definitions | `Entities/`: fields, slots of typed items, entity and item references, an item type registry, a typed entity document (`EntityDocument`, `EntityElement`, `EntityItem`), a path editor generalised from Umbraco.AI's block path engine, and the generic entity abilities. `Entities/Models/`: the typed layer package developers write against (`EntityDefinitionBase<TModel>`, model attributes, `ModelEditAbilityBase`) |
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

There are two ways to describe a contract. They mean the same thing, and fluent configuration wins where both are
used.

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
    [Slot(RuleGroup, Min = 1)] public List<DiscountRule> Rules { get; set; } = [];
}

[ItemType("percentageOff", "Percentage off")]
public sealed class PercentageOffReward : DiscountReward
{
    [Required, Range(1, 100)] public decimal? Percent { get; set; }
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
    .Slot(x => x.Crops, CropGroup));

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
| The structure: fields, types, required, allowed values, references, slots and their limits | Property types, plus attributes or fluent configuration |
| JSON Schema for structured values (a focal point) | `JsonSchemaExporter` over the property type |
| Culture variation | `Varying<T>` (a value per culture), on fields and slots |
| Block settings and grid placement shapes | `[ItemSettings]`/`HasSettings`, `[ItemLayout]`/`HasLayout`, a slot's layout type |
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
| Editing contract and its five item classes | 27 |
| Mapping for the discount: load, save, dates, keys | about 35 |
| Domain rules (dates in order, unique code) and item registration | about 18 |
| Provider settings helper: item class ↔ alias plus string settings. Written once for every Commerce rule and reward type, third parties' included | 22 |

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
| 3 | Keep a base version (and base values) per entity from its first change | To show what others changed since, and find conflicts |
| 4 | Report conflicts in a neutral form: entity, property alias, culture, segment, base, draft and live values | So a coordinator, the backoffice or an agent can show and resolve them |
| 5 | Let the caller choose keys for new content | So other changes in the set can refer to content before it exists |
| 6 | Make reads draft-aware: get by key, and find, through a draft set | So whoever builds the set sees their own changes |
| 7 | Validate a whole set, per entity, only in the cultures each document has | Before anyone approves it; see finding 1 |
| 8 | Apply all or nothing in one scope, record that it applied, and refuse to apply twice | A coordinator applies products one after another and resumes after a failure |
| 9 | Raise notifications: applying, applied, discarded | Audit, cache and other packages hook in |
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
| A third party's item type is creatable with no change to Commerce or the AI adapter | `A_third_party_rule_type_is_creatable_with_no_change_to_commerce` |
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
| The structure (types, required, allowed values, references, slots, variation, settings, placement, JSON Schema) comes from model classes and attributes | `TypedModelTests.The_structure_comes_from_the_model_class_and_its_attributes`, `Varying_values_settings_layout_and_json_values_are_described_too` |
| A model round-trips through its working copy, keeping item keys and giving each item its path | `A_model_round_trips_through_its_document_with_item_keys_and_paths` |
| Each problem is reported once, whether it comes from the structure, an attribute or a domain rule | `Each_problem_is_reported_once_whether_it_comes_from_the_structure_or_an_attribute` |
| Content changes in a change set are a CMS draft set: the backoffice can read it, it records only what changed, and discarding the set discards it | `CmsDocumentDefinitionTests.Content_changes_in_a_change_set_are_a_CMS_draft_set_that_records_only_what_changed` |
| The CMS draft set works on its own, with no abilities: stage, read through, conflict, resolve, validate, apply | `A_draft_set_works_on_its_own_with_no_abilities` |
| Entity types are UDI entity types, package types are registered, and references can be given as UDIs, stored in the field's own form, with the wrong type refused | `UdiTests` |
| Workflow step types are derived from abilities | `WorkflowTests.Step_types_are_derived_from_abilities` |
| A workflow graph (edges as item references) is validated, and can refer to a discount created in the same set | `A_workflow_from_text_links_its_steps_and_can_refer_to_a_discount_in_the_same_set` |
| Names and values vary by culture; invariant values are shared across cultures | `CmsContentTests.Names_and_values_vary_by_culture_while_invariant_values_are_shared` |
| A value that varies by culture needs a known culture; an entity with no culture can't be applied | `A_value_that_varies_by_culture_needs_a_known_culture` |
| Each culture is validated separately, and problems say which culture | `Each_culture_is_validated_separately_and_problems_say_which` |
| Blocks in an invariant Block List hold per-culture values, and a block's settings element is edited and validated at `{item}/@settings` | `Block_values_vary_per_culture_inside_an_invariant_block_list_and_settings_are_their_own_element` |
| Block Grid: column span is a placement value at `{item}/@layout`, areas are slots with limits, and a type can be allowed at the root and in areas | `Grid_blocks_have_placement_values_and_areas_that_are_slots` |
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
| The schema comes from the document type: cultures, varying fields, block types per slot, settings, grid areas and placement | `The_schema_comes_from_the_document_type_and_its_block_editors` |
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
| `Variations` (typed `Variation`, named after the CMS's `Variations` / `ContentVariation`) on the entity, fields and slots, with `VariantId` narrowed per value | CMS property values carry a culture or segment only if their property varies that way, and Umbraco rejects values that don't match |
| Variant values stored per variant (`variants["en-US"].fields`), names as a culture-variant field | CMS names vary by culture; block values inside an invariant block property vary per culture |
| Culture-variant slots (a list per culture) | A culture-variant block property holds one whole envelope per culture |
| `culture` / `segment` on `Problem` | The CMS's `PropertyValidationError` reports alias, culture and segment |
| `ItemTypeDefinition.SettingsShape`, edited at `{item}/@settings` | Blocks have a settings element type next to their content type |
| `SlotDefinition.ItemLayout`, edited at `{item}/@layout` | A Block Grid's column and row span belong to the block's place, not its values |
| `ItemTypeDefinition.AlsoIn` | Grid blocks can be allowed at the root and in areas |
| `Many`, `ReferencesAnyOf`, `FieldType.Json` | Tags and dropdowns are string arrays; a tree picker points at documents or media; focal points, crops and date-times are objects |

What still does not fit, and needs a decision rather than more code:

| Product | Gap | Consequence |
| --- | --- | --- |
| CMS | Blocks in rich text are placed by markup, not by slot order | Adding a block to rich text needs an anchor in the markup; editing existing ones fits |
| CMS | Segments are not managed (no list to validate against); "expose" (which cultures a block exists in) is derived by the CMS on read | The CMS definition must produce expose entries and accept any segment |
| CMS | Update replaces all properties, and saves invalid values while returning errors | The definition must send the full working copy and treat returned errors as a failed apply |
| CMS | No concurrency token on update | Change sets rebase instead (see "Rebasing"); a change saved between validating and saving is still not caught |
| Commerce | A discount's rules are one recursive tree under a group rule (AND/OR is a setting); rule and reward nodes have no ids | Fits as a single-item slot with nested slots, but the definition must assign item keys on load and drop them on save |
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
