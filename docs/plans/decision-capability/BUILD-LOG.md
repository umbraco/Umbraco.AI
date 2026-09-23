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
