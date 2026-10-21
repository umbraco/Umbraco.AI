# Architecture

Extraction is a new capability that copies how Decision is built, file for file. Decision is the
nearest sibling: an Umbraco-owned client (no M.E.AI type exists), a batch request over shared state,
one typed answer per item, an experimental flag, an Automate action and one HTTP-only provider.
Everything below names the Decision file it copies. The repo's `add-ai-capability` skill is the
checklist for the Core part.

All paths are relative to the repo root. Decision's shape is the same on `v18/dev` and `v17/dev`
(checked 20-10-2026), so the same design applies to both lines.

## Extension points

None of the CMS's own extension points (property editor, dashboard, content app) are the main one.
The feature hangs off Umbraco AI's own capability SDK and two existing extension points:

| Extension point | Used for | Copies |
|---|---|---|
| Umbraco AI capability (`IAICapability`) | `IAIExtractionCapability` + three base classes | `IAIDecisionCapability`, `Providers/IAICapability.cs` |
| Umbraco AI provider (`AIProviderBase<TSettings>`) | New `Umbraco.AI.Cloudflare` package | `Umbraco.AI.TypeSafe` |
| Umbraco Automate action (`DynamicOutputActionBase<TSettings>`) | "Extract Values" action | `AskDecisionsAction` |
| Backoffice property editor UI | Field list editor for the action's settings | `property-editors/decision-question-list/` |

**Why not add question types to Decision:** the answer shapes differ (a value plus a text position,
not a choice plus odds), the providers differ (Clairic and Clef are separate models), and no
Decision provider today could answer them. Recorded in `DECISION-LOG.md`.

## Data model & persistence

No new tables or migrations. Everything new stores through existing generic paths:

- **Profiles:** a new `AIExtractionProfileSettings` (empty, like `AIDecisionProfileSettings`) is stored
  in the existing profile row. `Capability` is stored as an `int`.
- **Capability number:** `AICapability.Extraction = 9`. Slots 6 and 7 are reserved for TextToSpeech
  and SpeechToSpeech. The number is saved in `umbracoAIProfile`, usage, statistics and audit tables,
  and Deploy artifacts, so it can never change after release.
- **Default profile:** a new `AISettings.DefaultExtractionProfileId` (`Guid?`) plus
  `AIOptions.DefaultExtractionProfileAlias` (config fallback). Settings are saved without
  per-field repository code, so no migration is needed.
- **Usage and audit:** recorded through `IAIOperationTracker` with `Capability = Extraction`. No
  schema change.

## Connected systems

What a new capability conventionally touches, from the Decision map and the `add-ai-capability`
skill:

