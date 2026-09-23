# Architecture

## Extension points

None of Umbraco CMS's usual extension points (property editor, dashboard,
content app, etc.) apply — this is entirely internal to `Umbraco.AI.Core`'s
own provider/capability SDK. The relevant "extension point" is the pattern
every capability already follows there:

```
IAICapability                          (Umbraco.AI.Core/Providers/IAICapability.cs)
  └─ IAI<Foo>Capability                 (per-capability contract)
       └─ AI<Foo>CapabilityBase          (no settings)
       └─ AI<Foo>CapabilityBase<TSettings>
       └─ AI<Foo>CapabilityBase<TSettings, TCapabilitySettings>
```

`Decision` follows this exactly, modelled directly on `IAISpeechToTextCapability`
/ `AISpeechToTextCapabilityBase<...>` (`Umbraco.AI.Core/SpeechToText/`,
`Umbraco.AI.Core/Providers/IAICapability.cs`), since SpeechToText is the most
recently added capability and the cleanest template: client interface → three
capability-base arities → factory → profile-resolving service → middleware
collection → tracking/telemetry middleware. A new `Decision/` feature folder
holds the equivalents, matching the existing `Chat/`, `SpeechToText/`,
`ImageGeneration/` folders (`Umbraco.AI/CLAUDE.md`'s feature-sliced
organization rule: "New capability → create new feature folder").

**Why full depth, not a shortcut:** the brief's kill criterion is
specifically "does representing three primitive answer types need
disproportionate new plumbing." Building the real plumbing (not a
shortcut straight from provider to client) is what actually tests that
question — a hand-wired one-off wouldn't tell us anything about whether this
fits the architecture.

## The client abstraction — proprietary, not an M.E.AI wrapper

Every existing capability's client type is a Microsoft.Extensions.AI type:
`IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`,
`ISpeechToTextClient`, and (experimentally, within M.E.AI itself, `MEAI001`)
`IImageGenerator`. **M.E.AI has no client abstraction for a typed
yes/no/choice/score decision.** This capability introduces
`Umbraco.AI.Core.Decision.IAIDecisionClient` — the first client type in this
codebase that isn't an M.E.AI type Umbraco AI is merely wrapping.

```csharp
namespace Umbraco.AI.Core.Decision;

public interface IAIDecisionClient : IDisposable
{
    Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    // Mirrors IChatClient.GetService — lets tracking/telemetry middleware and
    // callers reach through wrapper layers the same way every other client does.
    object? GetService(Type serviceType, object? serviceKey = null);
}

public enum AIDecisionKind { Binary, Choice, Score }

public sealed class AIDecisionQuestion
{
    public required AIDecisionKind Kind { get; init; }
    public required string Prompt { get; init; }
    /// <summary>Required when <see cref="Kind"/> is <see cref="AIDecisionKind.Choice"/>.</summary>
    public IReadOnlyList<string>? Choices { get; init; }
    /// <summary>Optional bounds when <see cref="Kind"/> is <see cref="AIDecisionKind.Score"/>.</summary>
    public (double Min, double Max)? ScoreRange { get; init; }
}

public sealed class AIDecisionOptions
{
    public string? ModelId { get; init; }
}

public sealed class AIDecisionResponse
{
    public required AIDecisionKind Kind { get; init; }
    public bool? BinaryAnswer { get; init; }      // set iff Kind == Binary
    public string? SelectedChoice { get; init; }  // set iff Kind == Choice
    public double? Score { get; init; }           // set iff Kind == Score
    public required double Confidence { get; init; } // 0.0–1.0, always present
    public string? ModelId { get; init; }
    public AIDecisionUsage? Usage { get; init; }
}
```

**Decision:** one flat response type with a `Kind` discriminant and
per-kind nullable fields, covering all three of Jev's answer shapes
(binary/choice/score) from the start.
**Rejected alternative:** mirroring Jev's own C# 15 closed record hierarchy
(`Answer` → `NoulAnswer`/`ChoiceAnswer`/`ScoreAnswer`) 1:1 — that ties a
general Umbraco AI capability to one vendor's specific type design, and this
codebase's other response types (`ChatResponse`, etc.) are already flat DTOs,
not discriminated hierarchies.

**Decision:** generalize to all three answer shapes now, not just
binary/yes-no.
**Rejected alternative:** shipping only `Binary` for the spike — a
yes/no-only shape would look suspiciously Jev-specific rather than a
provider-agnostic capability, and the extra surface (an enum + two nullable
fields) is small.

## Data model & persistence

None. No new EF Core entities, no migration. `AIProfile`/`AIConnection`
already store `AICapability` as an int (`Umbraco.AI.Core.Models.AICapability`
+ persistence mapping), so adding one new enum member is forward-compatible
with existing schema — profiles/connections for a `Decision`-capability
provider persist exactly like any other capability's do today.

## Key decisions

### 1. Add `AICapability.Decision`, don't touch `AICapability.Moderation`

