# Reviewing the proof of concept

> **Status:** Experiment. This guide is for reviewing the design through its code: what to read, in what order, and
> what to question. The design itself is in the "Abilities, entities and change sets" doc. [README.md](README.md) has
> the findings.

## What you are reviewing

It tests one claim. If packages declare **abilities** (operations) and **entity definitions** (what their entities look
like), and write calls are recorded into **change sets**, then "create or edit an entity from a description" needs no
per-product code. The same pieces then serve the copilot, Automate and MCP.

The code is about 6,600 lines of source and 2,100 lines of tests in seven projects. The source splits into the framework
(about 4,100), the in-memory samples (about 1,000), the real CMS definition (about 1,000) and the draft sets stand-in
(about 500). A full review takes two to three hours.
A first pass over the story and the contracts (passes 1 to 3 below) takes about 45 minutes.

```bash
cd experiments/abilities-poc
dotnet test   # 43 in-memory tests (seconds) and 14 against a real Umbraco 18 on SQLite (about 40 seconds)
```

## The map

Five source projects and two test projects. The arrows show what references what.

```
Umbraco.Abilities            the framework: abilities, entity definitions, change sets
   ↑            ↑                          (references Umbraco.Cms.Core for UDIs, notifications, the background queue)
   │            │
Umbraco.Abilities.AI         turns every ability into an M.E.AI AIFunction: the copilot's tools
   │            │
Umbraco.Abilities.Samples    in-memory products: CMS pages and media, Commerce, an Acme rule type, email, Automate
   │
Umbraco.Abilities.Cms  ──→   Umbraco.Cms.DraftSets
   the real CMS document       a stand-in for a Draft Sets feature in CMS core:
   definition, and the         references Umbraco only, never Abilities
   draft set adapter
```

The tests: `Umbraco.Abilities.Tests` (xunit) runs the samples through the AI tools. `Umbraco.Abilities.Cms.Tests` (NUnit)
does the same against a real Umbraco.

### How one call flows

A model calls a tool. Here is everything it passes through, and where each piece lives:

```
model calls entity_setValue (JSON arguments)
  → AbilityAIFunction                     Umbraco.Abilities.AI: JSON in, result JSON out
  → AbilityInvoker.InvokeAsync            ChangeSets/ChangeSetService.cs (end of file)
      no change set, or not a Write  → ability.InvokeAsync: it runs now
      a Write inside a change set    → ChangeSetService.RecordAsync: recorded, not run
  → RecordAsync
      side effect (email.send)       → kept in ChangeSet.AfterApply, runs after apply
      entity edit (IEntityEditAbility):
        working state                ← GetWorkingStateAsync (live + the set's edits, or the product's draft)
        edit.Apply(document, input)  → EntityEditor: a path edit on the EntityDocument
        authorize                    → the definition asks the product: may this user make this change?
        keep it                      → ChangeSet.Changes, or a product's own draft (IDraftingEntityDefinition)
  → result JSON back to the model: { success, status, result } or { status, correctable, message, problems }

later, model calls changeSet_apply
  → ChangeSetService.ApplyAsync
      authorize every change for the user applying  → AccessDenied, nothing applied
      validate (structure, then each product's rules, then conflicts with changes saved elsewhere)
      ChangeSetApplyingNotification (cancelable)
      one step per product, each in its own transaction: save each entity, or apply the product's draft
      side effects: inline, or queued on the background queue with retries
      ChangeSetAppliedNotification
```

## Reading order

Each pass builds on the one before it. The **Look for** notes are what a reviewer should push on.

### Pass 1: the story, through the tests (15 minutes)

Start with what the system does, before how it does it.

1. `tests/Umbraco.Abilities.Tests/ScriptedAgent.cs` is the harness. It plays a model: it calls the AI tools by name, with
   JSON, as a user. Every test reads like a conversation.
2. `CreationPromptTests.Discount_from_text_is_built_in_a_draft_set_and_only_goes_live_when_applied` covers "10% off the
   Summer collection for VIP customers in December". The model discovers the structure, finds the collection by name,
   builds the discount step by step and validates it. Nothing is live until it applies.
3. `CreationPromptTests.Validation_reports_problems_by_path_and_the_agent_fixes_them_with_the_same_paths` shows that
   problem paths are edit paths, so a model can fix its own work.
