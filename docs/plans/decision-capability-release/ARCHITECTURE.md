# Architecture

Design for `BRIEF.md`. Builds on the spike's Core plumbing (branch
`v18/feature/decision-capability`, see the archived `decision-capability/` plan folder for
how it was built and why). Everything below was checked against the real code on
`v18/dev`/`v17/dev` and the spike branch on 24-09-2026, not from memory.

## Extension points

| Layer | Product | Extension point | Precedent to copy |
|-------|---------|-----------------|-------------------|
| Capability | `Umbraco.AI.Core` | `AICapability.Decision = 8` + `IAIDecisionClient` pipeline (already on spike branch) | `SpeechToText/` |
| Provider | new `Umbraco.AI.TypeSafe` | `[AIProvider]` class, auto-discovered via `IDiscoverable` | `Umbraco.AI.FireworksAI` (HTTP, no SDK) |
| Management API | `Umbraco.AI.Web` | Versioned management controllers under `Api/Management/Decision/` | `ImageGeneration/` (`GenerateImageController`) |
| TS client | `@umbraco-ai/core` | Public `UaiDecisionController` → repository → server data source | `image-generation/`, `chat/` |
| Backoffice | `@umbraco-ai/core` | Profile-settings switch case, Settings editor picker, lang keys | `ImageGeneration` equivalents |
| Deploy | `Umbraco.AI.Deploy` | `AISettingsArtifact` + `UmbracoAISettingsServiceConnector` default-profile slot | ImageGeneration fix (#227/#228) |
| Automate | `Umbraco.AI.Automate` | `[Action]`-attributed `ActionBase<TSettings, TOutput>` classes | `TranscribeAudioAction` |
| Auto mode | `Umbraco.AI.Agent` | `AIAgentService.SelectAgentForPromptAsync` | itself (existing chat path becomes the fallback) |

No new CMS extension points. Every layer extends an Umbraco AI pattern that already exists
for another capability.

## Data model & persistence

**No migrations.**

- `AISettings.DefaultDecisionProfileId` (`Guid?`) is a new property on an existing type.
  `AISettingsFactory` persists settings reflectively (`GetProperties`), so it's stored with no
  schema change. Config fallback: `AIOptions.DefaultDecisionProfileAlias`.
- Decision profiles and TypeSafe connections are ordinary `AIProfile`/`AIConnection` rows.
- `AIDecisionProfileSettings` is a new, empty, sealed profile-settings type. Jev has no
  per-profile knobs today. It exists so `AIProfileSettingsSerializer`, the Web polymorphic
  model, and Deploy import all have a real Decision case instead of silently returning null
  (the exact gap ImageGeneration hit). Adding a field later is additive.

## Core types (reworked from the spike)

The spike's flat `AIDecisionQuestion`/`AIDecisionResponse` (`Kind` + nullable per-kind fields,
`Choices` as a string list, `ScoreRange` as a ValueTuple) is replaced by one type per kind.
This mirrors the shape Microsoft's proposal uses (dotnet/extensions#7764:
`BinaryDecisionQuestion`/`ChoiceDecisionQuestion`/`ScoreDecisionQuestion`), so a later
migration is mostly renames. It also fits Jev's real API, which the spike's shape didn't
(labelled score levels, per-option descriptions, separate content vs. instructions).

```csharp
// Umbraco.AI.Core/Decision/ — all [Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionQuestion
{
    public required string Instructions { get; init; }  // what to decide
    public string? Context { get; init; }               // the content to judge (Jev "state")
}
public abstract class AIDecisionQuestion<TResponse> : AIDecisionQuestion
    where TResponse : AIDecisionResponse;

public sealed class AIBinaryDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionResponse>
{
    public string? TrueCriteria { get; init; }   // what "yes" means (optional)
    public string? FalseCriteria { get; init; }  // what "no" means (optional)
}
public sealed class AIChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionResponse>
{
    public required IReadOnlyList<AIDecisionOption> Options { get; init; } // 2..255, unique keys
}
public sealed record AIDecisionOption(string Key, string? Description = null);
public sealed class AIScoreDecisionQuestion : AIDecisionQuestion<AIScoreDecisionResponse>
{
    public required IReadOnlyList<string> Levels { get; init; } // 2..10, lowest first
}

public abstract class AIDecisionResponse
{
    public string? ModelId { get; init; }
    public UsageDetails? Usage { get; init; }
    public object? RawRepresentation { get; init; }
    public abstract double Confidence { get; }
}
public sealed class AIBinaryDecisionResponse : AIDecisionResponse
{
    public required double Probability { get; init; }   // P(true), 0..1
    public bool Answer => Probability >= 0.5;
    public override double Confidence => Answer ? Probability : 1 - Probability;
}
public sealed class AIChoiceDecisionResponse : AIDecisionResponse
{
    public required string Choice { get; init; }        // an option Key
    public required double ChoiceConfidence { get; init; }
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = ...;
    public override double Confidence => ChoiceConfidence;
}
public sealed class AIScoreDecisionResponse : AIDecisionResponse
{
    public required double Score { get; init; }         // 0-based level index, fractional
    public required string Level { get; init; }         // label of the nearest level
    public required double ScoreConfidence { get; init; }
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = ...; // by label
    public override double Confidence => ScoreConfidence;
}
```

