# Architecture

> **Status:** Archived 09-10-2026. Shipped: merged to `v18/dev` (#419) and `v17/dev` (#428) on 09-10-2026, together with the follow-on `decision-evaluators` plan (#430, #431). Only T26 (the public Umbraco.Docs PR, branch `ai/decision-docs`) was still open at archive time.

Design for `BRIEF.md`. Builds on the spike's Core plumbing (branch
`v18/feature/decision-capability`, see the archived `decision-capability/` plan folder for
how it was built and why). Everything below was checked against the real code on
`v18/dev`/`v17/dev` and the spike branch on 24-09-2026, not from memory.

## Extension points

| Layer | Product | Extension point | Precedent to copy |
|-------|---------|-----------------|-------------------|
| Capability | `Umbraco.AI.Core` | `AICapability.Decision = 8` + `IAIDecisionClient` pipeline (already on spike branch) | `SpeechToText/` |
| Provider | new `Umbraco.AI.TypeSafe` | `[AIProvider]` class, auto-discovered via `IDiscoverable` | `Umbraco.AI.FireworksAI` (HTTP, no SDK) |
| Management API | `Umbraco.AI.Web` | Versioned management controllers under `Api/Management/Decision/` | `ImageGeneration/` (`GenerateImageController`) |
| TS client | `@umbraco-ai/core` | Public `UaiDecisionController` → repository → server data source | `image-generation/`, `chat/` |
| Backoffice | `@umbraco-ai/core` | Profile-settings switch case, Settings editor picker, lang keys | `ImageGeneration` equivalents |
| Deploy | `Umbraco.AI.Deploy` | `AISettingsArtifact` + `UmbracoAISettingsServiceConnector` default-profile slot | ImageGeneration fix (#227/#228) |
| Automate | `Umbraco.AI.Automate` | `[Action]`-attributed `ActionBase<TSettings, TOutput>` classes | `TranscribeAudioAction` |
| Auto mode | `Umbraco.AI.Agent` | `AIAgentService.SelectAgentForPromptAsync` | itself (existing chat path becomes the fallback) |

No new CMS extension points. Every layer extends an Umbraco AI pattern that already exists
for another capability.

## Data model & persistence

**No migrations.**

- `AISettings.DefaultDecisionProfileId` (`Guid?`) is a new property on an existing type.
  `AISettingsFactory` persists settings reflectively (`GetProperties`), so it's stored with no
  schema change. Config fallback: `AIOptions.DefaultDecisionProfileAlias`.
- Decision profiles and TypeSafe connections are ordinary `AIProfile`/`AIConnection` rows.
- `AIDecisionProfileSettings` is a new, empty, sealed profile-settings type. Jev has no
  per-profile knobs today. It exists so `AIProfileSettingsSerializer`, the Web polymorphic
  model, and Deploy import all have a real Decision case instead of silently returning null
  (the exact gap ImageGeneration hit). Adding a field later is additive.

## Core types

One request carries the content being judged (`State`) and one or more typed questions about
it. The response carries one typed answer per question, keyed by question id, plus the model
and usage for the whole call. This follows the shape Microsoft is converging on for M.E.AI
(dotnet/extensions#7764 and its Layer 1 PR #7795: shared state, heterogeneous id'd questions,
keyed answers, `TrueProbability`, optional `Confidence`, ordinal score levels). Our provider
contract (`IAIDecisionClient`) is shaped like their `IDecisionClient`, so when M.E.AI ships it
our provider layer can wrap theirs and the public service stays put. Where the issue and the PR
disagree (choice-option naming, `JsonElement` state, feature vectors), we keep ours. See
DECISION-LOG 01-10-2026 and 02-10-2026.

```csharp
// Umbraco.AI.Core/Decision/ — all [Experimental(AIDecisionDiagnostics.DiagnosticId)]

// ---- Request ----
public sealed class AIDecisionRequest
{
    public string? State { get; init; }                                // the content being judged, shared by every question
    public required IReadOnlyList<AIDecisionQuestion> Questions { get; init; } // 1..n, unique ids
}

public abstract class AIDecisionQuestion
{
    public string? Id { get; init; }                // correlation id; required + unique in every request, optional for AskAsync (assigned)
    public required string Instructions { get; init; }  // what to decide
}
public abstract class AIDecisionQuestion<TAnswer> : AIDecisionQuestion
    where TAnswer : AIDecisionAnswer;

public sealed class AIBinaryDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionAnswer>
{
    public string? TrueCriteria { get; init; }   // what "yes" means (optional)
    public string? FalseCriteria { get; init; }  // what "no" means (optional)
}
public sealed class AIChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionAnswer>
{
    public required IReadOnlyList<AIDecisionOption> Options { get; init; } // 2..255, unique keys
}
public sealed record AIDecisionOption(string Key, string? Description = null); // Key is opaque, returned exactly
public sealed class AIScoreDecisionQuestion : AIDecisionQuestion<AIScoreDecisionAnswer>
{
    public required IReadOnlyList<AIDecisionScoreLevel> Levels { get; init; } // 2..10, lowest first; list position = level index
}
public sealed record AIDecisionScoreLevel(string Description);

// ---- Response ----
public class AIDecisionResponse
{
    public required IReadOnlyDictionary<string, AIDecisionAnswer> Answers { get; init; } // keyed by question Id
    public string? ModelId { get; init; }        // the concrete build that answered, e.g. "jev-1.13.0"
    public UsageDetails? Usage { get; init; }    // whole call
    public object? RawRepresentation { get; init; }
}
public sealed class AIDecisionResponse<TAnswer> : AIDecisionResponse
    where TAnswer : AIDecisionAnswer
{
    public required TAnswer Answer { get; init; } // the single answer, typed (Answers still holds it too)
}

public abstract class AIDecisionAnswer
{
    public object? RawRepresentation { get; init; }
}
public sealed class AIBinaryDecisionAnswer : AIDecisionAnswer
{
    public required double TrueProbability { get; init; }  // P(true), 0..1. No Confidence: the probability is the distribution.
    public bool IsTrue(double threshold = 0.5) => TrueProbability >= threshold; // cut-off is the caller's choice
}
public sealed class AIChoiceDecisionAnswer : AIDecisionAnswer
{
    public required string Choice { get; init; }                          // one of the option Keys, exactly
    public required IReadOnlyDictionary<string, double> Probabilities { get; init; } // every option Key
    public double? Confidence { get; init; }                              // provider summary; not comparable across providers
}
public sealed class AIScoreDecisionAnswer : AIDecisionAnswer
{
    public required double Score { get; init; }                           // 0..N-1, fractional (expected level index)
    public required IReadOnlyDictionary<int, double> Probabilities { get; init; } // every level index 0..N-1
    public double? Confidence { get; init; }
}

// ---- Provider contract (one per configured capability) ----
public interface IAIDecisionClient : IDisposable
{
    Task<AIDecisionResponse> GetResponseAsync(AIDecisionRequest request, AIDecisionOptions? options = null, CancellationToken cancellationToken = default);
    object? GetService(Type serviceType, object? serviceKey = null);
}

// ---- Public service ----
public interface IAIDecisionService
{
    // One question. state = the content being judged.
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(AIDecisionQuestion<TAnswer> question, string? state = null, AIDecisionOptions? options = null, CancellationToken ct = default);
    // + (Guid profileId, question, state?, options?, ct), (string profileAlias, ...), (Action<AIDecisionBuilder> configure, question, state?, ct)

    // Several questions about one state, one model call.
    Task<AIDecisionResponse> GetDecisionResponseAsync(AIDecisionRequest request, AIDecisionOptions? options = null, CancellationToken ct = default);
    // + the same Guid / alias / builder overloads
}
```

- **`AskAsync` is a thin helper over the batch path.** It builds a one-question request
  (assigning an internal id when `Id` is null), calls the same pipeline, and wraps the single
  answer in `AIDecisionResponse<TAnswer>`. The service checks the answer is a `TAnswer` and
  throws `AIProviderException` if not (a provider bug, caught inside tracking, see below).
- **The provider contract is batch-only and non-generic.** Middleware, tracking, telemetry and
  the error classifier don't care about kinds or counts, so they stay one class each. The
  method is named `GetResponseAsync`, the same as M.E.AI's, so the later swap is mechanical.
- **`State` is plain text**, not M.E.AI's `JsonElement`. Every caller we have (Automate
  bindings, Copilot prompt, content) is already text, and text maps onto a JSON string value
  trivially. A typed-state overload can be added later.