`Moderation = 3` shipped as public API on 23-11-2025 (current package version
18.3.5) and was never implemented. Per this repo's "never break a public
API" rule, it can't be renamed or removed. **Decision:** add a new member,
`Decision = 8`, and leave `Moderation` in place, unused, for now.

Numbering note: `AICapability.cs` already has a trailing comment reserving
`6` and `7` for `TextToSpeech`/`SpeechToSpeech`. `Decision` must not squat on
those — it takes `8`, and the comment becomes
`// Future: TextToSpeech = 6, SpeechToSpeech = 7`(unchanged; `Decision` is
listed as a real member above it, not folded into that comment).

**Rejected alternative:** renaming `Moderation` to `Decision` (breaks public
API for any consumer that references the enum member by name — a real
compile break, even though the underlying wire value is an int and would be
unaffected). **Also rejected:** reusing value `3` under a new name via
`[Obsolete]` — enums don't support the same "old method proxies to new
method" backwards-compatible shim that methods do; the member itself has to
stay.

This leaves an open product question — undecided here, deliberately: does a
*real*, non-experimental Moderation feature ever get built as a distinct
thing from Decision, or does Decision end up subsuming what Moderation was
for? Not this spike's call. Recorded as TODO.

### 2. Proprietary client abstraction, gated as experimental — not a bet that it's permanent

Since no M.E.AI type exists to wrap, `IAIDecisionClient` is genuinely
Umbraco-AI-owned, breaking the "thin wrapper, no proprietary abstractions"
philosophy for the first time. **Decision:** treat this exactly like
`ImageGeneration` did when *it* introduced structural uncertainty (in that
case, M.E.AI's own image types being experimental) — a two-layer gate:

- Compile-time: `[Experimental(AIDecisionDiagnostics.DiagnosticId)]`
  (`UMBRACOAI_DECISION`) on the whole public surface
  (`IAIDecisionCapability`, `AIDecisionCapabilityBase*`, `IAIDecisionClient`,
  etc.), forcing an explicit opt-in (`#pragma warning disable
  UMBRACOAI_DECISION` / `<NoWarn>`) from any consumer.
- Runtime: `AIExperimentalOptions.Decision` (bool, default `false`), read by
  `AIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision)`
  (`Umbraco.AI.Core/Settings/AIExperimentalFeatures.cs` — currently a
  `switch` with one case for `ImageGeneration`; add
  `AICapability.Decision => _options.CurrentValue.Decision`). Config key:
  `Umbraco:AI:Experimental:Decision`.

**Rejected alternative:** shoehorning decisions into `IChatClient` (e.g. ask
the model to return JSON and parse it). Rejected because that's exactly the
autoregressive, token-by-token path Jev's whole pitch is skipping — routing
through `IChatClient` would misrepresent what the capability is for, and
would silently work "fine" for a normal chat model while being architecturally
dishonest about what a Decision provider actually does.

If M.E.AI ever ships an official abstraction for this model shape, the
expectation (recorded, not built) is that `IAIDecisionClient` gets replaced
by wrapping that type, the same way a hypothetical future non-experimental
`IImageGenerator` would replace today's `MEAI001`-suppressing usage.

### 3. Full capability depth (client, factory, service, middleware, tracking)

Mirrors `SpeechToText/` file-for-file: `IAIDecisionClientFactory`,
`IAIDecisionService`/`AIDecisionService` (profile-alias resolving, same shape
as `IAISpeechToTextService`), `IAIDecisionMiddleware` +
`AIDecisionMiddlewareCollection(Builder)`, `AITrackingDecisionClient` +
`AITrackingDecisionMiddleware` (usage/audit recording — see
[[project_operation_tracker]] for the capability-recording bugs already found
and fixed across Chat/Embedding/SpeechToText; a new capability should not
reintroduce the same class of gap), `AIOpenTelemetryDecisionMiddleware`,
`AIErrorClassifyingDecisionClient`, executing/executed notifications,
scoped-profile/scoped-inline client wrappers.

**Rejected alternative:** a minimal ad hoc client wired directly from
provider to caller, skipping factory/middleware/service layers "since it's
just a spike." Rejected because that wouldn't exercise the actual thing the
brief is testing — whether this shape of model fits the *real* capability
architecture, tracking and all.

### 4. Spike provider talks to Jev over raw HTTP, not the community SDK

**Decision (build vs. buy):** the throwaway provider that exercises this in
the next phase calls Jev's REST API directly via `HttpClient` + JSON.
**Rejected alternative:** referencing `RavenValentin/TypeSafe.Jev`. Rejected
because it targets `.NET 11`/`C# 15` against this repo's `net10.0`, and its
own author describes it as "vibe-coded" — not something to depend on even
for a disposable spike. A few lines of `HttpClient` avoid both problems and
keep the spike fully disposable, per the brief's non-goal of not solving the
SDK/framework mismatch.
