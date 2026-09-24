# Decision Log

## 24-09-2026 — Scoped during `umb-explore`

- New plan folder, not a rewrite of `decision-capability/`: the spike's `PLAN.md` and
  `BUILD-LOG.md` are a finished record of a completed spike, and later phases here would
  overwrite them. The spike folder is a candidate for `docs/archive/` once this lands.
- Stays experimental (flag default off, `UMBRACOAI_DECISION` diagnostic): `IAIDecisionClient`
  is Umbraco-owned with no M.E.AI equivalent, and the flag keeps a later migration to
  Microsoft's abstraction (dotnet/extensions#7764) non-breaking.
- Ships on v18 and v17 together, not v18-first: user choice.
- Backoffice scope is connection + Decision profile + a default Decision profile setting in
  the Settings section. No try-it panel.
- Audience is developers (C# and TypeScript). No editor-facing consumer (agent tool,
  Automate step, Prompt) in scope.
- "C# and TS clients" means `IAIDecisionService` in-process, plus a public TypeScript client
  following the existing Chat pattern (`UaiChatController` → repository → Management API
  endpoint). No separate C# HTTP client.
- All three answer kinds (binary, choice, score) required at first release, which means
  fixing the spike's known `Score` criteria gap.
- If Microsoft ships its abstraction mid-build: keep going, migrate later.
- An Umbraco Automate "ask a decision" action is in scope, built last. User said it must
  exist before Decision ever releases, so it goes in this plan rather than a follow-up:
  "plan done" then really means "releasable." Modeled on `TranscribeAudioAction`.
  Confirmed it's an Automate *action* (workflow step), not an agent tool.
- Copilot auto mode agent selection moves to a Decision `Choice` question when Decision is
  available, falling back to the existing chat-classifier path otherwise. Chosen because it's
  small, the fallback already exists, and it's the best real proof the API fits an internal
  use. Accepted cost: `Umbraco.AI.Agent` joins the release and depends on an experimental API.
- Public docs (Umbraco.Docs, v17 + v18) are in scope, written last and held as a draft PR
  until release, matching the ImageGeneration docs precedent.

## 24-09-2026 — Designed during `umb-design`

- Rebase and continue the spike branch rather than rebuild: dry-run merge onto
  `origin/v18/dev` is clean, and a simulated v17 cherry-pick auto-merges every code file.
- One question/response type per kind (binary/choice/score), replacing the spike's flat
  `Kind`-discriminated shape. Mirrors dotnet/extensions#7764 for an easier migration, and fits
  Jev's real API (labelled score levels, per-option descriptions, content vs. instructions).
  User choice.
- Client stays non-generic; the service is generic on the question's response type.
- Auto mode uses the default Decision profile. No separate Decision classifier setting. User
  choice.
- Hide disabled experimental capabilities in the Settings UI via a new generic
  `GET capabilities/enabled`, fixing ImageGeneration's empty picker too. User chose this over
  matching ImageGeneration's current behavior.
- Hide providers with zero enabled capabilities from `GET providers`. TypeSafe is the first
  experimental-only provider.
- Three Automate actions (yes/no, pick-one, score). User choice. Gated at compose time plus a
  run-time guard, because Automate has no availability hook.
- Auto mode uses Decision with no confidence threshold, and falls back to the unchanged chat
  path on any failure.
- `UMBRACOAI_DECISION` suppressed per file with `#pragma`, never `NoWarn`.
- Empty `AIDecisionProfileSettings` type added so serializer/Deploy/Web have a real Decision
  case (avoids the ImageGeneration null-settings gap).
- Jev batch questions not exposed (YAGNI).
- TypeSafe package scaffolded via the `add-provider` skill (user pointed this out). The one
  deliberate deviation: it gets a unit test project, since its hand-written HTTP mapping is
  the riskiest code in the feature.

## 24-09-2026 — Sequencing decided during `umb-plan`

- **Merge `origin/v18/dev` into the spike branch instead of rebasing** (reverses
  ARCHITECTURE decision 1's "rebase" wording). The branch is already pushed, so a rebase would
  need a force-push. A merge gets the same up-to-date code with no history rewrite.
- **Spike code is deleted first (T1), before the type rework (T2).** The spike client in
  `Tests.Common` uses the flat types T2 removes, so deleting it later would mean fixing
  throwaway code only to delete it.
- **One OpenAPI client regeneration (T13), after the backend wire task (T12).** Regeneration
  needs a running demo site and touches one shared generated folder. Doing it once, after
  every API change has landed, avoids three frontend tasks racing on `src/api/`.
- **Every real entry point gets its own wire task** (T12, T17, T19, T21, T23, T25). Unit-green
  isn't accepted for the API, backoffice, Deploy, Automate, or Copilot paths.
- **v17 port (T24) waits for every v18 wire task.** Porting before v18 is proven would mean
  fixing each bug twice.
- **Pending specs are staged in `specs/` inside this plan folder, not in their test
  projects.** They reference types that don't exist until their task lands, and C# test
  projects compile every file in the folder. Staged in place, they'd break the build for T0-T1
  and every task before theirs. Each task moves its own files in and commits them with the
  production code. See `specs/README.md`.
- **No capability check on `defaultDecisionProfileId`** (DR-3 AC4 dropped; SPEC.md corrected).
  Spec-writing found SPEC's "same check as the other default slots" was wrong: no default
  slot validates capability today. User chose to match the existing slots rather than add a
  Decision-only check or change every slot's API behavior.