4. `ChangeSetTests.A_campaign_spanning_content_commerce_and_a_side_effect_is_applied_together` is one set holding a
   page, a discount that refers to that page, and an email to send afterwards.

**Look for:** whether the tool calls are ones a real model could plausibly make. They're scripted here. Nothing proves an
LLM chooses them (see "What it does not prove" in the README).

### Pass 2: layer 1, abilities (15 minutes)

| File | What it is |
| --- | --- |
| `src/Umbraco.Abilities/Abilities/Ability.cs` | The contract. `[Ability]` declares an operation: id, `Kind` (Explain, Read, Transform, Trigger, Write), `Reversible`, `Retries`. `AbilityBase<TInput, TOutput>` gives typed input from JSON. `AbilityContext` holds the acting user's key, the caller and the active change set. `AbilityResult` mirrors the CMS's `Attempt<TResult, TStatus>`, and `Problem` mirrors `PropertyValidationError` |
| `src/Umbraco.Abilities.AI/AbilityAIFunction.cs` | The whole AI adapter: an ability becomes an `AIFunction`, with a schema from its input type. Only irreversible Writes need approval |
| `AbilityInvoker` (end of `ChangeSets/ChangeSetService.cs`) | The single entry point. A Write with an active change set is recorded instead of run |

**Look for:** whether `Kind` plus `Reversible` is enough to decide approval. Finding 1 in the README says approval
depends on the call, not just the tool. Also check whether the result shape suits Automate as well as AI.

### Pass 3: layer 2, entity definitions (45 minutes)

This is the core idea: a product-neutral way to describe and edit any entity. Read in this order.

| File | What it is |
| --- | --- |
| `Entities/Structure.cs` | The structure language. `ElementShape` holds fields and slots. A **field** has a type, required, allowed values, references and `ContentVariation`. A **slot** holds typed **items** (rules, blocks, workflow steps), with min and max, optional per-item settings, and placement values (`ItemLayout`). `ItemTypeRegistry` maps a group to item types, so third parties add types (`Acme`) |
| `Entities/IEntityDefinition.cs` | What a product implements: describe, new, load, validate, save, find, and optional subtypes (document types). `IVersionedEntityDefinition` is for products with version history |
| `Entities/EntityDocument.cs` | The working copy: a typed tree (`EntityDocument` → `EntityElement` → `EntityItem`) of fields, slots, and values per culture and segment. Its JSON form is what tools return |
| `Entities/EntityEditor.cs` | Path editing, generalised from Umbraco.AI's block path engine: `slot/{key}/slot`, `{item}/@settings`, `{item}/@layout`. `FieldChecker` checks and normalizes values. A UDI given for a reference is turned into the field's own form |
| `Entities/StructureValidator.cs` | Generic validation from the structure: required (per culture), allowed values, references that exist (including in the same set), slot limits. It also renders the JSON Schema the model sees |
| `Entities/EntityAbilities.cs` | The generic tools every entity gets: `entity.getSchema`, `find`, `get`, `create`, `setValue`, `addItem`, `removeItem`, `moveItem` |
| `Entities/EntityUdis.cs` | Identity is a UDI. Entity types are UDI entity types, registered with `UdiParser` |
| `Entities/EntityAuthorization.cs` | `IAuthorizingEntityDefinition`: the product answers "may this user read, create or update this entity, in these cultures?" |
| `Entities/AbilityItemTypes.cs` | Abilities become item types: how Automate's step types come for free |

Then the typed layer, which is what a package developer actually writes against:

| File | What it is |
| --- | --- |
| `Entities/Models/ModelAttributes.cs` | `[Entity]`, `[ItemType]`, `[Slot]`, `[EntityName]`, `[EntityVersion]`, `[ItemSettings]`, `[ItemLayout]`, `Varying<T>`, `EntityItemModel` |
| `Entities/Models/ModelConfiguration.cs` | The same, fluently, for classes a package can't annotate, such as Management API request models |
| `Entities/Models/ModelShapes.cs` | Works out the structure from a class, maps the class to and from an `EntityDocument`, and runs DataAnnotations on entity paths |
| `Entities/Models/EntityDefinitionBase.cs` | `EntityDefinitionBase<TModel>`: a package implements load and save of its model, and the framework does the rest. `ModelEditAbilityBase` turns `Edit(model, input)` into an ability that works directly and inside a change set |

