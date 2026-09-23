# Plan

Task checklist for `umb-build-loop`. All work lands on `v18/dev` only (or a
worktree branched from it) — no backport, per the brief. Every task is
Umbraco.AI.Core-only C# except T10/T11, which add one disposable spike
provider.

- [x] **T1** — story: DC-1. Add `AICapability.Decision = 8` to
  `Umbraco.AI.Core/Models/AICapability.cs`; leave `Moderation = 3` untouched;
  update the trailing `// Future: TextToSpeech = 6, SpeechToSpeech = 7`
  comment to stay accurate now that `8` is taken.
  depends-on: none. parallel-group: A

- [x] **T2** — story: DC-2. Create `Umbraco.AI.Core/Decision/` and add the
  core types: `AIDecisionKind`, `AIDecisionQuestion`, `AIDecisionOptions`,
  `AIDecisionResponse` (`Usage` is M.E.AI `UsageDetails`, see DECISION-LOG
  2026-09-23 — not a fresh `AIDecisionUsage` type), `IAIDecisionClient`, and
  `AIDecisionDiagnostics` (`DiagnosticId = "UMBRACOAI_DECISION"`), per
  `ARCHITECTURE.md`'s type sketch. No validation logic yet — plain types.
  depends-on: none. parallel-group: A

- [x] **T3** — story: DC-1 (AC2, AC3, AC7). Add
  `AIExperimentalOptions.Decision` (bool, default `false`) and the
  `AICapability.Decision => _options.CurrentValue.Decision` case in
  `AIExperimentalFeatures.IsCapabilityEnabled`'s switch
  (`Umbraco.AI.Core/Settings/`). Add/extend
  `AIExperimentalFeaturesTests.cs` with the `Decision` equivalents of the
  existing `ImageGeneration` cases.
  depends-on: T1. parallel-group: B

- [x] **T4** — story: DC-2 (AC5, AC6). Add the validating wrapper that
  enforces `AskAsync`'s sad-path rules (empty/whitespace `Prompt` → always
  `ArgumentException`; `Kind = Choice` with fewer than 2 `Choices` →
  `ArgumentException`) in front of every `IAIDecisionClient`, in the same
  spot `AIErrorClassifyingSpeechToTextClient` wraps for SpeechToText.
  depends-on: T2. parallel-group: B

- [x] **T5** — story: DC-2 (AC1–AC4), DC-3. Add `IAIDecisionCapability` and
  `AIDecisionCapabilityBase` / `AIDecisionCapabilityBase<TSettings>` /
  `AIDecisionCapabilityBase<TSettings, TCapabilitySettings>` to
  `Umbraco.AI.Core/Providers/IAICapability.cs`, mirroring
  `IAISpeechToTextCapability`/`AISpeechToTextCapabilityBase<...>` exactly.
  `Kind => AICapability.Decision`. Mark the whole surface
  `[Experimental(AIDecisionDiagnostics.DiagnosticId)]`.
  depends-on: T1, T2.

- [x] **T6** — story: DC-3 (AC1). Add `DeclaredSettingsDecisionClient` and
  `CapabilitySettingsDecisionClient<TCapabilitySettings>` to
  `Umbraco.AI.Core/Providers/`, mirroring
  `DeclaredSettingsSpeechToTextClient`/`CapabilitySettingsSpeechToTextClient`.
  depends-on: T5. (Built together with T5 — inseparable, see BUILD-LOG.)

- [ ] **T7** — story: DC-3 (AC2). Add the remaining `Decision/` feature-folder
  plumbing, file-for-file from `SpeechToText/`: `IAIDecisionClientFactory` +
  `AIDecisionClientFactory`, `IAIDecisionMiddleware` +
  `AIDecisionMiddlewareCollection(Builder)`, `AITrackingDecisionClient` +
  `AITrackingDecisionMiddleware`, `AIOpenTelemetryDecisionMiddleware`,
  `AIErrorClassifyingDecisionClient`,
  `AIDecisionExecutingNotification`/`AIDecisionExecutedNotification`,
  `ScopedProfileDecisionClient`/`ScopedInlineDecisionClient`.
  depends-on: T5, T6.

  **Explicit acceptance criteria added after T4's review** (T4 built
  `ValidatingDecisionClient` — DC-2 AC5/AC6's sad-path enforcer — but it's
  unwired until this task; these two points must hold once T7 is done):
  - `AIDecisionClientFactory` must wrap every provider's raw `IAIDecisionClient`
    in `ValidatingDecisionClient`.
  - **Wrapping order matters and must NOT mirror "innermost" literally
    from the SpeechToText precedent**: `ValidatingDecisionClient` must sit
    **outside** `AIErrorClassifyingDecisionClient` (and outside the tracking
    middleware / executing-notification layer too) — not inside it. If a
    caller's `ArgumentException` (invalid question shape) passes through
    the error classifier first, it gets caught and rethrown as
    `AIProviderException`, which breaks DC-2 AC5/AC6's contract (a caller
    error must be rejected as `ArgumentException`, not misreported as a
    provider failure) and would falsely log/audit it as a provider error
    too.

