# Abilities, entities and draft sets: proof of concept

> **Status:** Experiment. Not a product, not shipped, not wired into the monorepo build. It exists to
> test the design in the "Abilities, entities and draft sets" doc before anything is built for real.

It tests one claim: if packages declare **abilities** and **entity definitions**, and Write calls are
recorded into **draft sets**, then "create an entity from a description" needs no per-product code. That
covers a discount with a rule tree, a page with nested blocks, or a workflow whose steps form a graph.

## Run it

```bash
cd experiments/abilities-poc
dotnet test
```

The folder has its own `Directory.Build.props`, `Directory.Packages.props` and `NuGet.config`, so the
monorepo's central packages and versioning don't apply. Only nuget.org is needed.

## What is here

| Project | Plays the part of | Depends on |
| --- | --- | --- |
| `Umbraco.Abilities` | The contract and runtime: abilities, entity definitions, the path editor, draft sets, the invoker | Nothing |
| `Umbraco.Abilities.AI` | Umbraco.AI's adapter: every ability becomes an M.E.AI `AIFunction` | Contract, `Microsoft.Extensions.AI.Abstractions` |
| `Umbraco.Abilities.Samples` | Product packages: CMS pages with nested blocks, Commerce collections and discounts, a third-party rule type, an email package, Automate workflows | Contract only |
| `Umbraco.Abilities.Tests` | A scripted model calling the AI tools with JSON arguments | All of the above |

Each design layer and where it lives:

| Layer | Code |
| --- | --- |
| 1. Abilities | `Abilities/Ability.cs`: `[Ability]` with `Kind` and `Reversible`, typed `AbilityBase`, failures with categories and paths, `EntityRef` |
| 2. Entity definitions | `Entities/`: fields, slots of typed items, entity and item references, an item type registry, a path editor generalised from Umbraco.AI's block path engine, and the generic entity abilities |
| 3. Draft sets | `DraftSets/`: a recorded unit of "any Write ability call", working state worked out in memory, all-or-nothing apply, live-change detection |

## What the tests show

| Claim | Test |
| --- | --- |
| A discount is built from text step by step, nothing is live until apply, and the result is stored | `CreationPromptTests.Discount_from_text_is_built_in_a_draft_set_and_only_goes_live_when_applied` |
| Problem paths are the same paths the edit abilities take, so a model can fix its own work | `Validation_reports_problems_by_path_and_the_agent_fixes_them_with_the_same_paths` |
| Bad edits are correctable Input failures with a path; edits never run outside a set | `Bad_edits_are_correctable_input_failures_with_paths` |
| A third party's item type is creatable with no change to Commerce or the AI adapter | `A_third_party_rule_type_is_creatable_with_no_change_to_commerce` |
| Every ability becomes an AI tool; only irreversible Writes are wrapped for approval | `Every_ability_is_an_AI_tool_and_only_irreversible_writes_need_approval` |
| One set holds a page with nested blocks, a discount referring to that new page, and a recorded operation, all applied together | `DraftSetTests.A_campaign_spanning_content_commerce_and_operations_is_applied_together` |
| A failure during apply leaves nothing applied | `If_anything_fails_while_applying_nothing_is_applied` |
| Apply is refused if live data changed under the set | `Applying_is_refused_when_live_data_changed_underneath_the_set` |
| Reads inside a set show its edits; reads outside show live data | `Reads_inside_a_set_show_its_edits_and_reads_outside_show_live_data` |
| Replay is deterministic, and keys handed to the model are the stored keys | `Replaying_a_set_gives_the_same_entity_every_time` |
| Workflow step types are derived from abilities | `WorkflowTests.Step_types_are_derived_from_abilities` |
| A workflow graph (edges as item references) is validated, and can refer to a discount created in the same set | `A_workflow_from_text_links_its_steps_and_can_refer_to_a_discount_in_the_same_set` |

## What building it taught us

These feed back into the design doc.

1. **Approval depends on the call, not just the tool.** `email.send` and `commerce.activateDiscount` need
   approval when run directly. Inside a draft set they are only recorded, so asking then is pointless; the
   approval belongs on `draftSet.apply`. M.E.AI's `ApprovalRequiredAIFunction` wraps a tool once, so the
   adapter can't express "only outside a set". The contract needs approval decided per call, from the
   context.
2. **Recorded operations are not checked when recorded.** An `email.send` with a bad recipient is
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

## What it does not prove

- **Real products.** The CMS page is a stand-in. Real documents bring cultures and segments, which the
  structure language does not cover yet, plus real Block List / Block Grid values and
  `IContentEditingService`. Commerce and Automate models are guesses from the domain, not their code.
- **A real model.** The tests script the tool calls. No LLM chose them.
- **Persistence and scale.** Sets are in memory, and working state is recomputed by replaying every call
  on each read.
- **Access checks.** The `Access` failure category exists but nothing enforces permissions.
- **Opening a set in an editor.** Nothing loads a set's working state into a backoffice workspace.
- **Transactions across separate databases.** All samples share one in-memory database, standing in for
  Umbraco's shared scope.