- **No reference slot yet.** Feedback on #7764 shows a separate "what to judge against" slot
  helps accuracy, but neither the issue nor PR #7795 has one. Naming the content `State` (not
  `Context`) leaves room to add it without a confusing rename.
- **No batch size cap in Core.** Limits are provider-specific (per #7764). The TypeSafe
  client enforces Jev's limit, if it documents one.
- `AIDecisionKind` stays deleted. The type is the discriminator.
- `AIDecisionOptions` keeps the M.E.AI-style mutable class with `Clone()` (`ModelId` only).

### Checks

- **Caller input (`ValidatingDecisionClient`, outermost, via the shared internal
  `DecisionQuestionValidator`):** at least one question; every question id non-blank and
  unique within the request (`AskAsync` assigns one first when null); `Instructions` not blank; choice options 2..255 with
  unique, non-blank keys; score levels 2..10, none blank. Never counted as a provider failure.
- **Provider output (`AIErrorClassifyingDecisionClient`, inside tracking):** a mismatch is an
  `AIProviderException`, recorded as a failed call:
  - exactly one answer per question id, no extras, each of the question's kind;
  - every probability in [0, 1];
  - choice: `Probabilities` has exactly the option keys; `Choice` is one of them;
  - score: `Probabilities` has exactly the indexes 0..N-1; `Score` within [0, N-1];
  - choice and score probabilities sum to 1 within `max(0.02, 0.005 × count)` (allows
    two-decimal rounding);
  - `Confidence`, when present, in [0, 1].
  Filling omitted zero entries or re-keying is the provider adapter's job, not the checker's.

### Tracking and telemetry

- One model call = one usage record, however many questions. The audit snapshot lists each
  question (id, kind, instructions, criteria/option keys/level descriptions) and each answer,
  plus `State`.
- OTel span `gen_ai.decision` gets `gen_ai.decision.question_count` and
  `gen_ai.request.kind` (the distinct kinds, comma-joined, e.g. `binary,score`).
  `gen_ai.response.confidence` is dropped: it was per answer, and binary has none.

## Connected systems

- **Deploy, settings, profiles, persistence:** unaffected. The rework changes request/answer
  shapes only; no stored data holds a question or answer.
- **Management API, TS client, Automate, Copilot auto mode, TypeSafe, docs:** all consume the
  reworked types and change with them (see SPEC).
- **Audit log / usage analytics:** snapshot shape changes (see Tracking and telemetry).
  Existing audit rows are opaque JSON and stay readable.

## Key decisions

1. **Continue the spike branch, don't rebuild.** The Core plumbing is reviewed and tested,
   and `origin/v18/dev` merges in conflict-free. Work continues on
   `v18/feature/decision-capability`, brought up to date by merging `v18/dev` in (not a
   rebase, which would need a force-push). The spike's plan folder moves to
   `docs/archive/decision-capability/`.
   *Rejected:* a fresh branch cherry-picking Core commits. Same result, more churn.

2. **One type per decision kind** (see Core types).
   *Rejected:* keeping the spike's flat type. Every consumer (API, TS, Automate outputs)
   would re-check `Kind` and nulls, and it drifts further from #7764.

3. **TypeSafe provider is scaffolded with the `add-provider` skill**, which owns the package
   layout and every registration point (root `.slnx`, demo/test-site install scripts,
   `azure-pipelines.yml` `level1Products`, `version.json`, `changelog.config.json` scope,
   marketplace files, initial `CHANGELOG.md`). Build tasks follow the skill rather than
   restating its checklist here. The only Decision-specific parts are the capability class,
   the client, and the per-file `UMBRACOAI_DECISION` pragma.

   It calls Jev over HTTP directly (`IHttpClientFactory` + `System.Text.Json`), like
   `FireworksAIProvider`.
   *Rejected:* the community `RavenValentin/TypeSafe.Jev` SDK. It targets .NET 11/C# 15 and
   is unofficial.
   - One Jev call per `AIDecisionRequest`: each question is keyed by its `Id`, and `State`
     is sent as Jev's `state` (falling back to the first question's `Instructions` when null,
     since Jev requires a state). Each question's `Instructions` is sent as `instructions`.
     Jev's question-count limit, if documented, is enforced here as a 400-mapped validation
     failure before the call.
   - Score: levels are sent as their descriptions in list order; Jev's index-keyed
     `probabilities` map straight to `AIScoreDecisionAnswer.Probabilities`. If Jev omits
     zero-probability entries (choice or score), the client fills them with 0 so the answer
     is complete. To be confirmed against live Jev.
   - `usage.input_tokens`/`output_tokens` map to `UsageDetails`, so analytics work.
   - `options.ModelId` (falling back to the profile model) is sent as `model`. The models list
     is `jev-latest` only, since Jev documents no models endpoint. Listing models sends one tiny
     authenticated `noul` probe, cached for an hour per connection settings, so "Test
     connection" (which calls `GetModelsAsync`) fails for a bad key.
   - 401 → auth failure, 422 → validation failure, 429/529 → retried up to 2 times with
     exponential backoff (honoring `Retry-After`), then a rate-limit/overloaded failure. All
     surface as `HttpRequestException` with `StatusCode`, which the base provider classifier
     maps (401 Authentication, 422 InvalidRequest, 429 RateLimited, 529 Transient).