- `IAIDecisionClient.AskAsync(AIDecisionQuestion, AIDecisionOptions?, CancellationToken)`
  stays **non-generic**, returning the base `AIDecisionResponse`. Middleware, tracking,
  telemetry and the error classifier don't care about the kind, so they stay one class each.
- `IAIDecisionService.AskAsync<TResponse>(AIDecisionQuestion<TResponse> question, ...)` is
  generic. The question's type decides the response type, so callers get
  `AIBinaryDecisionResponse` back with no cast. The service checks the client returned the
  matching type and throws `AIProviderException` if not (a provider bug, not a caller bug).
  Overloads: profile `Guid`, profile alias `string`, `Action<AIDecisionBuilder>`, and none
  (default Decision profile).
- `AIDecisionKind` is deleted. The type is the discriminator.
- `AIDecisionOptions` keeps the spike's M.E.AI-style mutable class with `Clone()` (`ModelId`
  only).
- `ValidatingDecisionClient` rules become per type: `Instructions` not blank; choice options
  2..255 with unique, non-blank keys; score levels 2..10, none blank. It stays outermost (the
  spike's wrapping-order test stays).

## Key decisions

1. **Continue the spike branch, don't rebuild.** The Core plumbing is reviewed and tested,
   and `origin/v18/dev` merges in conflict-free. Work continues on
   `v18/feature/decision-capability`, brought up to date by merging `v18/dev` in (not a
   rebase, which would need a force-push). The spike's plan folder moves to
   `docs/archive/decision-capability/`.
   *Rejected:* a fresh branch cherry-picking Core commits. Same result, more churn.

2. **One type per decision kind** (see Core types).
   *Rejected:* keeping the spike's flat type. Every consumer (API, TS, Automate outputs)
   would re-check `Kind` and nulls, and it drifts further from #7764.

3. **TypeSafe provider is scaffolded with the `add-provider` skill**, which owns the package
   layout and every registration point (root `.slnx`, demo/test-site install scripts,
   `azure-pipelines.yml` `level1Products`, `version.json`, `changelog.config.json` scope,
   marketplace files, initial `CHANGELOG.md`). Build tasks follow the skill rather than
   restating its checklist here. The only Decision-specific parts are the capability class,
   the client, and the per-file `UMBRACOAI_DECISION` pragma.

   It calls Jev over HTTP directly (`IHttpClientFactory` + `System.Text.Json`), like
   `FireworksAIProvider`.
   *Rejected:* the community `RavenValentin/TypeSafe.Jev` SDK. It targets .NET 11/C# 15 and
   is unofficial.
   - One question per call, keyed `"q"`. Jev's batch form (many questions against one
     `state`) is not exposed. It would need a new batch API shape; YAGNI until a consumer
     wants it.
   - `Context ?? Instructions` is sent as `state`. `Instructions` is sent as `instructions`.
   - `usage.input_tokens`/`output_tokens` map to `UsageDetails`, so analytics work.
   - `options.ModelId` (falling back to the profile model) is sent as `model`. The models list
     is `jev-latest` only, since Jev documents no models endpoint. Listing models sends one tiny
     authenticated `noul` probe, cached for an hour per connection settings, so "Test
     connection" (which calls `GetModelsAsync`) fails for a bad key.
   - 401 → auth failure, 422 → validation failure, 429/529 → retried up to 2 times with
     exponential backoff (honoring `Retry-After`), then a rate-limit/overloaded failure. All
     surface as `HttpRequestException` with `StatusCode`, which the base provider classifier
     maps (401 Authentication, 422 InvalidRequest, 429 RateLimited, 529 Transient).

4. **Default Decision profile only, no Decision classifier setting.** Copilot auto mode checks
   `HasDefaultProfileAsync(AICapability.Decision)` and asks via the default profile.
   *Rejected:* a separate "Classifier Decision Profile" setting. Jev has one model, so a second
   profile slot adds UI and Deploy fields for no real choice.

5. **Hide disabled experimental capabilities in the UI properly** with one new generic read,
   `GET capabilities/enabled`, returning the `AICapability` names enabled on this install.
   The Settings editor hides a default-profile picker whose capability isn't in that list.
   This fixes ImageGeneration's always-visible empty picker too.
   *Rejected:* matching ImageGeneration's current behavior (empty picker shows).
   *Rejected:* adding flags to the settings response. Settings are persisted data; enabled
   capabilities are install config.
   - `AllProviderController` also drops providers with **zero** enabled capabilities.
     TypeSafe is the first provider whose only capability is experimental. Without this it
     would show in the provider dropdown with nothing to do. Providers with at least one
     enabled capability are unaffected.

