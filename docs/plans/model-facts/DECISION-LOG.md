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
- **09-10-2026** (T2) Keep `IAIModelFactProvider` as an interface (matches `IAIFileProcessingHandler`;
  later members ship as default interface methods). New members on `AIModelFact`/`AIModelFactContext`
  must be optional `init` properties, never `required`. Options bound at `Umbraco:AI:ModelFacts`
  in `AddUmbracoAICore` beside sibling options.
- **09-10-2026** (T2) T3 must call `builder.AIModelFactProviders()` in core so the collection is
  registered even before T4 appends the built-in provider (otherwise the service fails to resolve).
- **09-10-2026** (T3) Provider failures and timeouts are not cached (retried next request); empty
  results are. Duplicate requested ids collapse to the first. A provider that ignores the token
  is abandoned at the timeout (its task keeps running; acceptable for a misbehaving provider).
  Urls are stored normalised (`AbsoluteUri`). Fact providers resolve as singletons, so they can't
  inject scoped services directly; note this in the public docs (T11).
- **09-10-2026** (T4) A provider that writes a zero price gets a "$0.00" fact (honest claim of
  free); only OpenRouter's writer drops zero. Prices under half a cent per 1M show as $0.00 (F2);
  revisit in slice 2. Public docs must tell packages to `[ComposeAfter(typeof(UmbracoAIComposer))]`
  so the built-in provider stays first.
- **09-10-2026** (T5) OpenRouter drops a price only when both input and output are zero (or
  either is invalid/negative/over 1,000,000 per token). A mixed free-input/paid-output model gets
  a price with $0.00 input (none exist today). Fields are read leniently so bad upstream data
  means "no fact", never a broken model list. The OpenRouter metadata merge lets existing
  (settings-support) entries win, like core's `WithMetadata`.
- **09-10-2026** (T5) OpenRouter now needs a Core newer than the current floor `[18.5.2, …)`.
  Raise the range at release (T11 note).
- **09-10-2026** (T6) Capability is validated before the connection lookup (no capability +
  unknown connection = 400). Numeric capability values ("1") are rejected as 400, stricter than the
  sibling `/models` endpoint; the backoffice always sends names. A disabled experimental
  capability returns 200 with no items, matching `CapabilitiesConnectionController`. Listed models
  are deduped by id (ordinal). `Tone` serialises as a string via the API's enum converter.