| System | Applies? | Notes |
|---|---|---|
| Experimental flag | Yes | `Umbraco:AI:Experimental:Extraction`, default off. `AIExperimentalOptions.Extraction` + one arm in `AIExperimentalFeatures`. Compile time: `[Experimental("UMBRACOAI_EXTRACTION")]`. |
| Usage analytics + audit log | Yes, generic | `AITrackingExtractionMiddleware` calls `IAIOperationTracker.TrackAsync`. The dashboards pick it up by capability. |
| OpenTelemetry | Yes | `AIOpenTelemetryExtractionMiddleware`, must call `AITraceTags.Apply(activity)`. |
| Notifications | Yes | `AIExtractionExecutingNotification` (cancelable) + `AIExtractionExecutedNotification`, same constructor shape as Decision's. |
| Runtime context scopes | Yes | `AIRuntimeContextCallScope.Begin(...)` + `SetProfileMetadata`. Feature type `Constants.FeatureTypes.InlineExtraction = "inline-extraction"`. |
| Deploy: profiles | Yes, generic | `UmbracoAIProfileServiceConnector` already serializes settings by runtime type. Needs only a test. |
| Deploy: settings | Yes | `AISettingsArtifact.DefaultExtractionProfileUdi` + export/import in `UmbracoAISettingsServiceConnector`, like Decision's. |
| Management API capability list | Yes | Add to `EnabledCapabilitiesController.ImplementedCapabilities`. |
| Profile editor UI | Yes | Empty settings element ("no settings"), type guard, mapper arms. |
| Settings screen default-profile picker | Yes | New row, shown only when the flag is on. |
| Guardrails, test graders | No | Decision has `DecisionGuardrailEvaluator` and `DecisionJudgeGrader`. Extraction has no "pass/fail" meaning to judge with. Out of scope. |
| Agent tools, Copilot, Prompt | No | Non-goals in `BRIEF.md`. |
| New provider package plumbing | Yes | Marketplace metadata, install scripts, CI registration, `changelog.config.json` scope (the `add-provider` skill's list; the release manifest is added on the release branch). |
| Root test solution | Yes | New test projects must be added to the root slnx, or CI never runs them. |
| Version history, search indexing, cache refreshers | No | No content entities are added. |

## Key decisions

### 1. Core types mirror Decision's, with one subclass per field type

```
AIExtractionRequest   { string? State; required IReadOnlyList<AIExtractionField> Fields; DateOnly? ReferenceDate }
AIExtractionResponse  { required IReadOnlyDictionary<string, AIExtractionAnswer> Answers; string? ModelId; UsageDetails? Usage; object? RawRepresentation }
AIExtractionResponse<TAnswer> : AIExtractionResponse { required TAnswer Answer }
AIExtractionOptions   { string? ModelId { get; set; }; Clone() }            // mutable class, like AIDecisionOptions

AIExtractionField (abstract)  { string? Id; required string Instructions; internal abstract Type ExpectedAnswerType; internal WithId(string) }
AIExtractionField<TAnswer>
  AITextExtractionField      → AITextExtractionAnswer      { string? Value; AIExtractionSpan? Span }
  AINumberExtractionField    → AINumberExtractionAnswer    { decimal? Value }          + Decimals?, Integer, Unit?
  AIDateExtractionField      → AIDateExtractionAnswer      { DateOnly? Value }
  AITimeExtractionField      → AITimeExtractionAnswer      { TimeOnly? Value }
  AIDateTimeExtractionField  → AIDateTimeExtractionAnswer  { DateTime? Value }
  AIDurationExtractionField  → AIDurationExtractionAnswer  { decimal? Value; AIDurationUnit Unit } + Unit, Decimals?, Integer
  AIListExtractionField      → AIListExtractionAnswer      { IReadOnlyList<string> Values; IReadOnlyList<AIExtractionSpan> Spans } + ItemType

AIExtractionAnswer (abstract) { double? Confidence; double? NullProbability; bool IsFound; object? RawRepresentation }
AIExtractionSpan(int Start, int End)   // character offsets into State, End exclusive (Clairic's [8,16] for an 8-char value at 8)
```

- **Why subclasses, not one class with a `Type` property:** Decision rejected a flat shape with a
  discriminator and nullable per-kind fields, and its single-answer `AskAsync<TAnswer>` overload
  depends on the generic subclass. Copying it keeps `ExtractAsync<TAnswer>` cast-free.
- **Number is `decimal`, not `double`:** most extracted numbers are money. Clairic's `decimals`
  option already rounds.
- **Duration stays a number plus a unit, not `TimeSpan`:** Clairic supports months and years, which
  `TimeSpan` can't represent.
- **List answers are strings in v1.** Item type (text, number, date) is sent to the provider, but
  values come back as invariant strings. Typed lists can come later without breaking this shape.
  > ASSUMPTION: Strings are good enough for v1, since Automate turns every value into a string
  > anyway.
- **"Not found" is `Value == null` (or an empty list), never a guess.** `IsFound` is a convenience.
  Clairic's `guess` field (what it would have picked) is not exposed; it's only in
  `RawRepresentation`.
- **Left out of v1:** `estimate`/`forecast` types, `images`, `null_threshold`, quantiles,
  `return_state_text`. Each is an additive change later.

### 2. Validation is provider-neutral and runs before any provider is called

`ValidatingExtractionClient` (outermost wrapper, like `ValidatingDecisionClient`) and an internal
`ExtractionFieldValidator`:

- At least one field. No upper limit in Core (Clairic has none).
- Every field id is non-blank, unique, and matches `^[A-Za-z_][A-Za-z0-9_]{0,63}$`. This is the
  safe overlap of Clairic's field-name rule and Automate's alias rule, and it's also a valid JSON
  property name for a future chat fallback.
- `Instructions` non-blank. `State` non-blank.
- `Decimals`, when set, is 0 to 6.

`ExtractionAnswerChecker` (like `DecisionAnswerChecker`) turns a missing, extra or wrong-type answer
into an `AIProviderException`.

### 3. Client pipeline copies Decision's wrapping order

`AIExtractionClientFactory` wraps, inside out: provider client → `AIErrorClassifyingExtractionClient`
→ middleware (`AIOpenTelemetryExtractionMiddleware`, `AITrackingExtractionMiddleware`) →
`ScopedProfileExtractionClient` → `ValidatingExtractionClient`. The skill's gotcha applies:
validation must sit outside error classification and tracking, covered by a factory-level test.

### 4. Service API copies `IAIDecisionService`

```
Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(AIExtractionField<TAnswer> field, string? state = null, AIExtractionOptions? options = null, CancellationToken ct = default)
  + (Guid profileId, ...), (string profileAlias, ...), (Action<AIExtractionBuilder> configure, ...)
Task<AIExtractionResponse> GetExtractionResponseAsync(AIExtractionRequest request, AIExtractionOptions? options = null, CancellationToken ct = default)
  + (Guid profileId, ...), (string profileAlias, ...), (Action<AIExtractionBuilder> configure, ...)
```

`AIExtractionBuilder` has `WithAlias`, `WithName`, `WithProfile(Guid)`, `WithProfile(string)`,
`WithContextItems`, `WithExtractionOptions`, `AsPassThrough`, like `AIDecisionBuilder`. Method names
follow M.E.AI style (`GetResponseAsync` on the client) for the same reason Decision's do.

### 5. Provider: new `Umbraco.AI.Cloudflare` package, Clairic only

Named after the company (`add-provider` rule), so Cloudflare chat, embeddings and Clef can be added to
the same package later.

- `[AIProvider("cloudflare", "Cloudflare")] CloudflareProvider : AIProviderBase<CloudflareProviderSettings>`,
  constructor `(IAIProviderInfrastructure, IHttpClientFactory, IMemoryCache)`, like `TypeSafeProvider`.
- `CloudflareProviderSettings`: `AccountId` (required), `ApiToken` (required, `IsSensitive = true`),
  `Endpoint` (default `https://api.cloudflare.com/client/v4`).
- `CloudflareExtractionCapability : AIExtractionCapabilityBase<CloudflareProviderSettings>`. Model
  list is static: `@cf/cloudflare/clairic`. Cloudflare's model search can't tell Clairic apart from
  chat models (both are "Text Generation"), so listing from the API adds nothing.
- `CloudflareExtractionClient` copies `TypeSafeDecisionClient`:
  `POST {Endpoint}/accounts/{AccountId}/ai/run/@cf/cloudflare/clairic`, Bearer token, up to 3
  attempts, retries only on 429 (honours `Retry-After` if sent, otherwise backs off, capped at 30s),
  delay and clock injected for tests.
- **Response envelope:** Cloudflare's REST API normally wraps results as
  `{ result, success, errors, messages }`, but the Clairic docs never show a REST response. The
  client accepts both the wrapped and the bare shape. On failure it reads `errors[].code/message`
  into the exception, so a "capacity exceeded" (3040) or "daily free limit used" (3036) error
  reads clearly in the audit log.
- **Connection check:** calls `GET {Endpoint}/accounts/{AccountId}/ai/models/search?search=clairic`.
  This proves the account ID and token without spending tokens. The result is cached for 1 hour
  per token hash, like TypeSafe's probe.
- **Usage:** `input_tokens` → `InputTokenCount`, `output_tokens` → `OutputTokenCount` (always 0).
- `RawRepresentation` keeps the provider's answer object so `guess`, `spans` etc. stay reachable.

### 6. Chat fallback: deferred to a follow-up

`BRIEF.md` marked it nice-to-have, to be dropped if messy or costly. It's both:

- The Automate profile picker filters by one capability. A fallback needs either a second "fallback
  chat profile" picker or a picker change to accept two capabilities.
- Strict JSON-schema output behaves differently per provider (DeepSeek downgrades to plain JSON,
  OpenAI strict mode rejects some schema shapes, see #249). That's a test matrix across 15+ providers.
- The answers would lack confidence and positions, so Automate recipes that route on confidence
  would silently behave differently.

**If picked up later,** the shape is already clear: a consumer-side fallback inside
`AIExtractionService` when no Extraction profile resolves, copying the
`DecisionAgentSelector` → `LLMAgentSelector` chain in `Umbraco.AI.Agent`. Never a pseudo-capability
that makes every chat provider claim Extraction. Nothing in this design blocks it: the Automate action
only calls `IAIExtractionService`.

**Rejected:** a Core-made "chat-backed Extraction" capability on every chat provider. It would list
15+ providers under Extraction, record chat tokens as Extraction usage, and hide the quality gap.

### 7. Management API: one batch endpoint

`POST /umbraco/ai/management/api/v1/extraction/extract`, in
`Umbraco.AI.Web/Api/Management/Extraction/`, copying `DecisionControllerBase` (group name, versioned
route, `[AICapabilityGate(AICapability.Extraction)]`, same auth base class).

- **Batch, unlike Decision's single-question `ask`:** extraction is almost always several fields
  over one text, and the backoffice client needs that. Recorded in `DECISION-LOG.md`.
- Fields and answers are polymorphic with a `$type` discriminator
  (`text|number|date|time|datetime|duration|list`), like `DecisionQuestionModel`.
- The answers dictionary uses `ExactKeyDictionaryJsonConverter` so field ids aren't camel-cased.
- The controller returns `OkObjectResult { DeclaredType = typeof(ExtractResponseModel) }`, or `$type`
  is dropped (Decision's quirk).
- **v17:** the generated client on v17 has no `$type` on the base models, so the frontend data source
  needs the same per-line split Decision's has.

### 8. Automate action: "Extract Values"

`ExtractValuesAction : DynamicOutputActionBase<ExtractValuesSettings>` in `Umbraco.AI.Automate`,
copying `AskDecisionsAction`:

- **Settings:**
  - `Fields` (`Uai.PropertyEditorUi.ExtractionFieldList`, max 20, not bindable)
  - `Context` (text area, bindable)
  - `ReferenceDate` (text, bindable, optional, ISO date)
  - `ProfileId` (`Uai.PropertyEditorUi.ProfilePicker`, `capability: Extraction`)
- **Outputs, always one per field alias, even when not found:**
  `{ value, found, confidence }`. `AskDecisions` leaves a missing answer out. Extraction doesn't,
  so `${steps.x.orderId.found}` always resolves and an `If` on it is reliable.
- **Output schema types:** text, date, time, datetime → `string`; number, duration → `number`;
  list → `array` of `string`; `found` → `boolean`; `confidence` → `["number","null"]`.
- Dates come out as ISO strings, so Automate's string comparison orders them correctly.
- **Gating:** excluded at compose time when `Umbraco:AI:Experimental:Extraction` is off, plus the
  run-time `IsCapabilityEnabled` check that returns a validation failure. Same as the Decision
  actions.
- **Errors:** validation problems → `StepRunErrorCategory.Validation`; final catch uses
  `StepErrorCategoryMapping.FromException(ex)`.

### 9. Field list editor lives in Core's frontend, like Decision's

`Uai.PropertyEditorUi.ExtractionFieldList` + `Uai.Modal.ExtractionFieldConfigEditor` in
`Umbraco.AI.Web.StaticAssets/Client/src/property-editors/extraction-field-list/`, copying
`decision-question-list/` (~740 lines of source). "Add field" opens the item picker to choose a
type, then a per-type config modal.

> ASSUMPTION: The two editors stay separate copies. A shared generic "typed item list" component
> would cut some duplication but is a refactor of shipped Decision code. Not worth the risk here.

### 10. Ships on v18 and v17 together

Build on `v18/dev`, then port to `v17/dev` on its own branch and PR (Backport Workflow). The only
known per-line difference is the frontend data source (decision 7). Migration-free, so the
migration-ID drift problem doesn't apply.

## Adding a field type later

Each field type name is a separate wire or storage contract, so it lives in five places. A new
type (for example `estimate`) touches all of them:

- Core: the field/answer pair, `ExtractionFieldKind`, the validator and answer checker.
- Management API: the `$type` field and answer models in `ExtractRequestModel` and
  `ExtractResponseModel`, and the controller mapping.
- Cloudflare: the request type name and the response mapping in `CloudflareExtractionClient`.
- Automate: `ExtractValuesAction`'s kind mapping and output schema.
- Frontend: `extraction-field-kinds.ts`, the field modal's extra settings, `src/extraction/types.ts`
  and the server data source mapping.
