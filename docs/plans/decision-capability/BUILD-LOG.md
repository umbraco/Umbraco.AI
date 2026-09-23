# Build Log

- **T1** — `8e83dfc` — `AICapability.Decision = 8` added, `Moderation = 3` and the
  `TextToSpeech`/`SpeechToSpeech` placeholder comment left untouched. Verified: builder ran
  `dotnet build` on `Umbraco.AI.Core.csproj` (0 errors); reviewer independently re-ran the
  build and confirmed every existing `switch` over `AICapability` has a safe default branch,
  so the new value can't break anything already shipped. PASS on first review pass.

- **T2** — (pending SHA) — `Decision/` core types added (Kind, Question, Options, Response +
  factories, IAIDecisionClient, Diagnostics). First review pass found 2 Important issues:
  a custom `AIDecisionUsage` type would have forced an unplanned converter in T7 and lost
  usage fields the tracking pipeline already reads, and the response factories couldn't set
  `ModelId`/`Usage`. Both fixed (switched `Usage` to M.E.AI `UsageDetails`, added optional
  `modelId`/`usage` params to the factories) and re-reviewed. PASS on second review pass.

- **T3** — `2f0389eb` — `AIExperimentalOptions.Decision` (default off) + the
  `AIExperimentalFeatures.IsCapabilityEnabled` switch case added, mirroring `ImageGeneration`
  exactly. Verified for real: reviewer built a scratch xUnit project (assembly named
  `Umbraco.AI.Tests.Unit` for `InternalsVisibleTo`) referencing the freshly built
  `Umbraco.AI.Core.dll`, compiled only `AIExperimentalFeaturesTests.cs`, ran it — 7/7 passed,
  including both new Decision cases. PASS on first review pass.

- **T4** — `3078ecab` — `ValidatingDecisionClient` added (sad-path enforcement: empty/whitespace
  `Prompt` and `Choice` with <2 `Choices` both throw `ArgumentException` before reaching any
  inner client). Verified: reviewer independently built a scratch project and got 5/5 real
  passes on `ValidatingDecisionClientTests.cs`. PASS with carry-forward items (not defects):
  the class isn't wired into any factory yet (that's T7), and — important — T7 must place it
  *outside* `AIErrorClassifyingDecisionClient`/tracking, not innermost, or caller errors get
  miscategorized as provider failures. PLAN.md's T7/T8 entries were updated with explicit
  acceptance criteria for this. Also applied a small suggested fix (`ArgumentNullException.
  ThrowIfNull(question)`) before committing.
