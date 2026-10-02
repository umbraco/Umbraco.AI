# Brief

## Problem

The `decision-capability` spike (branch `v18/feature/decision-capability`, plan folder
`docs/archive/decision-capability/`) proved that a typed-decision model like
TypeSafe AI's Jev can be represented as an Umbraco AI capability without new middleware
shapes or new Profile/Connection concepts. It round-tripped a live Jev answer
(`Kind=Binary BinaryAnswer=True Confidence=0.99`) through a full `AICapability.Decision`
pipeline in `Umbraco.AI.Core`. The spike's kill criterion was not hit.

But nothing a real developer can use exists yet:

- The only provider is a throwaway `JevSpikeProvider` hidden in `Umbraco.AI.Tests.Common`
  on purpose, so it never auto-discovers into a real install.
- There's no way to call Decision from outside C# in-process: no Management API endpoint,
  no TypeScript client.
- A Decision profile can't be set as the site's default, and Umbraco Deploy wouldn't carry
  that default between environments.
- The spike branch was never merged and is behind `v18/dev`.

This feature takes Decision from "proven possible" to "installable and usable by a
developer", while keeping it experimental.

### Who it's for

**Developers** building on Umbraco AI: package developers and site developers who want a
cheap, typed yes/no, pick-one, or score answer from C# or from backoffice TypeScript,
instead of prompting a chat model and parsing free text.

Backoffice users touch it for setup (connection, profile, default profile), plus **one
consumer: an Umbraco Automate action** (in `Umbraco.AI.Automate`) that asks a Decision
question as a workflow step, so an automation can branch on a typed answer. This is
a release blocker: Decision does not ship without it.

