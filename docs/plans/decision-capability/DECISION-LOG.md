# Decision Log

## 2026-09-23 — Scoped as a spike, not a feature

Decided during `umb-explore`:

- This is an internal architecture spike, not an editor- or developer-facing
  feature. No BRIEF success criteria assume a shippable product.
- "Decision" generalizes the already-reserved `AICapability.Moderation`
  slot rather than adding a second, overlapping capability kind — but the
  actual enum/naming mechanics are left open for `umb-design`, since
  `Moderation` already shipped as public API and can't just be renamed.
- Success is a working throwaway code spike (round-trip a real typed
  decision through Jev), not just a written verdict — the user overrode the
  initially-recommended "no code" scope once asked directly about success
  criteria.
- Any client abstraction sketched for this must sit behind an experimental
  feature flag, following the existing `Umbraco:AI:Experimental:ImageGeneration`
  precedent, because M.E.AI has no upstream abstraction for typed-decision
  models to wrap yet.
- Explicitly not in scope: a real provider package, backoffice UI, or
  resolving the Jev community .NET SDK's .NET 11/C# 15 vs Umbraco AI's
  net10.0 mismatch.
- Kill criterion: if representing three primitive answer types (yes/no,
  choice, score) turns out to need substantial new plumbing (new middleware
  shape, new Profile/Connection concepts), that's a signal to wait rather
  than build now.

## 2026-09-23 — Design: new enum value, proprietary client, full plumbing

Decided during `umb-design`:

- Add `AICapability.Decision = 8`, leaving `Moderation = 3` untouched and
  unused — renaming it would break public API (shipped 2025-11-23, still
  live at package version 18.3.5). Value `8`, not `6`/`7`, because those are
  already earmarked in a code comment for future `TextToSpeech`/
  `SpeechToSpeech`. Whether `Moderation` is ever built as something distinct
  from `Decision` is left open, not decided here.
- New client abstraction `IAIDecisionClient` is genuinely Umbraco-AI-owned
  (no M.E.AI type exists to wrap) — the first true break from the
  "thin wrapper, no proprietary abstractions" rule. Gated the same way
  `ImageGeneration` was: `[Experimental("UMBRACOAI_DECISION")]` at compile
  time, `AIExperimentalOptions.Decision` (default off) at runtime.
- One flat `AIDecisionResponse` (a `Kind` discriminant + nullable per-kind
  fields) covering all three of Jev's answer shapes (binary/choice/score)
  from the start, rather than mirroring Jev's own polymorphic C# 15 record
  hierarchy or shipping binary-only.
- Full capability depth mirrored from `SpeechToText/` (client, factory,
  service, middleware collection, tracking/telemetry) — a shortcut
  wouldn't actually test the brief's central question.
- The eventual spike provider calls Jev's HTTP API directly via
  `HttpClient`, not the community `RavenValentin/TypeSafe.Jev` SDK — avoids
  both the .NET 11/C# 15 mismatch and depending on an unofficial,
  self-described "vibe-coded" package, even for throwaway code.

## 2026-09-23 — Plan: T9 runs independently of the main capability chain

Decided during `umb-plan`:

- The three experimental-gating enforcement points (hidden capability
  listing, empty connections-by-capability, rejected profile creation) are
  already generic in `AIConnectionService`/`AIProfileService` — they key off
  `IAICapability.Kind`, not a hardcoded switch per capability. So T9 (DC-1's
  wire proof) only needs a minimal test-double capability with
  `Kind = AICapability.Decision`, not the real `AIDecisionCapabilityBase`
  from T5. That lets T9 run as its own parallel branch (group C) instead of
  blocking on the T5→T8 chain, and it exists specifically to *prove* the
  generic wiring holds for the new value rather than assume it from reading
  the code.
- T10 (the spike provider) is sequenced after the full T5–T8 chain rather
  than after just T5, even though writing the capability class itself only
  needs the base class — kept simple deliberately, per the brief's own
  instruction to keep this spike's breakdown small rather than
  over-fragmenting task boundaries.

## 2026-09-23 — Specs: two gaps found while writing them, resolved without reopening design

Discovered/decided while running `bdd-specs`:

- `AIDecisionResponse` needs named static factories (`ForBinary`, `ForChoice`,
  `ForScore`) to make DC-2's per-`Kind` shape guarantees unit-testable in
  isolation, rather than leaving every provider to hand-build a correctly
  -shaped response. This is a constructor convenience on the same flat type
  `ARCHITECTURE.md` already decided on, not a design change — folded into
  T2, not a new task.
- DC-3 AC1 (declared-settings enforcement) has nothing to strip today:
  unlike `ChatOptions`/`EmbeddingGenerationOptions`/`SpeechToTextOptions`/
  `ImageGenerationOptions`, `AIDecisionOptions` has no per-request knob
  besides `ModelId` — Jev exposes no sampling-style settings. The generated
  spec (`Providers/DeclaredSettingsEnforcementTests.cs`) only proves the
  `DeclaredSettingsDecisionClient` wrapper exists and passes options through
  unchanged; a real "declared unsupported X is removed" case waits until/if
  `AIDecisionOptions` grows a strippable field.
- DC-4 AC1's genuinely-live Jev round trip (real network, real credentials)
  stays a manual verification step during T11, matching this repo's existing
  practice for real-provider-key checks (e.g. the Prompt wand's live OpenAI
  test). The generated spec (`Decision/JevSpikeProviderTests.cs`) instead
  fakes only the `HttpMessageHandler`, so it still exercises the real
  `HttpClient` pipeline and the real spike client class — the closest
  automatable proxy for "real entry point" without spending a real API call
  in every test run.