4. **Default Decision profile only, no Decision classifier setting.** Copilot auto mode checks
   `HasDefaultProfileAsync(AICapability.Decision)` and asks via the default profile.
   *Rejected:* a separate "Classifier Decision Profile" setting. Jev has one model, so a second
   profile slot adds UI and Deploy fields for no real choice.

5. **Hide disabled experimental capabilities in the UI properly** with one new generic read,
   `GET capabilities/enabled`, returning the `AICapability` names enabled on this install.
   The Settings editor hides a default-profile picker whose capability isn't in that list.
   This fixes ImageGeneration's always-visible empty picker too.
   *Rejected:* matching ImageGeneration's current behavior (empty picker shows).
   *Rejected:* adding flags to the settings response. Settings are persisted data; enabled
   capabilities are install config.
   - `AllProviderController` also drops providers with **zero** enabled capabilities.
     TypeSafe is the first provider whose only capability is experimental. Without this it
     would show in the provider dropdown with nothing to do. Providers with at least one
     enabled capability are unaffected.

6. **Three Automate actions, not one.** "Ask yes/no", "Ask pick-one", "Ask score", each with
   simple settings and a typed output that If/Switch steps can bind to.
   *Rejected:* one action with a kind setting. It would need fields that show and hide by
   kind, which Automate doesn't appear to support, plus a loose output.
   - **Gating:** Umbraco.Automate has no runtime "is this action available" hook. Actions
     are excluded from `builder.AutomateActions()` at compose time when
     `Umbraco:AI:Experimental:Decision` is false, and each `ExecuteAsync` also returns
     `Failed` if the flag is off at run time (flag flipped without a restart).
     `ActionCollectionBuilder` is a `LazyCollectionBuilderBase`, so `Exclude<T>()` works from
     any composer. Known upstream limitation: Umbraco.Automate silently skips a now-missing
     step in an already-published automation (umbraco/Umbraco.Automate#343).
   - **Yes/no cut-off is a setting.** "Ask yes/no" has a `Threshold` (`double`, default 0.5,
     0..1). `Answer` = `TrueProbability >= Threshold`. Automate renders `double` settings with
     `Umb.PropertyEditorUi.Decimal` automatically (`EditableModelSchemaBuilder`). Not bindable:
     Automate only binds `string`/`IList<string>`. Feedback on #7764 measured the trade-off:
     0.5 → 0.9 cut false passes from 27% to 2% but raised false fails from 11% to 30%, so the
     cut-off belongs to whoever builds the automation.
   - The `Context` setting keeps its name and label in the UI (it's what authors know) and
     maps to the request's `State`.
   - **A fourth action, "Ask questions", batches several questions about one Context into
     one call.** Settings: `ProfileId`, `Context` (bindable), `Questions`.
     - `Questions` uses a new `Uai.PropertyEditorUi.DecisionQuestionList` editor: a
       `uui-ref-node` list where "Add" opens the item picker modal to choose the kind
       (yes/no, pick-one, score), then a config modal for that kind; clicking a row reopens its
       config modal. This copies `uai-guardrail-rule-config-builder` /
       `uai-test-grader-config-builder` (`UAI_ITEM_PICKER_MODAL` → config editor modal), which
       also gets around Automate having no per-kind show/hide.
     - Stored as a flat list (`AskDecisionsQuestion { Kind, Alias, Instructions, TrueCriteria,
       FalseCriteria, Threshold, Options[{Key,Value}], Levels[] }`), not a polymorphic one, so
       Automate's settings deserialization needs no `$type` handling.
     - Outputs are per question, keyed by alias, via Automate's `DynamicOutputActionBase`
       (output schema built from the settings, as `RunScriptAction` does). So If/Switch can bind
       `refund.answer` or `category.choice`.
     - Text inside the list isn't bindable (Automate binds top-level `string`/`IList<string>`
       only). Context is, and it's the part that changes per run. The three single-question
       actions stay for when the question text itself must be bound.
     *Rejected:* an inline repeater editor with per-row kind switching (cramped, and unlike
     every other "add configured items" list in the backoffice).

7. **Auto mode routes with a `DecisionAgentSelector`, plugged into Agent's selector chain.**
   `Umbraco.AI.Agent` selects auto-mode agents through `IAIAgentSelectionService` and an ordered
   `AIAgentSelectorCollection` (pluggable agent selection, #463): candidate filtering runs first,
   one candidate short-circuits, then each `IAIAgentSelector` runs until one returns a result,
   else the first candidate (`fallback`). `DecisionAgentSelector` (selector id `decision`, in
   `Agents/Selection/`) is registered by default **before** `LLMAgentSelector`, so the default
   chain is Decision → LLM:
   1. It returns `null` ("no opinion") when Decision is off, no default Decision profile
      resolves, there are more than 255 candidates, Decision throws (logged as a warning;
      cancellation is rethrown), or the answer isn't a candidate id.
   2. Otherwise it asks one `AIChoiceDecisionQuestion`: options are the candidates (key = agent
      id, description = name + description), state = the last user message (as
      `LLMAgentSelector` uses). **No confidence threshold.**
   The sticky selector's guidance becomes `Insert<StickyAgentSelector>()` (first in the chain);
   `InsertBefore<LLMAgentSelector, …>` would now land after Decision, which would override it.
   *Rejected:* keeping the Decision branch inside `AIAgentService` (the selection code moved out
   of it on dev); registering it opt-in only (changes today's behavior for sites using Decision);
   falling back to chat on low confidence.

8. **Experimental suppression is per file, never project-wide.** Consumers outside Core
   (`TypeSafe`, `Web`, `Deploy`, `Automate`, `Agent`) use
   `#pragma warning disable UMBRACOAI_DECISION` in the files that touch Decision, matching
   `OpenAIProvider.cs` (`UMBRACOAI_IMAGEGEN`) and `TranscribeAudioAction.cs` (`MEAI001`).
   *Rejected:* `NoWarn` in csproj. It would hide future experimental APIs too.

9. **Spike code is deleted, not moved.** `Tests.Common/Decision/Spike/*` goes. Its HTTP tests
   are rewritten against the real `TypeSafeDecisionClient` in a new
   `Umbraco.AI.TypeSafe.Tests.Unit`. The T12 "real provider goes inert" tests in Core's
   `AIProfileServiceTests`/`AIConnectionServiceTests` switch to the existing
   `FakeDecisionCapability` (Core tests must not reference a provider package). The new test
   project is added to the root `.slnx`, since CI only runs what's listed there (see
   `project_ci_test_coverage` memory).
   - **Deliberate deviation from `add-provider`:** that skill says providers have no test
     project by convention. The root `CLAUDE.md` lists `tests/ProviderName.Tests.Unit/` as
     the provider shape. TypeSafe gets a test project because, unlike SDK-backed providers,
     its client *is* the wire mapping (hand-written HTTP/JSON). The spike showed every guessed
     wire detail was wrong on first contact, so that mapping needs its own tests.

10. **Release scope and version floors.** Products touched: `Umbraco.AI` (minor),
    `Umbraco.AI.TypeSafe` (new; `18.0.0` on v18, `17.0.0` on v17), `Umbraco.AI.Deploy`,
    `Umbraco.AI.Automate`, `Umbraco.AI.Agent` (minor each). Every add-on that calls new Core
    API raises its `Umbraco.AI.Core` floor to the Core version that ships Decision.

11. **v17 is a straight port after v18 is complete.** Simulated cherry-pick of the spike
    onto `origin/v17/dev` auto-merged every code file. `TranscribeAudioAction` and
    `SelectAgentForPromptAsync` are identical on both lines. Port via the `backport` skill
    as a separate branch and PR. Remaining v17 risk is compile-time only (CMS 17 APIs), to
    be proven by building, not assumed.

12. **Docs are one Umbraco.Docs branch** (`ai/decision-docs`), both `17/` and `18/`, held
    as a draft PR until release. Page list in `SPEC.md`.

## Security

- **Auth:** new endpoints inherit `UmbracoAICoreManagementControllerBase`'s
  `BackOfficeAccess` policy, the same as Chat's completion endpoint and
  `GenerateImageController`, so the TS client is callable from backoffice code outside the AI
  section.
- **Input limits at the API boundary:** the request model enforces the same structural
  limits as `ValidatingDecisionClient` (option/level counts, non-blank keys) before any
  provider call, returning 400. Oversized content is left to Jev's 422, surfaced as 400.
- **API key:** `[AIField(IsSensitive = true)]`, so it's masked in the editor and never
  returned to the client, per the masked-sensitive-fields work.
- **Frontend:** responses contain only option keys/level labels the caller supplied plus
  numbers. Rendered through Lit bindings, never `unsafeHTML`.
