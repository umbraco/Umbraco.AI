# Build Log

- **09-10-2026 T1** `42a9cca7`: Metadata keys, readers, writer, AIModelPricing. 1491/1491 unit tests
  (reviewer rerun). One fix round: ReadDecimal allowed thousands separators ("3,5" read as 35).
  Smoke: no entry point yet (readers first used by T4/T5); covered by the T7 wire check.
- **09-10-2026 T2** `d3b43999`: Public contract, ordered collection, builder extension, options.
  1492/1492 (reviewer rerun). Passed first review. Smoke: collection resolves from a real
  UmbracoBuilder in the spec; full DI path covered by T7.
- **09-10-2026 T3** `ba3dd4e1`: IAIModelFactService (cache, timeout, ordering, URL sanitising).
  1513/1513 (reviewer rerun, service specs stable over repeated runs). One fix round: a provider
  ignoring the token wasn't bounded (now `WaitAsync`), and post-processing (null facts, cache
  insert) could fail the whole request. Smoke: registration verified; live path in T7.
- **09-10-2026 T4** `a80ac92b`: AIMetadataModelFactProvider, registered first. 1527/1527 (reviewer
  rerun). Passed first review. Also tidied T3's registration test (empty Dispose, header wording).
  Smoke: real UmbracoBuilder resolves it first in the collection; live path in T7.
- **09-10-2026 T5** `ebcd62f9`: OpenRouter writes context window + price metadata. 13/13 OpenRouter
  tests (reviewer rerun; root slnx discovers them). One fix round: a single odd JSON value could
  fail the whole model list; fields now read as `JsonElement?` and parsed leniently.
  Live check (`GET /api/v1/models`, 469 models): `context_length` always an int 4,095–2,000,000;
  `pricing.prompt`/`completion` always plain decimal strings; 19 models "0"/"0" (free); 7 routers
  "-1"/"-1"; no mixed zero/non-zero; no unparseable values. Smoke: live in T7.
- **09-10-2026 T6** `b1219ccd`: ModelFactsConnectionController + response models + map definition.
  1556/1556 (reviewer rerun). One fix round: a provider timeout (TaskCanceledException) escaped as
  500; the mapping-only record was public. Smoke: live request in T7.
- **09-10-2026 T7** `301620e4`: Wire + live check, regenerated core client (`ConnectionsService.getModelFacts`).
  Live on the worktree demo site (OpenRouter connection `openrouter-facts`, dummy key; listing
  is public): `?capability=Chat&modelId=anthropic/claude-sonnet-4` → `core.contextWindow` "200,000"
  + `core.price` "$3.00 / $15.00"; full list 469 items (443 priced); `:free` model → context
  window only; missing capability → 400, `Bogus` → 400, unknown connection → 404, no auth → 401.
  Endpoint in `/umbraco/openapi/ai-management.json`. Follow-up in the same task: `capability`
  marked `[Required]` so OpenAPI/client match the SPEC. 1556/1556, build:core green.
  Env: worktree inherited wdp.port 44355 (taken by another site); set to 44399.
- **09-10-2026 T8** `f34787a2`: Internal model-facts repository + data source, types, mapper, en.ts
  `uaiModelFacts` area. build:core + test:core green (reviewer rerun). Passed first review; nothing
  reaches the public rollup. Smoke: covered by T10's live UI check.
- **09-10-2026 T9** `3fddaa34`: `<uai-model-facts>` element + display logic; 38 pending FE specs now
  running (62/62 test:core, reviewer rerun). One fix round: a failed request raised a backoffice
  error toast (`tryExecute` now `disableNotifications`) and the error wasn't logged. Detail popover
  uses `uui-button` + `popovertarget` like the create-collection actions. Smoke: T10.
