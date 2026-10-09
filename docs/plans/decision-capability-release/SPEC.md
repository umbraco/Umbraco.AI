# Spec

"Flag on/off" below means `Umbraco:AI:Experimental:Decision` = `true`/`false` (default
`false`). All routes are under `/umbraco/ai/management/api/v1/`.

## Management API surface

### `POST decision/ask` (new, `AskDecisionController`, group "Decision")

One question per call. Several questions in one call is C#-only for now
(`IAIDecisionService.GetDecisionResponseAsync`).

Request:

```json
{
  "profileIdOrAlias": "my-decision-profile",   // optional; omitted = default Decision profile
  "state": "Buy cheap watches at ...",          // optional; the content being judged
  "question": {
    "$type": "binary",                          // "binary" | "choice" | "score"
    "instructions": "Is this comment spam?",
    "trueCriteria": "Promotional or scam",      // binary only, optional
    "falseCriteria": "A genuine comment",       // binary only, optional
    "options": [{ "key": "seo", "description": "SEO help" }],      // choice only, 2..255
    "levels": [{ "description": "poor" }, { "description": "ok" }, { "description": "good" }] // score only, 2..10, lowest first
  }
}
```

Responses (`$type` matches the question's; `confidence` omitted when the provider gives none):

```json
{ "$type": "binary", "trueProbability": 0.97, "modelId": "jev-1.13.0", "usage": { "inputTokens": 42, "outputTokens": 1 } }
{ "$type": "choice", "choice": "seo", "confidence": 0.91, "probabilities": { "seo": 0.91, "other": 0.09 }, "modelId": "...", "usage": {...} }
{ "$type": "score",  "score": 1.8, "confidence": 0.8, "probabilities": { "0": 0.05, "1": 0.15, "2": 0.8 }, "modelId": "...", "usage": {...} }
```

Guarantees:

- Flag off → **404**, empty body, before profile lookup or validation. Still listed in
  OpenAPI (same as `image-generation/generate`).
- Missing/blank `instructions`, choice options outside 2..255 or with duplicate/blank keys,
  score levels outside 2..10 or blank, or unknown `$type` → **400** ProblemDetails. No
  provider call is made.
- `profileIdOrAlias` given but not found → **404** ProblemDetails (`ProfileNotFound`).
- Profile found but not a Decision profile → **400** ProblemDetails.
- No `profileIdOrAlias` and no default Decision profile configured → **400** ProblemDetails
  saying no default Decision profile is set.
- Provider validation failure (Jev 422) → **400** ProblemDetails. Other provider failures
  (auth, rate limit after retries, overloaded, network) → the same status/ProblemDetails
  shape `GenerateImageController` uses for provider errors.
- Provider answer incomplete or inconsistent (missing/extra probabilities, sum off by more
  than the rounding tolerance, a choice that isn't one of the keys, a score outside 0..N-1)
  → the provider-error status/ProblemDetails, and the call is recorded as failed.
- Success → **200**, and one usage record appears in analytics for the Decision capability.

### `GET capabilities/enabled` (new)

- Returns a JSON array of `AICapability` names enabled on this install, e.g.
  `["Chat","Embedding","SpeechToText"]`.
- Includes `ImageGeneration` only when its flag is on, and `Decision` only when its flag
  is on. Excludes reserved, unimplemented values (`Moderation`, `Media`).
- Same auth as the other capability-listing endpoints.

### Changed endpoints

- `GET settings` / `PUT settings`: new `defaultDecisionProfileId` (nullable GUID).
  Round-trips like `defaultImageGenerationProfileId`. No capability check on `PUT`, matching
  the other default slots (none validate today; the UI picker filters to Decision profiles).
- `GET providers`: providers with zero enabled capabilities are omitted. Flag off →
  TypeSafe absent. Flag on → TypeSafe present with `["Decision"]`. Every other provider's
  output is unchanged.
- Profile endpoints: `settings` accepts/returns `{ "$type": "decision" }` for Decision
  profiles. Create/update of a Decision profile with the flag off → 400 (existing generic
  gate, unchanged).

## Frontend components (`@umbraco-ai/core`)

### `UaiDecisionController` (public, `src/decision/`)

- `ask(question, options?)`, where `question` is one of `UaiBinaryDecisionQuestion`,
  `UaiChoiceDecisionQuestion`, `UaiScoreDecisionQuestion` (TS discriminated union on
  `kind: "binary" | "choice" | "score"`), and `options` = `{ state?, profileIdOrAlias?, signal? }`.
  Score levels are `{ description }[]`.
- Returns `{ data?, error? }`. `data`'s type is narrowed by the question type through
  overloads (plus a union overload):
  - binary → `UaiBinaryDecisionResult`: `trueProbability`, `modelId?`, `usage?`;
  - choice → `UaiChoiceDecisionResult`: `choice`, `probabilities` (by key), `confidence?`, …;
  - score → `UaiScoreDecisionResult`: `score`, `probabilities` (`Record<number, number>`, by
    level index), `confidence?`, ….
- Maps `kind` ↔ the API's `$type`. The generated OpenAPI types never leak into the public
  types.
- Exported through `src/decision/exports.ts` → root `src/exports.ts`, tagged `@public`,
  present in the api-extractor rollup (`types/umbraco-ai-public-types.d.ts`).
- JSDoc states the feature is experimental and that the server returns 404 when it's off.
  A 404 comes back as `error`, never thrown.

### `Uai.PropertyEditorUi.DecisionQuestionList` (Automate "Ask questions")

- Renders the questions as a `uui-ref-node` list: name = the instructions (truncated), detail =
  kind and alias. Each row has edit and remove.
- "Add question" opens the item picker modal listing Yes/no, Pick-one and Score. Choosing one
  opens a config modal for that kind over the picker; submitting adds the question and closes
  both; cancelling returns to the picker.
- Clicking a row opens its config modal directly; submit replaces it, cancel leaves it.
- The config modal edits Alias, Instructions and the kind's fields. Pick-one options use the
  key/value list editor; score levels use the CMS multiple-text-string editor. It won't submit
  with a blank or duplicate alias, or a field outside its bounds.
- Value: `[{ kind, alias, instructions, ... }]`, the flat shape in ARCHITECTURE decision 6.
  Emits `UmbChangeEvent` on every add/edit/remove.
- Built like `uai-guardrail-rule-config-builder`.

### Enabled capabilities (internal)

- A repository method returning the enabled-capability list from `GET capabilities/enabled`,
  fetched once per backoffice session and shared.

### Settings editor (`settings-editor.element.ts`)

- New "Default Decision Profile" picker (`capability="Decision"`), saved as
  `defaultDecisionProfileId`.
- The Decision and ImageGeneration pickers render **only** when their capability is in the
  enabled list. The other pickers always render.
- A hidden picker's saved value is preserved on save, not cleared.

### Profile workspace

- A Decision profile opens without errors and shows a Decision settings view
  (`uai-decision-profile-settings`). It has no fields today and says so, rather than
  rendering blank.
- Capability shows as "Decision" everywhere capabilities are labelled
  (`uaiCapabilities_decision` in `lang/en.ts`).

## Provider: `Umbraco.AI.TypeSafe`

- Provider id `typesafe`, name "TypeSafe AI". Settings: `ApiKey` (sensitive, required),
  `Endpoint` (default `https://api.typesafe.ai`).
- Exposes only `Decision`. Models list: `jev-latest`.
- Sends one `POST {Endpoint}/v1/systemone` per request with `Authorization: Bearer <ApiKey>`,
  `state` = the request's `State` (first question's instructions when null), and every
  question keyed by its id:
  - binary → `type: "noul"`. `criteria` is `{ "true", "false" }` only if either criteria is
    set, otherwise omitted entirely (never `null`).
  - choice → `type: "choice"`, `criteria` = `{ key: description ?? key }`.
  - score → `type: "score"`, `criteria` = the level descriptions in order.
- Maps `noul` → `TrueProbability`. Maps `choice`/`score` → answer, confidence, and
  probabilities (score probabilities kept by level index). Fills omitted zero-probability
  entries with 0. Maps `usage` → `UsageDetails` (once per call).
- A three-question request (binary + choice + score) makes exactly one HTTP call and returns
  three keyed answers.
- 429/529 retried at most twice with backoff, honoring `Retry-After`. 401/422 are not
  retried.
- Test connection succeeds with a valid key and fails with a bad key. It calls
  `GetModelsAsync`, which sends one cached `noul` probe (see ARCHITECTURE decision 3).

## Deploy

- `AISettingsArtifact.DefaultDecisionProfileUdi` is exported when set and declared as a
  dependency, so the profile deploys first. On import it's mapped back to
  `DefaultDecisionProfileId`.
- A Decision profile deploys and imports with its settings intact (non-null
  `AIDecisionProfileSettings`).
- A TypeSafe connection deploys like any other: the sensitive key isn't in the artifact.

## Automate actions (`Umbraco.AI.Automate`)

Group "AI". Common settings: `ProfileId` (profile picker, `capability: "Decision"`,
empty = default Decision profile), `Instructions` (required, bindable), `Context`
(optional, bindable; sent as the request's `State`).

| Action | Extra settings | Output (bindable in If/Switch) |
|--------|----------------|--------------------------------|
| Ask yes/no | `TrueCriteria`, `FalseCriteria` (optional), `Threshold` (0..1, default 0.5) | `Answer` (bool, `Probability >= Threshold`), `Probability` |
| Ask pick-one | `Options` (2..255, key + optional description) | `Choice` (key), `Confidence` (empty if none) |
| Ask score | `Levels` (2..10, lowest first) | `Score` (number), `Level` (label of the nearest level), `Confidence` (empty if none) |

### Ask questions (batch)

- Settings: `ProfileId`, `Context` (bindable, sent as `State`), `Questions` (1..20, the
  question-list editor below).
- Each question has an `Alias` (required, unique in the step, letters/digits/underscore,
  starting with a letter; used as its output key), a kind, `Instructions`, and the kind's
  fields: yes/no `TrueCriteria`, `FalseCriteria`, `Threshold` (0..1, default 0.5); pick-one
  `Options` (2..255 key/value); score `Levels` (2..10).
- One run = one Decision call, whatever the number of questions.
- Output: one object per alias. Yes/no `{ answer, probability }`, pick-one
  `{ choice, confidence }`, score `{ score, level, confidence }`. The output schema follows the
  configured questions, so the binding picker lists each alias and its fields.
- Duplicate or invalid aliases, or any invalid question → `Validation` failure, no provider
  call.

- Flag off at startup → none of the four appear in the action picker.
- Flag off at run time → step fails with category `Validation` and a message saying Decision
  is disabled. No provider call.
- `Threshold` outside 0..1 → `Validation` failure, no provider call.
- Invalid settings → `Validation` failure. Provider errors → `Unknown` failure with the
  provider's message.
- Editors: `Options` uses the `Uai.PropertyEditorUi.KeyValueList` editor (rows of key +
  value, add/remove/reorder, min 2 / max 255), stored as `[{ key, value }]`; the value is the
  optional description. `Levels` uses the CMS `MultipleTextString` list editor. Instructions,
  Context and the yes/no criteria support bindings; options, levels and profile don't.

## Copilot auto mode (`Umbraco.AI.Agent`)

- `DecisionAgentSelector` is registered by default before `LLMAgentSelector`
  (`builder.AIAgentSelectors()`), selector id `decision`.
- 0 or 1 candidate agents: unchanged (no selector runs).
- 2..255 candidates, flag on, default Decision profile set: exactly one Decision call, no chat
  call. The chosen agent is the one whose id Jev returns, and `AIAgentSelectedNotification` /
  audit metadata record selector id `decision`. The `agent_selected` event content is unchanged.
- Flag off, no default Decision profile, Decision throws, an unknown key, or more than 255
  candidates: the selector returns null and the chain carries on exactly as without it (LLM
  classifier → first candidate); a failed Decision attempt is logged.
- `StickyAgentSelector`'s docs say to register it first (`Insert<StickyAgentSelector>()`).
- Frontend: no change.

## Removed

- `Umbraco.AI/tests/Umbraco.AI.Tests.Common/Decision/Spike/` (all 6 files) and any test
  referencing `JevSpike*`.
- `AIBinaryDecisionResponse`/`AIChoiceDecisionResponse`/`AIScoreDecisionResponse` (replaced by
  per-kind answers in a keyed `AIDecisionResponse`), `AIDecisionQuestion.Context` (now
  `AIDecisionRequest.State`), the abstract `Confidence` on every response, `Probability`
  (now `TrueProbability`), `Answer` (now `IsTrue(threshold)`), the score `Level` label and
  label-keyed score probabilities, and the yes/no Automate output `Confidence`.
- `AIDecisionKind`, the flat `AIDecisionQuestion`/`AIDecisionResponse` shapes, and their
  `ForBinary`/`ForChoice`/`ForScore` factories.
- No file or type name in `src/` or `tests/` contains "Spike" or "Jev" (except
  `jev-latest` as a model id).

## Docs (Umbraco.Docs, each in `17/` and `18/`)

New (`ai-in-umbraco/` unless noted):
- `using-the-api/decision/README.md`: experimental warning, enabling, `IAIDecisionService`
  for all three kinds.
- `extending/providers/decision-capability.md`
- `management-api/decision/README.md`, `management-api/decision/ask-decision.md`
- `frontend/decision-controller.md`
- `providers/typesafe.md`

Edited:
- `SUMMARY.md`, `concepts/capabilities.md`, `reference/models/ai-capability.md`,
  `reference/configuration/ai-options.md`, `providers/README.md`
- `frontend/README.md`, `frontend/types.md`
- `concepts/settings.md`, `backoffice/managing-settings.md`, `backoffice/managing-profiles.md`
- `management-api/settings/get.md`, `management-api/settings/update.md`
- `add-ons/deploy/deploying-entities.md`
- `add-ons/agent-copilot/copilot.md` (auto mode routing)
- `umbraco-automate/add-ons/ai/actions.md` (four new actions)

The pages describe the reworked shapes: `state` on the request, `GetDecisionResponseAsync`
for several questions in one call, `TrueProbability` with `IsTrue(threshold)`, optional
`Confidence`, score probabilities by level index, and the yes/no `Threshold` setting.

Experimental pages use the existing `{% hint style="warning" %}` pattern with the flag JSON
and the diagnostic id.
