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
| `Umbraco.Abilities.Cms` | The real entity definition for CMS documents, on `IContentEditingService` | Contract, Umbraco 18 |
| `Umbraco.Abilities.Tests` | A scripted model calling the AI tools with JSON arguments | Contract, AI adapter, samples |
| `Umbraco.Abilities.Cms.Tests` | The same, against a real Umbraco on SQLite | Contract, AI adapter, CMS definition, Umbraco test host |

Each design layer and where it lives:

| Layer | Code |
| --- | --- |
| 1. Abilities | `Abilities/Ability.cs`: `[Ability]` with `Kind` and `Reversible`, typed `AbilityBase`, failures with categories and paths, `EntityRef` |
| 2. Entity definitions | `Entities/`: fields, slots of typed items, entity and item references, an item type registry, a path editor generalised from Umbraco.AI's block path engine, and the generic entity abilities |
| 3. Change sets | `ChangeSets/`: a recorded unit of "any Write ability call", working state worked out in memory, apply product by product (each all or nothing) and resumable, live-change detection. Generalises the CMS Draft Sets PoC (`Umbraco.Cms.DraftSets`) |

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
| Apply is refused if live data changed under the set | `Applying_is_refused_when_live_data_changed_underneath_the_set` |
| Reads inside a set show its edits; reads outside show live data | `Reads_inside_a_set_show_its_edits_and_reads_outside_show_live_data` |
| Replay is deterministic, and keys handed to the model are the stored keys | `Replaying_a_set_gives_the_same_entity_every_time` |
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
5. **Problem paths that equal edit paths matter.** Because `rewards/{key}/percent` is both what
   validation reports and what `entity.setValue` takes (`elementPath` + `field`), the fix loop needs no
   translation.
6. **Abilities as item types works.** Reflecting an ability's input type into fields made Automate's step
   types come for free, including an entity reference from `[EntityKey]`.

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

## Against a real Umbraco

`CmsDocumentDefinition` is a real definition for documents. One entity type, `cms.document`, has a subtype per
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
| Apply is refused when the page was saved in Umbraco after being edited in the set | `Applying_is_refused_when_the_page_changed_in_Umbraco_after_it_was_edited_in_the_set` |

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
5. **`UpdateDate` works as a change check.** It is compared before saving, as Umbraco has no check of its own.

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
| `Variance` on the entity, fields and slots, with `VariantId` narrowed per value | CMS property values carry a culture or segment only if their property varies that way, and Umbraco rejects values that don't match |
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
| CMS | No concurrency token on update | Change detection has to compare `UpdateDate` or `VersionId` inside the apply scope |
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
