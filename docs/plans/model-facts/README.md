# Model Facts

Read these in order:

1. [x] **BRIEF.md** — Editors pick a model from a bare dropdown; let core and packages attach short facts (price, context window, CO2e, retirement) shown wherever a model is chosen.
2. [x] **ARCHITECTURE.md** — An ordered collection of fact providers in Core, a connection-scoped facts endpoint, and a built-in provider that turns new Metadata keys into facts.
3. [x] **SPEC.md** — `GET connections/{id}/model-facts`, OpenRouter context window + price, and a `<uai-model-facts>` list under the profile Model field.
4. [x] **STORIES.md** — 6 stories for slice 1 (contract, Metadata keys, built-in facts, OpenRouter, endpoint, UI) plus a slice 2 placeholder.
5. [x] **PLAN.md** — 11 tasks; first parallel group is the Metadata keys (T1) and the public contract (T2).
6. [x] **BUILD-LOG.md** — 11 tasks done (10 code commits plus the pending-specs commit); 5 tasks needed a fix round after review. All suites green, live-checked in the backoffice.

See `DECISION-LOG.md` for why things changed along the way.

Source idea: `docs/ideas/model-facts.md` (PR #518).
