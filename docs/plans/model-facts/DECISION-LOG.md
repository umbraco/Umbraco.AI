# Decision Log

- **08-10-2026** (umb-explore) Packages supply data, not UI. A free-form extension slot under the
  Model field is rejected (inconsistent layout, a11y per package, no reuse, one-consumer contract).
- **08-10-2026** (umb-explore) Profile pickers (agents, prompts, settings) are out of scope. Users
  choose profiles there, not models.
- **08-10-2026** (umb-explore) Core only supplies facts where a provider API already returns them
  (OpenRouter context length + pricing; Anthropic max tokens). No hand-maintained fact tables.
- **08-10-2026** (umb-explore) The "second consumer" gate is met by core's own OpenRouter facts, so
  planning goes ahead now rather than waiting.
- **08-10-2026** (umb-explore) v18 only first. The new public contract settles on one line before a
  v17 backport is considered.
- **08-10-2026** (umb-explore) First slice is the contract, OpenRouter facts, and the selected
  model's facts under the Model field. The sortable table picker is a second slice.
- **08-10-2026** Metadata vs facts: two layers. `AIModelDescriptor.Metadata` stays the provider-owned,
  machine-readable channel (new governed keys such as context window and price, each with a
  reader). Facts are the display layer from any source. Core ships one built-in fact provider that
  turns well-known Metadata keys into facts; packages add their own fact providers and never write
  Metadata. Rejected: facts inside Metadata (packages can't write it; strings can't carry
  label/tone/detail/sort) and facts replacing Metadata (it drives settings pruning and needs
  machine values).
- **08-10-2026** (umb-design) Facts endpoint is `GET connections/{id}/model-facts?capability&modelId`,
  not the idea's `POST /model-facts`. The server needs full descriptors (Metadata) for the built-in
  provider, and connection scope gives fact providers the connection id for free. Rejected: idea's
  POST (no descriptors); facts inside `GET …/models` (slow providers would delay the list).
- **08-10-2026** (umb-design) Fact providers register through an ordered collection builder;
  registration order is display order. Rejected: attribute discovery (no ordering control).
- **08-10-2026** (umb-design) `AIModelFact` and `AIModelFactContext` are classes with
  `required`/`init` properties so fields can be added later without breaking packages.
- **08-10-2026** (umb-design) Slice 1 keeps the `uui-select` and adds facts under it. The picker
  modal is slice 2 and needs a table-in-modal precedent from CMS core first.
- **08-10-2026** (umb-design) Labels may be `#keys`, localized by `localize.string()` like existing
  server-supplied field labels. No server-side culture plumbing in slice 1.
- **09-10-2026** (umb-design) Matt approved the design, including the connection-scoped endpoint over the idea's POST.
- **09-10-2026** (umb-plan) Endpoint wiring (T7) waits for the built-in provider and OpenRouter
  (T4, T5), so the live check proves real facts end to end, not an empty list.
- **09-10-2026** (umb-plan) Frontend starts only after the regenerated client is committed (T7),
  so the repository is written against the real generated types.
- **09-10-2026** (umb-plan) OpenRouter gains its first test project; it must be added to the root
  `Umbraco.AI.slnx` or CI silently won't run it.
- **09-10-2026** (umb-plan) Pending C# specs are wrapped in `#if MODEL_FACTS_PENDING` because the
  types they use don't exist yet, and a non-compiling spec would break every build. The task that
  makes a file pass removes its guard in the same commit. Specs were written in the worktree, not
  on trunk, so they travel with the feature branch.
- **09-10-2026** (T1) Readers are strict: context window must be a positive int; pricing needs all
  three keys, invariant decimal without thousands separators, >= 0. Zero price is valid in the
  reader; turning "0" into "no fact" is the writer's (OpenRouter's) job. ReadInt now pins
  invariant culture (also used by image.maxEdge; no behaviour change).