One **internal consumer** too: Copilot's auto mode agent selection
(`Umbraco.AI.Agent`'s `AIAgentService.SelectAgentForPromptAsync`) uses a Decision
`Choice` question to pick the agent when Decision is available, and falls back to today's
chat-classifier path when it isn't. No agent tool, Prompt, or other Copilot consumer in
this scope.

### Why now

- Jev shipped 15-09-2026 and has real community traction; the spike showed the
  architecture fits.
- The Core plumbing already exists on the spike branch. The remaining work is the
  surrounding layers every shipped capability has (provider package, API, client, Deploy,
  settings UI).
- Microsoft's own abstraction proposal ([dotnet/extensions#7764](https://github.com/dotnet/extensions/issues/7764))
  is still `untriaged` as of 24-09-2026, with no maintainer response. Waiting for it is
  likely a long wait (the comparable text-to-image proposal took 14+ months and is still
  experimental). See the spike's `REFERENCES.md`.

### Success looks like

1. A developer installs a new `Umbraco.AI.TypeSafe` provider package, turns on
   `Umbraco:AI:Experimental:Decision`, and in the backoffice:
   - creates a TypeSafe connection (API key),
   - creates a Decision profile against it,
   - sets it as the **default Decision profile** in the Umbraco AI Settings section.
2. They get a typed answer for **all three kinds** (binary/`noul`, choice, score), live
   against Jev, from:
   - **C#**: `IAIDecisionService.AskAsync(...)` (profile by id, alias, or default).
   - **TypeScript**: a public client class exported from `@umbraco-ai/core`, following
     the existing Chat pattern (`UaiChatController` → repository → server data source →
     a Management API endpoint like `CompleteChatController`).
3. Umbraco Deploy moves the TypeSafe connection, the Decision profile, and the default
   Decision profile setting between environments without dropping anything.
4. With the flag off (the default), Decision is fully inert everywhere: hidden from
   capability listings, no connections returned for it, profile creation rejected, no
   default-profile picker shown, API endpoint unavailable or rejecting.
5. Usage shows up in the existing analytics/audit tracking like other capabilities.
6. An automation in Umbraco Automate can include an "ask a decision" action (profile,
   question, kind, choices/criteria as settings) whose typed output (answer + confidence)
   later steps can branch on. Precedent: `Umbraco.AI.Automate/Actions/TranscribeAudioAction.cs`,
   which already wraps a single capability (SpeechToText) as an action.
7. Copilot auto mode picks an agent with a Decision `Choice` question (choices = the
   available agents, answer + confidence) when the flag is on and a Decision profile is
   available. When the flag is off or no Decision profile is set up, it behaves exactly as
   it does today: the chat classifier prompt with GUID parsing (`GetClassifierProfileAsync`,
   falling back to the default chat profile, then the first agent). No change for sites that
   never enable Decision.
8. No spike code remains: `Tests.Common/Decision/Spike/*`, the "spike" naming, and any
   temp demo-site verification code are gone; tests target the real provider instead.
9. Ships on **v18 and v17 together** in the same release round, `Umbraco.AI.Automate`
   and `Umbraco.AI.Agent` included.
10. Public docs in `Umbraco.Docs` for both v17 and v18: the Decision capability (clearly
   marked experimental, with how to turn the flag on), the TypeSafe provider, the C# and
   TypeScript APIs, and the Automate action. Opened as a draft PR and held until the
   feature releases, as with ImageGeneration's docs (draft PR #8222).

### Constraints

- **Stays experimental.** `[Experimental("UMBRACOAI_DECISION")]` at compile time and
  `Umbraco:AI:Experimental:Decision` (default off) at runtime, like `ImageGeneration`.
  `IAIDecisionClient` is an Umbraco-owned abstraction with no M.E.AI equivalent; if
  Microsoft ships one, the plan is to migrate to it later. The experimental flag is what
  makes that migration non-breaking.
- **Public API rules still apply** to everything outside the experimental surface (e.g.
  new settings fields, Deploy artifact fields). `AICapability.Moderation = 3` stays
  untouched.
- **Provider naming:** package is named after the company, `Umbraco.AI.TypeSafe`, not the
  model (see the `add-provider` skill).
- **Targets:** `net10.0`, CMS 18.x on v18 and CMS 17.x on v17. The community Jev .NET SDK
  targets .NET 11 / C# 15, so it can't be referenced as-is.
- **No forward-merge between lines.** v17 gets a separate port branch and PR (Backport
  Workflow in root `CLAUDE.md`).

### Riskiest unknowns

1. **v17 parity.** The spike was v18-only. Unknown whether every piece Decision's Core
   plumbing leans on (capability settings series, `AIExperimentalFeatures`, operation
   tracker) exists in the same shape on `v17/dev`. TODO: check before design commits to
   "port the same code."
2. **Frontend generic handling.** Unknown how much of the connection/profile editors
   already work for a new capability generically vs. needing Decision-specific pieces
   (e.g. a per-capability profile-settings element, a capability label/icon, the
   settings-section default-profile picker). TODO: check in design.
3. **Experimental gating at the API and UI layers.** The spike proved gating in
   `AIConnectionService`/`AIProfileService` only. Unknown how the new endpoint, the TS
   client, and the Settings default-profile field should behave when the flag is off.
   TODO: design decides; success criterion 4 says "fully inert."
4. **Jev's wire API** is young. The spike found every guessed detail wrong on first
   contact. Known open gap: `Score` needs a `criteria` array (level labels) that
   `AIDecisionQuestion` doesn't carry yet. Unknown whether Jev's `state` (shared context
   across a batch) should be exposed. TODO: design.
5. **How the public TS types map a flat, `Kind`-discriminated `AIDecisionResponse`**
   (nullable per-kind fields) into a clean TypeScript shape. TODO: design.
6. **Experimental gating inside Automate.** Unknown whether Umbraco Automate can hide or
   refuse an action at runtime based on `Umbraco:AI:Experimental:Decision`, or whether
   actions are registered unconditionally. Also unknown how an Automate action consumes
   an `[Experimental]`-marked Core API (suppressing `UMBRACOAI_DECISION` in a shipped
   package). TODO: check in design.
7. **Which profile auto mode uses.** Auto mode already has its own "classifier profile"
   setting. Unknown whether that setting should accept a Decision profile, or auto mode
   should just use the new default Decision profile. Also open: whether a low-confidence
   Decision answer should fall back to the chat path, and whether Jev's `choice` type has a
   limit on the number of options (a site can have many agents). TODO: design.
8. **Carry-over vs. rebuild.** Whether to rebase the spike branch or cherry-pick its Core
   commits onto a fresh branch. Parked for design / build-loop; noted here only because
   the spike's `PLAN.md`/`BUILD-LOG.md` are a finished record that shouldn't be
   overwritten.

### Smallest version worth shipping

All of success criteria 1-8 on v18, then the v17 port. The Automate action and the
auto mode change are built last, once the Core API they call is settled. Docs are written after that, against the
final shape. Every layer is thin; dropping
any one (API, TS client, Deploy, settings) leaves a capability that's hard to use or
silently lossy, which is worse than not shipping. Order inside the build is a design/plan
question.

### What would change course

Microsoft shipping its own abstraction mid-build does **not** stop this: keep going and
migrate later behind the experimental flag. A real reason to pause would be finding that
the Management API or frontend layers need genuinely new shared concepts (not just a
Decision-shaped copy of the SpeechToText/ImageGeneration pattern).

## Non-goals

- **Other consumers.** No agent tool, Prompt integration, or Copilot use of Decision.
  The Automate action and Copilot auto mode agent selection are the only consumers in
  scope.
- **A try-it / test panel** in the backoffice for asking ad-hoc questions.
- **Graduating from experimental.** Stable, flag-free release waits on the M.E.AI
  question.
- **A separate C# HTTP client** for calling the Management API from outside the site.
  "C# client" here means the in-process `IAIDecisionService`.
- **Implementing `AICapability.Moderation`.** Left reserved and unused.
- **Other decision-model vendors.** Only TypeSafe/Jev. The Core abstraction should not
  assume Jev specifics, but no second provider is built to prove that.
- **Validating Jev's cost/accuracy claims.** Already accepted as validated by the
  community.