- [ ] **T8** — story: DC-3 (AC3). **Also add one sad-path spec** (not in the
  original story) proving T7's wiring actually holds at the real entry
  point: `IAIDecisionService.AskAsync` called with an invalid question
  (e.g. `Choice` with one entry) throws `ArgumentException`, and the fake
  provider client underneath is never invoked. This is the test that would
  catch T7's ordering mistake described above if it happens anyway.

  Add `IAIDecisionService`/`AIDecisionService`
  (profile-alias resolution via `IdOrAlias`, mirroring
  `IAISpeechToTextService`/`AISpeechToTextService`) and register everything
  from T5–T8 in DI: `AIDecisionBuilder` +
  `UmbracoBuilderExtensions.Collections.cs` entries (the
  `builder.AIDecisionMiddleware()` collection-builder accessor), each also
  marked `[Experimental(AIDecisionDiagnostics.DiagnosticId)]` the same way
  `AIImageGenerationMiddleware()` is today.
  depends-on: T7.

- [ ] **T9** — story: DC-1 (AC4, AC5, AC6) — **wire task**. Using a minimal
  test-double `IAICapability` with `Kind = AICapability.Decision` (does not
  need T5's real base class), write and pass the specs proving
  `AIConnectionService`'s capability listing / `GetConnectionsByCapabilityAsync`
  and `AIProfileService`'s profile-create path already honor the new
  capability correctly through their existing generic
  `IsCapabilityEnabled(cap.Kind)` checks — no production code change
  expected here; this task's job is to prove that, not assume it.
  Acceptance is these specs passing against the real services, not a mocked
  `IAIExperimentalFeatures`.
  depends-on: T1, T3. parallel-group: C (runs alongside T5→T8 and T4)

- [ ] **T10** — story: DC-4 (AC1). Build the disposable spike provider: an
  `[AIProvider]`-attributed class (e.g. `"typesafe-jev-spike"`) whose
  `AIDecisionCapabilityBase`-derived capability returns an
  `IAIDecisionClient` implementation that calls Jev's HTTP API directly via
  `HttpClient` + `System.Text.Json` (no reference to
  `RavenValentin/TypeSafe.Jev`). Implement `Binary` at minimum; `Choice`/
  `Score` only if time allows, per `SPEC.md`.
  depends-on: T8, T4.

- [ ] **T11** — story: DC-4 (AC1) — **wire task**. Register the spike
  provider (Composer, demo site or a dedicated test host — whichever this
  repo's provider-testing convention already uses) with
  `Umbraco:AI:Experimental:Decision` set to `true` and a real Jev API key,
  create a Decision profile against it, and call
  `IAIDecisionService.AskAsync` with a real `Binary` question end-to-end.
  Acceptance is a real network round trip returning a typed
  `AIDecisionResponse` — not a mocked `HttpClient`.
  depends-on: T10, T9.

- [ ] **T12** — story: DC-4 (AC2). With the same spike provider now
  registered from T11, flip `Umbraco:AI:Experimental:Decision` back to its
  default (`false`) and re-run DC-1's AC4–AC6 checks against it specifically
  — confirming the *real* provider, not just a test double, goes fully inert
  when the flag is off.
  depends-on: T11.

## Parallel groups

- **A** (no shared files, no ordering): T1, T2
- **B** (each depends only on its own group-A prerequisite): T3, T4
- **C** (independent side-branch once T1/T3 land): T9 — runs alongside the
  T5→T6→T7→T8 main chain

## First shippable slice

T1 → T2 → T3 → T5 (i.e., DC-1's enum-and-gating plus DC-2's bare types wired
into a capability that exists but declares nothing dangerous) is the
smallest useful checkpoint: it proves the enum/flag mechanics end-to-end
before any of the heavier plumbing (T6–T8) or the real HTTP spike (T10–T12)
gets built.
