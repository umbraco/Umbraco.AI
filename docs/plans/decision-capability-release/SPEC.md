# Spec

"Flag on/off" below means `Umbraco:AI:Experimental:Decision` = `true`/`false` (default
`false`). All routes are under `/umbraco/ai/management/api/v1/`.

## Management API surface

### `POST decision/ask` (new, `AskDecisionController`, group "Decision")

Request:

```json
{
  "profileIdOrAlias": "my-decision-profile",   // optional; omitted = default Decision profile
  "question": {
    "$type": "binary",                          // "binary" | "choice" | "score"
    "instructions": "Is this comment spam?",
    "context": "Buy cheap watches at ...",      // optional
    "trueCriteria": "Promotional or scam",      // binary only, optional
    "falseCriteria": "A genuine comment",       // binary only, optional
    "options": [{ "key": "seo", "description": "SEO help" }],  // choice only, 2..255
    "levels": ["poor", "ok", "good"]            // score only, 2..10, lowest first
  }
}
```

Responses (`$type` matches the question's):

```json
{ "$type": "binary", "answer": true, "probability": 0.97, "confidence": 0.97, "modelId": "jev-latest", "usage": { "inputTokens": 42, "outputTokens": 1 } }
{ "$type": "choice", "choice": "seo", "confidence": 0.91, "probabilities": { "seo": 0.91, "other": 0.09 }, "modelId": "...", "usage": {...} }
{ "$type": "score",  "score": 1.8, "level": "good", "confidence": 0.8, "probabilities": { "poor": 0.05, "ok": 0.15, "good": 0.8 }, "modelId": "...", "usage": {...} }
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
  `kind: "binary" | "choice" | "score"`), and `options` = `{ profileIdOrAlias?, signal? }`.
- Returns `{ data?, error? }`. `data`'s type is narrowed by the question type through
  overloads: a binary question yields `UaiBinaryDecisionResult`
  (`answer`, `probability`, `confidence`, `modelId?`), and likewise for choice and score.
- Maps `kind` ↔ the API's `$type`. The generated OpenAPI types never leak into the public
  types.
- Exported through `src/decision/exports.ts` → root `src/exports.ts`, tagged `@public`,
  present in the api-extractor rollup (`types/umbraco-ai-public-types.d.ts`).
- JSDoc states the feature is experimental and that the server returns 404 when it's off.
  A 404 comes back as `error`, never thrown.

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
- Sends `POST {Endpoint}/v1/systemone` with `Authorization: Bearer <ApiKey>`, one question
  keyed `"q"`:
  - binary → `type: "noul"`. `criteria` is `{ "true", "false" }` only if either criteria is
    set, otherwise omitted entirely (never `null`).
  - choice → `type: "choice"`, `criteria` = `{ key: description ?? key }`.
  - score → `type: "score"`, `criteria` = the levels array.
- Maps `noul` → `Probability`. Maps `choice`/`score` → answer, confidence, and probabilities
  (score probabilities re-keyed from level index to label). Maps `usage` → `UsageDetails`.
- 429/529 retried at most twice with backoff, honoring `Retry-After`. 401/422 are not
  retried.
- Test connection succeeds with a valid key and fails with a clear message on 401.
  TODO: build confirms how "test connection" works for a provider with no chat capability.

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
(optional, bindable).

| Action | Extra settings | Output (bindable in If/Switch) |
|--------|----------------|--------------------------------|
| Ask yes/no | `TrueCriteria`, `FalseCriteria` (optional) | `Answer` (bool), `Probability`, `Confidence` |
| Ask pick-one | `Options` (2..255, key + optional description) | `Choice` (key), `Confidence` |
| Ask score | `Levels` (2..10, lowest first) | `Score` (number), `Level` (label), `Confidence` |

- Flag off at startup → none of the three appear in the action picker.
- Flag off at run time → step fails with category `Validation` and a message saying Decision
  is disabled. No provider call.
- Invalid settings → `Validation` failure. Provider errors → `Unknown` failure with the
  provider's message.
- TODO: build confirms which Automate field editor fits `Options`/`Levels` (list editor vs.
  one-per-line text).

## Copilot auto mode (`Umbraco.AI.Agent`)

- 0 or 1 available agents: unchanged (no model call).
- 2..255 agents, flag on, default Decision profile set: exactly one Decision call, no chat
  call. The chosen agent is the one whose id Jev returns. The `agent_selected` event
  content is unchanged.
- Flag off, or no default Decision profile, or Decision throws, or it returns an unknown
  key, or more than 255 agents: behavior is identical to today (classifier chat profile →
  default chat → first agent), and a failed Decision attempt is logged.
- Frontend: no change.

## Removed

- `Umbraco.AI/tests/Umbraco.AI.Tests.Common/Decision/Spike/` (all 6 files) and any test
  referencing `JevSpike*`.
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
- `umbraco-automate/add-ons/ai/actions.md` (three new actions)

Experimental pages use the existing `{% hint style="warning" %}` pattern with the flag JSON
and the diagnostic id.
