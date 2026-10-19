# Spec

Slice 1, v18 only. See `ARCHITECTURE.md` for the types and the reasons.

## Management API surface

### `GET /umbraco/ai/management/api/v1/connections/{connectionIdOrAlias}/model-facts`

Controller: `ModelFactsConnectionController` (Connection API group), `[ApiVersion("1.0")]`,
`[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]`.

Query:

| Param | Required | Meaning |
|-------|----------|---------|
| `capability` | yes | `AICapability` name (case-insensitive), e.g. `Chat`. |
| `modelId` | no | Only return facts for this model. Absent = every model the connection lists for the capability. |

Response `200`: `ModelFactsResponseModel`

```json
{
  "items": [
    {
      "model": { "providerId": "openrouter", "modelId": "anthropic/claude-sonnet-4" },
      "facts": [
        {
          "key": "core.contextWindow",
          "label": "#uaiModelFacts_contextWindow",
          "shortLabel": "#uaiModelFacts_contextWindowShort",
          "value": "200,000",
          "sortValue": 200000,
          "detail": null,
          "tone": "Neutral",
          "url": null
        }
      ]
    }
  ]
}
```

Guarantees:

- Unknown connection id/alias → `404` ProblemDetails (same helper as the models endpoint).
- Missing or unparseable `capability` → `400` ProblemDetails.
- Capability the connection doesn't support → `200` with empty `items`.
- `modelId` not in the connection's list → `200` with empty `items`.
- Models with no facts are omitted from `items`. No fact providers registered → empty `items`.
- Facts in each item are already in display order (warnings first, then fact-provider
  registration order). No display cap is applied here; that's a frontend rule.
- A fact provider that throws or exceeds its timeout is skipped and logged; the request still
  returns `200` with the other providers' facts.
- A failure to list the connection's models → `200` with empty `items`, logged at Warning (not
  swallowed silently like `GET …/models` does today).
- `url` values that aren't absolute `http`/`https` are dropped server-side (set to `null`).
- Response never includes connection settings.

### Core service behavior (`IAIModelFactService`)

- Calls each registered `IAIModelFactProvider` **once per request**, with only the models not
  already cached for that provider.
- Caches each provider's per-model result for its `CacheDuration`; `TimeSpan.Zero` means no caching.
- Caches an empty result too, so "no facts for this model" doesn't re-query every time.
- Runs providers concurrently, each under `AIModelFactOptions.ProviderTimeout` (default 2 s).
- Ignores facts for model ids that weren't requested.

### Built-in `AIMetadataModelFactProvider` (registered first by core)

| Metadata present | Fact |
|------------------|------|
| `model.contextWindow` | key `core.contextWindow`, label `#uaiModelFacts_contextWindow`, value formatted with thousands separators, `sortValue` = tokens |
| `pricing.*` (all three keys) | key `core.price`, label `#uaiModelFacts_price`, value `"$3.00 / $15.00"` (currency symbol for USD, else ISO code prefix), `sortValue` = input price per 1M, `detail` = `#uaiModelFacts_priceDetail` ("Per 1M tokens, input / output. From the provider; may be out of date.") |
| Partial or malformed values | No fact for that key (never throws) |

### OpenRouter provider

- `OpenRouterModelInfo` also reads `context_length` and `pricing.prompt` / `pricing.completion`.
- `GetModelsAsync` writes `model.contextWindow` and the three `pricing.*` keys (per-token × 1,000,000,
  currency `USD`) alongside the existing settings-support metadata.
- Missing, zero or unparseable values → key not written.

## Frontend components

All in `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src`, all internal (not added to
`exports.ts`).

### `UaiConnectionModelFactsRepository` + server data source

Location: `connection/repository/model-facts/`, mirroring `connection/repository/models/`.

- `requestModelFacts({ connectionId, capability, modelId? })` → `{ data?: UaiModelFactsModel[], error? }`.
- Maps the response to `UaiModelFactModel { key, label, shortLabel?, value, sortValue?, detail?, tone, url? }`.

### `<uai-model-facts>`

Location: `profile/components/model-facts/`, exported through `profile/components/index.ts` (internal
barrel), not `exports.ts`.

Properties: `connectionId`, `capability`, `modelId`.

Must observably:

- Render nothing until all three properties are set.
- Request facts when any property changes; ignore a response that arrives after a newer request
  (no flicker of the previous model's facts).
- Render each fact as one row: label (through `localize.string()`), then value.
- Show `detail` as a tooltip/popover on the row, localized; no tooltip affordance when `detail` is empty.
- Show `Warning` facts with a warning style (`uui-tag color="warning"` or the UUI warning text
  color) and list them first.
- Show at most 6 facts, in the order the server sent them.
- Show `url` as a "Learn more" link (`#uaiModelFacts_learnMore`) only for absolute http/https,
  opening in a new tab.
- Render all text with Lit bindings, never `unsafeHTML`.

### Profile Settings view change

In `profile-details-workspace-view.element.ts`, inside the Model field's `slot="editor"`, render
`<uai-model-facts>` below the `uui-select`, bound to the current connection, capability and
selected model. Changing connection clears the model, so the facts clear with it.

### The five spots

1. **First run:** no model selected → nothing renders. No placeholder text; the field's own
   description already says what to do.
2. **The mistake:** no applicable user error. A failed facts request renders nothing and logs to
   the console. It never shows an error under the Model field, because facts are optional and a
   red message would suggest the model choice is wrong.
3. **The wait:** while facts load for a newly selected model, show a compact `uui-loader-bar`-style
   indicator in the facts area only (the existing model loader is for the list, not facts).
4. **The finish:** facts appearing under the field is the confirmation. No notification.
5. **The return:** deliberate skip. Facts are derived from the saved model, so reopening the
   profile shows them again. No view state to remember in slice 1.

### Localization (`en.ts`)

New `uaiModelFacts` area: `contextWindow`, `contextWindowShort`, `price`, `priceShort`,
`priceDetail`, `learnMore`.

> ASSUMPTION: Price wording calls it the provider's list price and "may be out of date", so it
> can't be read as a quote. Flag CO2e/price wording for review before any public docs (as with
> Carbon's open wording review).
