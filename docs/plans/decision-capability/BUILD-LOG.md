# Build Log

- **T1** — `8e83dfc` — `AICapability.Decision = 8` added, `Moderation = 3` and the
  `TextToSpeech`/`SpeechToSpeech` placeholder comment left untouched. Verified: builder ran
  `dotnet build` on `Umbraco.AI.Core.csproj` (0 errors); reviewer independently re-ran the
  build and confirmed every existing `switch` over `AICapability` has a safe default branch,
  so the new value can't break anything already shipped. PASS on first review pass.