**Look for:**
- whether the structure language can carry your product's entities;
- whether paths are a good contract for a model;
- whether the typed layer removes enough work;
- how much the editing contract plus its mapping costs a real package. The README's "What the mapping costs" has the numbers.

### Pass 4: the samples, as package code (20 minutes)

These are what a package would write. Judge them as such: how short, how obvious, how much is boilerplate.

| File | What it shows |
| --- | --- |
| `Samples/Commerce/Domain.cs` then `Commerce.cs` | A domain shaped like Commerce's: immutable, `AsWritable()`, UTC dates, rules as provider alias plus string settings, no rule ids. Then the attributed editing contract (`DiscountEditModel`) and the definition that maps to the domain. Also `ActivateDiscountAbility`, a product ability that edits an entity |
| `Samples/Acme/AcmeWeekdayRule.cs` | A third party adds a rule type with one class and one registration |
| `Samples/Cms/CmsPage.cs` | CMS-shaped content: culture variation, blocks with settings, a grid with placement and areas, per-culture block lists, pickers. Media is described **fluently** over plain request models |
| `Samples/Automate/Workflow.cs` | A document-level definition, the escape hatch for structure that is data, not code. Step types are abilities |
| `Samples/Email/SendEmail.cs` | A side effect with retries |
| `Samples/SampleSite.cs` | How it's wired, as composers would wire it. Each block is one package |
| `Samples/InMemoryDatabase.cs`, `SampleAccess.cs` | Stand-ins for each product's storage, transaction and permission model |

**Look for:** the derived keys in `DiscountDefinition`, which exist because Commerce rules have no ids. That's a known
limit to raise with Commerce.

### Pass 5: layer 3, change sets (40 minutes)

| File | What it is |
| --- | --- |
| `ChangeSets/ChangeSet.cs` | The data. `Changes` are recorded entity edits. `AfterApply` holds side effects. `AppliedSteps` lets apply resume. `Bases` and `BaseVersions` record where rebasing starts from, and `Resolutions` records how conflicts were resolved. `Drafts` holds product drafts. Also the seams `IUnitOfWorkProvider`, `IEntityEditAbility` and `IDraftingEntityDefinition` |
| `ChangeSets/ChangeSetService.cs` | The coordinator. Read its public methods top to bottom (see below) |
| `Entities/EntityDiff.cs` | Compares two versions of any entity document, by path. This is what makes rebasing generic |
| `ChangeSets/ChangeSetAbilities.cs` | The `changeSet.*` tools |
| `ChangeSets/ChangeSetNotifications.cs` | CMS notifications: a cancelable applying, then applied, discarded, and side effect completed |

In `ChangeSetService.cs`, read the public methods in this order:

1. `RecordAsync`: how an edit becomes a recorded change (or a staged draft change), including the authorization check.
2. `GetWorkingStateAsync`: an entity as the set would leave it. The set's edits are replayed onto the **current** live
   version, which is what rebasing means here.
3. `GetRebasedAsync` and `ResolveConflictAsync`: what others changed since, where both sides changed the same value, and
   keeping yours or theirs.
4. `ValidateAsync`: structure, product rules and unresolved conflicts, per entity.
5. `ApplyAsync`: authorize for the applying user, validate, the cancelable notification, then one step per product (each
   all or nothing, resumable), then side effects (inline, or queued with Polly retries).

**Look for:**
- **Per-product, not cross-product, atomicity.** Products use their own connection strings, so a set applies one product
  at a time. A failure leaves earlier products applied and resumable. Check whether that's acceptable for your product.
- **Replay determinism.** Keys are chosen when a change is recorded, so they don't change on replay.
- **Conflicts.** Check how they're detected and resolved.
- **Ordering inside `ApplyAsync`:** authorization comes before validation.

### Pass 6: against a real Umbraco (40 minutes)

