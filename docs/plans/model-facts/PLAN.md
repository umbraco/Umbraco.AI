# Plan

Task checklist for `umb-build-loop`. Slice 1 only, `v18/dev` only (no v17 backport, per the
brief). Work happens in a worktree branched from `origin/v18/dev`; push trunk before
`EnterWorktree` (see `umb-build-loop-gotchas`).

Paths are relative to the repo root. `Core` = `Umbraco.AI/src/Umbraco.AI.Core`,
`Web` = `Umbraco.AI/src/Umbraco.AI.Web`, `FE` = `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src`,
`OR` = `Umbraco.AI.OpenRouter`.

- [ ] **T1**: story MF-2. Add the standard Metadata keys `model.contextWindow`,
  `pricing.inputPerMillionTokens`, `pricing.outputPerMillionTokens`, `pricing.currency` to
  `Core/Models/AIModelMetadataKeys.cs`; public record `AIModelPricing`; readers
  `GetContextWindow()` / `GetPricing()` in `Core/Extensions/AIModelDescriptorExtensions.cs`; a writer
  helper that formats in invariant culture (mirroring `AIModelSettingsSupport.ToMetadata()`).
  Specs: `MF2_*` in `Umbraco.AI.Tests.Unit`.
  depends-on: none. parallel-group: A

- [ ] **T2**: story MF-1 (AC1). Add the public contract in `Core/ModelFacts/`:
  `IAIModelFactProvider`, `AIModelFact`, `AIModelFactContext`, `AIModelFactTone`,
  `AIModelFactProviderCollection` + `AIModelFactProviderCollectionBuilder` (ordered), the
  `AIModelFactProviders()` builder extension in a new
  `Core/Configuration/UmbracoBuilderExtensions.ModelFacts.cs`, and `AIModelFactOptions`
  (`ProviderTimeout`, default 2 s, bound to `Umbraco:AI:ModelFacts`). Classes with
  `required`/`init` properties, not positional records (ARCHITECTURE decision 4).
  depends-on: none. parallel-group: A

- [ ] **T3**: story MF-1 (AC3–AC14). Add `IAIModelFactService` + internal `AIModelFactService`:
  per-model runtime caching keyed by provider type + connection + capability + model (empty
  results cached too), only uncached models passed to each provider, providers run concurrently
  under the timeout, failures logged at Warning and skipped, unrequested model ids dropped,
  non-http(s) `Url` nulled, ordering (Warning first, then registration order). Register in
  `AddUmbracoAICore`.
  depends-on: T2. parallel-group: B

- [ ] **T4**: story MF-3, MF-1 (AC2). Add internal `AIMetadataModelFactProvider` (reads T1's
  readers, `CacheDuration = TimeSpan.Zero`, never throws) and register it first via
  `builder.AIModelFactProviders().Append<AIMetadataModelFactProvider>()` in `AddUmbracoAICore`.
  depends-on: T1, T2. parallel-group: B

- [ ] **T5**: story MF-4. First, `curl https://openrouter.ai/api/v1/models` (public, no key) and
  confirm the `context_length` and `pricing.prompt` / `pricing.completion` shapes; record what
  was found in `BUILD-LOG.md`. Then extend `OR/src/.../OpenRouterModelsResponse.cs` and write
  the T1 keys in `OpenRouterChatCapability.GetModelsAsync` alongside the existing settings-support
  metadata. Create `OR/tests/Umbraco.AI.OpenRouter.Tests.Unit` (mirroring
  `Umbraco.AI.Anthropic.Tests.Unit`), add it to `OR/Umbraco.AI.OpenRouter.slnx` **and** the root
  `Umbraco.AI.slnx` (CI only runs what's listed there).
  depends-on: T1. parallel-group: B

- [ ] **T6**: story MF-5 (AC1–AC12). Add `ModelFactsConnectionController`
  (`[HttpGet("{connectionIdOrAlias}/model-facts")]`) beside `ModelsConnectionController`, plus
  `ModelFactsResponseModel` / `ModelFactsItemResponseModel` / `ModelFactResponseModel` and their
  `IMapDefinition`. Lists the connection's models for the capability (logging, not swallowing,
  listing failures), filters to `modelId` if given, calls `IAIModelFactService`. Specs as
  controller unit tests in `Umbraco.AI.Tests.Unit/Api/Management/`.
  depends-on: T3. parallel-group: C

- [ ] **T7**: wire: model facts into the Management API. On the demo site, with a real
  OpenRouter connection: `GET /umbraco/ai/management/api/v1/connections/{alias}/model-facts?capability=Chat&modelId=<a real model>`
  returns `core.contextWindow` and `core.price` facts; the endpoint appears in the Swagger doc;
  run `npm run generate-client` so the core client has `getModelFacts`. Commit the regenerated
  client.
  depends-on: T4, T5, T6.

- [ ] **T8**: story MF-6 (data). Add `FE/connection/repository/model-facts/` (repository + server
  data source, mirroring `repository/models/`), the `UaiModelFactModel` / `UaiModelFactsModel`
  types and mapper, and the `uaiModelFacts` area in `en.ts` (`contextWindow`,
  `contextWindowShort`, `price`, `priceShort`, `priceDetail`, `learnMore`). Internal only; no
  `exports.ts` change.
  depends-on: T7. parallel-group: D

- [ ] **T9**: story MF-6 (AC3–AC11). Add the internal `<uai-model-facts>` element in
  `FE/profile/components/model-facts/`, exported via `profile/components/index.ts` (internal
  barrel). Pure display logic (ordering check, 6-fact cap, safe-url check) in a separate
  module with a vitest spec, like `declared-settings.test.ts`. Lit text bindings only.
  depends-on: T8.

- [ ] **T10**: wire: `<uai-model-facts>` into the profile Settings view. Render it in the Model
  field's `slot="editor"` in `profile-details-workspace-view.element.ts`, bound to connection,
  capability and model. Verify on the demo site (Playwright): select an OpenRouter model → facts
  appear; switch model → they change; switch connection → they clear; a non-OpenRouter
  connection with no facts → nothing renders. `npm run build:core` green.
  depends-on: T9.

- [ ] **T11**: docs housekeeping. If PR #518 has merged, set `docs/ideas/model-facts.md` status
  to "Promoted to `docs/plans/model-facts/`". Note in `BUILD-LOG.md` the follow-ups: public docs
  PR for the extension point (umbraco-docs-pr), OpenRouter's raised Core floor at release, and
  slice 2.
  depends-on: T10.