## 2026-09-23 — T2 review: `AIDecisionResponse.Usage` reuses M.E.AI's `UsageDetails`

Found/decided during T2's review:

- `AIDecisionResponse.Usage` is `Microsoft.Extensions.AI.UsageDetails`, not a
  fresh Umbraco-owned `AIDecisionUsage` type. The tracking pipeline this
  capability must plug into (`AIOperationScope.CompleteAsync`,
  `AIOperationTracker.RecordUsageAsync`, `AIUsageRecordResult.Usage`,
  `AIAuditResponse.Usage`, `AITrackedOperationResult.Usage`) all take
  `UsageDetails` already, and `ImageGeneration.AITrackedImageResult.Usage`
  sets the precedent of using `UsageDetails` even on an Umbraco-owned
  wrapper type. A custom type here would have forced T7's
  `AITrackingDecisionClient` to write an unplanned converter, and would have
  silently dropped `CachedInputTokenCount`/`ReasoningTokenCount`/
  `AdditionalCounts` that existing usage-recording/dashboard code reads via
  `UsageDetailsExtensions`. "`IAIDecisionClient` isn't an M.E.AI wrapper" is
  about the client contract, not every field's data type — `AIDecisionUsage`
  is deleted.

## 2026-09-23 — T8 review: no Core-only `IdOrAlias` type; Decision follows Chat/SpeechToText's actual pattern

Found/decided during T8's review:

- `STORIES.md`'s DC-3 AC3 assumed `IAIDecisionService.AskAsync` would take a
  single "ID or alias" parameter, and T8's first pass built a Core-only
  `Decision.IdOrAlias` type for it (since `Umbraco.AI.Core` can't reference
  `Umbraco.AI.Web.Api.Common.Models.IdOrAlias`, which carries ASP.NET
  model-binding concerns anyway). That assumption didn't hold once checked
  against how `IAIChatService`/`IAISpeechToTextService` actually resolve
  profiles: neither takes an `IdOrAlias`-shaped parameter anywhere in Core.
  Their non-obsolete surface is the builder pattern
  (`Action<AIChatBuilder>`/`Action<AISpeechToTextBuilder>`), whose builders
  expose two distinct, separately-typed methods -- `WithProfile(Guid)` and
  `WithProfile(string)` -- never one type that could be either. Their
  obsolete legacy overloads mirror that split: a `Guid profileId` overload
  and a separate one for the default-profile case, never a combined
  ID-or-alias parameter.
- Deleted `Decision/IdOrAlias.cs` entirely. `IAIDecisionService.AskAsync` is
  now three overloads: `AskAsync(Guid profileId, ...)`,
  `AskAsync(string profileAlias, ...)`, and the builder-based
  `AskAsync(Action<AIDecisionBuilder> configure, ...)` (added for Finding 2's
  inline execution path). The two typed overloads delegate to the
  builder-based one via `AIDecisionBuilder.WithProfile(Guid)`/
  `WithProfile(string)` -- mirroring exactly how Chat's/SpeechToText's
  obsolete profile-id overloads delegate to their own builder-based main
  path -- collapsing what would otherwise be two separate implementations
  (direct profile resolution vs. builder resolution) into one real entry
  point. Neither typed overload is marked `[Obsolete]`: Decision has no
  shipped legacy surface to preserve, unlike Chat/SpeechToText.
- `AIDecisionBuilder` (T7) gained one more method on top of its original
  surface -- `WithDecisionOptions(AIDecisionOptions)` -- so the two typed
  overloads' `AIDecisionOptions? options` parameter has somewhere to go once
  routed through the builder. This is exactly the kind of addition T7's own
  doc comment anticipated ("T8 is expected to add whatever further
  configuration surface it needs on top of this rather than replace it").
- `ScopedInlineDecisionClient` (T7) is consumed on `AIDecisionService`'s
  execute path (`ExecuteDecisionAsync`), not just a "create a client" path --
  a deliberate choice to reuse the wrapper T7 already built rather than
  duplicate Chat's/SpeechToText's inline scope-management code a second time
  inside `AIDecisionService`. This makes Decision the first capability where
  a `Scoped*Inline*` wrapper sits on the execute path, which mattered for the
  feature-metadata decision below.
- The two typed overloads (`AskAsync(Guid, ...)`/`AskAsync(string, ...)`)
  delegate to the builder-based main path and therefore publish notifications
  via that delegation, exactly matching how Chat's/SpeechToText's obsolete
  profile-id overloads behave -- confirmed correct during T8's review, not a
  deviation needing its own fix.

## 2026-09-23 — T8 review round 2: feature-metadata rule follows the execute-path precedent, not the create-client-path one

Found/decided during T8's second review:

- Because `ScopedInlineDecisionClient` sits on the execute path (see above),
  its feature-metadata decision must follow the rule Chat's/SpeechToText's
  own execute paths use -- `setFeatureMetadata: !builder.IsPassThrough` --
  not the rule their `Scoped*Inline*` wrappers use on their create-client
  path (`setFeatureMetadata: !scopeExisted`). Those two rules disagree in
  two cases, not one: (1) a pass-through call made with no parent scope --
  the old rule wrongly stamped metadata, the new rule correctly doesn't --
  and (2) a normal, non-pass-through call made inside an already-existing
  parent scope (e.g. an agent run) -- the old rule wrongly skipped metadata,
  the new rule correctly stamps it. The first review round got case (1)
  backwards: a normal call inside an existing scope skipped metadata, while
  a pass-through call with no parent scope stamped it -- the opposite of
  `AsPassThrough()`'s own doc comment. Fixed by reading `_builder.IsPassThrough`
  directly (the class already holds the builder), no new parameter needed.