6. **Three Automate actions, not one.** "Ask yes/no", "Ask pick-one", "Ask score", each with
   simple settings and a typed output that If/Switch steps can bind to.
   *Rejected:* one action with a kind setting. It would need fields that show and hide by
   kind, which Automate doesn't appear to support, plus a loose output.
   - **Gating:** Umbraco.Automate has no runtime "is this action available" hook. Actions
     are excluded from `builder.AutomateActions()` at compose time when
     `Umbraco:AI:Experimental:Decision` is false, and each `ExecuteAsync` also returns
     `Failed` if the flag is off at run time (flag flipped without a restart).
     `ActionCollectionBuilder` is a `LazyCollectionBuilderBase`, so `Exclude<T>()` works from
     any composer. Known upstream limitation: Umbraco.Automate silently skips a now-missing
     step in an already-published automation (umbraco/Umbraco.Automate#343).

7. **Auto mode tries Decision, falls back to today's path.** In
   `SelectAgentForPromptAsync`, only when there are 2..255 available agents:
   1. If `Decision` is enabled and a default Decision profile resolves, ask an
      `AIChoiceDecisionQuestion`: options are the agents (key = agent id, description =
      name + description), context = the user's message.
   2. If that returns a known agent id, use it. **No confidence threshold.** Jev's answer is
      used as-is; a threshold would add a second model call on exactly the cases where
      latency matters.
   3. Any other outcome (flag off, no default profile, provider error, unknown key) logs at
      debug/warning and runs the existing chat-classifier code unchanged.
   *Rejected:* falling back to chat on low confidence (double cost/latency, no evidence it's
   needed yet).

8. **Experimental suppression is per file, never project-wide.** Consumers outside Core
   (`TypeSafe`, `Web`, `Deploy`, `Automate`, `Agent`) use
   `#pragma warning disable UMBRACOAI_DECISION` in the files that touch Decision, matching
   `OpenAIProvider.cs` (`UMBRACOAI_IMAGEGEN`) and `TranscribeAudioAction.cs` (`MEAI001`).
   *Rejected:* `NoWarn` in csproj. It would hide future experimental APIs too.

9. **Spike code is deleted, not moved.** `Tests.Common/Decision/Spike/*` goes. Its HTTP tests
   are rewritten against the real `TypeSafeDecisionClient` in a new
   `Umbraco.AI.TypeSafe.Tests.Unit`. The T12 "real provider goes inert" tests in Core's
   `AIProfileServiceTests`/`AIConnectionServiceTests` switch to the existing
   `FakeDecisionCapability` (Core tests must not reference a provider package). The new test
   project is added to the root `.slnx`, since CI only runs what's listed there (see
   `project_ci_test_coverage` memory).
   - **Deliberate deviation from `add-provider`:** that skill says providers have no test
     project by convention. The root `CLAUDE.md` lists `tests/ProviderName.Tests.Unit/` as
     the provider shape. TypeSafe gets a test project because, unlike SDK-backed providers,
     its client *is* the wire mapping (hand-written HTTP/JSON). The spike showed every guessed
     wire detail was wrong on first contact, so that mapping needs its own tests.

10. **Release scope and version floors.** Products touched: `Umbraco.AI` (minor),
    `Umbraco.AI.TypeSafe` (new; `18.0.0` on v18, `17.0.0` on v17), `Umbraco.AI.Deploy`,
    `Umbraco.AI.Automate`, `Umbraco.AI.Agent` (minor each). Every add-on that calls new Core
    API raises its `Umbraco.AI.Core` floor to the Core version that ships Decision.

11. **v17 is a straight port after v18 is complete.** Simulated cherry-pick of the spike
    onto `origin/v17/dev` auto-merged every code file. `TranscribeAudioAction` and
    `SelectAgentForPromptAsync` are identical on both lines. Port via the `backport` skill
    as a separate branch and PR. Remaining v17 risk is compile-time only (CMS 17 APIs), to
    be proven by building, not assumed.

12. **Docs are one Umbraco.Docs branch** (`ai/decision-docs`), both `17/` and `18/`, held
    as a draft PR until release. Page list in `SPEC.md`.

## Security

- **Auth:** new endpoints inherit `UmbracoAICoreManagementControllerBase`'s
  `BackOfficeAccess` policy, the same as Chat's completion endpoint and
  `GenerateImageController`, so the TS client is callable from backoffice code outside the AI
  section.
- **Input limits at the API boundary:** the request model enforces the same structural
  limits as `ValidatingDecisionClient` (option/level counts, non-blank keys) before any
  provider call, returning 400. Oversized content is left to Jev's 422, surfaced as 400.
- **API key:** `[AIField(IsSensitive = true)]`, so it's masked in the editor and never
  returned to the client, per the masked-sensitive-fields work.
- **Frontend:** responses contain only option keys/level labels the caller supplied plus
  numbers. Rendered through Lit bindings, never `unsafeHTML`.
