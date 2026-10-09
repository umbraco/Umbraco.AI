# Spec

> **Status:** Archived 24-09-2026. Completed spike (all 12 tasks done, never merged). Superseded by the full feature plan in `docs/plans/decision-capability-release/`, which builds on this spike's Core code.

## Management API surface

None. Per the brief's non-goals, this spike does no backoffice UI or
Management API work. No new controller, no new route.

## Frontend components

None, for the same reason — no Profile/Connection editor changes.

## Core capability contract

This is a code-only spike, so the only "externally observable contract" is
the C# surface itself — what `umb-plan`/`umb-build-loop` should treat as
testable, the same way `AIExperimentalFeaturesTests.cs` already tests the
`ImageGeneration` flag today.

### 1. `AICapability.Decision` exists and is gated like every other
   experimental capability

- `AICapability.Decision` is a new enum member (value `8`); `Moderation`
  (`3`) is untouched.
- `AIExperimentalOptions.Decision` defaults to `false`.
- `AIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision)`
  returns `false` when `AIExperimentalOptions.Decision` is `false` (default),
  `true` when it's `true` — same shape as the existing
  `IsCapabilityEnabled_ImageGeneration_*` tests.
- With the flag off (default):
  - `AIConnectionService.GetCapabilitiesAsync` (or equivalent) for a
    connection whose provider declares a `Decision` capability does **not**
    include `AICapability.Decision` in the result.
  - `AIConnectionService.GetConnectionsByCapabilityAsync(AICapability.Decision, ...)`
    returns an empty list, even if a connection's provider declares the
    capability.
  - `AIProfileService`'s create/update path throws `InvalidOperationException`
    for a profile whose `Capability` is `Decision`, with a message telling
    the caller to enable `Umbraco:AI:Experimental:Decision`.
- With the flag on: all three behave as they do for any non-experimental
  capability today (visible, connectable, profile-creatable).

### 2. `IAIDecisionClient.AskAsync` — per-`Kind` contract

Given a resolved `IAIDecisionClient` (from a spike provider's
`AIDecisionCapabilityBase`), `AskAsync(AIDecisionQuestion, ...)` must
guarantee, for each `AIDecisionQuestion.Kind`:

| `Kind`   | Required on the question                      | Required on the response |
|----------|------------------------------------------------|---------------------------|
| `Binary` | `Prompt` non-empty                              | `BinaryAnswer` non-null; `SelectedChoice`/`Score` null |
| `Choice` | `Prompt` non-empty; `Choices` non-null, ≥ 2 entries | `SelectedChoice` non-null and one of the supplied `Choices`; `BinaryAnswer`/`Score` null |
| `Score`  | `Prompt` non-empty; `ScoreRange` optional        | `Score` non-null (and within `ScoreRange` when the question supplied one); `BinaryAnswer`/`SelectedChoice` null |

In every case: `AIDecisionResponse.Kind` echoes the question's `Kind`, and
`Confidence` is present and within `[0.0, 1.0]`.

A `Choice` question with fewer than 2 `Choices` is a caller error —
`AskAsync` throws `ArgumentException` before any request reaches a provider
(mirrors existing argument validation style elsewhere in the codebase, e.g.
`ArgumentNullException.ThrowIfNull(settings)` in `AICapabilityBase`).

### 3. Per-model settings declaration and tracking wrap every client, same as other capabilities

- `IAIDecisionCapability.CreateClientAsync` returns a client wrapped by a
  `DeclaredSettingsDecisionClient`-equivalent, enforcing
  `GetSettingsSupport(modelId)` the same way `DeclaredSettingsChatClient` /
  `DeclaredSettingsSpeechToTextClient` do today.
- A request through `IAIDecisionService` records usage/audit information via
  `AITrackingDecisionMiddleware`, observable the same way existing capability
  usage is (ties to the capability-recording correctness work in
  [[project_operation_tracker]] — a new capability must not reintroduce the
  null-usage / dropped-LogKeys class of bug already fixed there).

### 4. Spike provider (next phase, not built here)

Not a contract on the general capability — a note for whoever builds the
throwaway provider: it should exercise `Kind.Binary` at minimum (Jev's
simplest primitive, e.g. "is this text spam?") against Jev's real HTTP API,
enough to prove the round trip end-to-end through
`IAIDecisionService.AskAsync` → provider → Jev → typed `AIDecisionResponse`.
`Choice`/`Score` exercised only if time allows — the capability shape
supports them either way per section 2 above.