| File | What it is |
| --- | --- |
| `Umbraco.Abilities.Cms/CmsStructureMapper.cs` | Builds the structure from document types and data type configuration: Block List, Block Grid (areas become slots, column span becomes placement), pickers, variation |
| `Umbraco.Abilities.Cms/CmsDocumentDefinition.cs` | The real definition. Reads go through Umbraco's own block value classes in editor format. Validation uses Umbraco's, with its JSON paths translated back to entity paths (`ErrorIndex`). Saves go through `IContentEditingService`. It is also the **draft set adapter** (stage, read, rebase, resolve, apply), and **authorization** goes through `IContentPermissionService` |
| `Umbraco.Cms.DraftSets/DraftSetService.cs` | The stand-in for CMS core's Draft Sets. It records per-property changes over live content and reads through them, reports what others changed and the conflicts, and applies in one scope. It writes audit entries and raises notifications through the scope. It references Umbraco only |
| `tests/Umbraco.Abilities.Cms.Tests/CmsDocumentDefinitionTests.cs` | Everything above against Umbraco 18 on SQLite: blocks, settings, grids and cultures saved by Umbraco; validation paths; rollback; resume; rebasing; draft sets; vetoes; real user permissions; the background queue |

**Look for:**
- **The adapter's two jobs.** Reading `CmsDocumentDefinition` alongside `DraftSetService`, ask whether the line between
  them is where core's Draft Sets would draw it.
- **The core checklist.** "What core's Draft Sets must address" in the README is the ask of the CMS team.
- **Validation in some cultures only.** Look at how validation is limited to the cultures a document has.

## How it all connects

The lifecycle of one change set, end to end:

| Stage | What happens | Where |
| --- | --- | --- |
| Discover | The model reads the structure, in two steps, and finds entities by name | `GetEntitySchemaAbility`, `FindEntityAbility`, `StructureValidator` (schema) |
| Start | `changeSet.create` | `ChangeSetService.Create` |
| Edit | Each Write is recorded. The working state is live plus the edits (or the product's draft). Each edit is authorized for the user making it | `AbilityInvoker`, `RecordAsync`, `EntityEditor`, `IAuthorizingEntityDefinition` |
| Read inside the set | Reads show the set's changes; reads outside show live data | `GetWorkingStateAsync`, `GetEntityAbility` |
| Check | Structure, product rules and conflicts with changes saved elsewhere | `ValidateAsync`, `StructureValidator`, `GetRebasedAsync`, `EntityDiff` |
| Resolve | Keep yours or theirs for each conflict | `ResolveConflictAsync` |
| Apply | Authorize for the applier, validate, cancelable notification, one transaction per product, side effects, applied notification | `ApplyAsync`, `IUnitOfWorkProvider`, `IDraftingEntityDefinition`, `IBackgroundTaskQueue` |
| After | A side effect with retries reports through a notification | `ChangeSetSideEffectCompletedNotification` |

The interfaces a product implements are the extension points. Everything else is the framework's:

| Interface | Implement it to |
| --- | --- |
| `IAbility` (via `AbilityBase`) | Offer an operation |
| `IEntityDefinition`, or `EntityDefinitionBase<TModel>` | Make an entity type editable from text |
| `IEntityEditAbility` (via `ModelEditAbilityBase`) | Make a product ability count as a change inside a set |
| `IUnitOfWorkProvider` | Give your product's step its own transaction |
| `IVersionedEntityDefinition` | Rebase from your version history instead of a snapshot |
| `IDraftingEntityDefinition` | Keep your own drafts (CMS Draft Sets) instead of the set replaying edits |
| `IAuthorizingEntityDefinition` | Apply your own permission model |

## What to question most

These are the judgments the PoC makes that a reviewer is best placed to challenge:

1. **The structure language.** Fields, slots, item types, settings and placement cover discounts, workflows and CMS
   blocks here. Will they cover your entity?
2. **Paths as the contract with a model.** They make the fix loop simple (a problem path is an edit path). Are they
   natural for a model to produce?
3. **The editing contract and its mapping.** About 35 lines per entity for Commerce, with a shared helper. Is that cheap
   enough for package developers?
4. **Atomicity per product.** Is "each product all or nothing, and resumable" acceptable, given products can't share a
   transaction?
5. **Who owns the coordinator.** Abilities owns it here. The README's "If core owned the change set" covers the
   alternative, and what core's Draft Sets must do to make it possible.
6. **What it doesn't prove.** No LLM is in the loop. Change sets and draft sets aren't stored. Retried side effects aren't
   durable. See "What it does not prove" in the README.

## Where the decisions are written down

- [README.md](README.md): findings, numbers, and the sections referred to above.
- The "Abilities, entities and change sets" design doc: the design, with the same findings folded in.
- Commit messages on this branch: each records what changed and why, in order.
