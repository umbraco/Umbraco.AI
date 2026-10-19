# Decision Log

> **Status:** Archived 09-10-2026. Shipped: merged to `v18/dev` (#419) and `v17/dev` (#428) on 09-10-2026, together with the follow-on `decision-evaluators` plan (#430, #431). Only T26 (the public Umbraco.Docs PR, branch `ai/decision-docs`) was still open at archive time.

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

## 24-09-2026 — Found during `umb-build-loop`

- **T2: choice option keys are unique by ordinal (case-sensitive) comparison.** `"a"` and
  `"A"` are distinct options. Keys are machine identifiers the caller supplies (e.g. agent
  ids) and come back verbatim as the answer, so they're matched exactly rather than folded.
- **T20: Automate "pick-one" options are a text area, one `key` or `key: description` per
  line**, split at the first colon, so keys can't contain `:`. No Automate or CMS field editor
  pairs a key with a description. Levels use the CMS `MultipleTextString` list editor, since
  they're labels only. Blank and duplicate keys aren't handled in the action; they reach Core's
  validator and fail as Validation.
- **Release-time item: version floors for consumers.** `Umbraco.AI.Automate` reaches Core only
  through its `Umbraco.AI.Agent` reference. Raising the root `Umbraco.AI.Core` floor alone won't
  protect Automate. The release must also raise Automate's `Umbraco.AI.Agent` floor to the Agent
  version that ships alongside Decision (and Agent's own Core floor, per T22).
- **T21: keep hiding the Automate Decision actions when the flag is off**, even though the live
  check showed Umbraco.Automate silently skips a missing step in an already-published automation.
  A disabled action can then make an If take the wrong branch while the run reports `Completed`.
  User decision: keep compose-time exclusion and log the silent-skip bug on the Umbraco.Automate
  issue tracker instead of dropping the exclusion. It's an upstream compiler bug that also hits
  uninstalled packages.
- The silent-skip bug is logged as umbraco/Umbraco.Automate#343 (reviewed by the user before
  posting). The `BranchOutcome` persistence bug is logged as umbraco/Umbraco.Automate#344.
- **TypeSafe "Test connection" does a real, cached key check.** Core's `TestConnectionAsync`
  only calls the first capability's `GetModelsAsync`, and TypeSafe's model list was static, so
  a wrong API key passed the test. Other providers list models live (auth-checked). Jev has no
  models endpoint, so listing models sends one tiny `noul` probe (a few hundred tokens), cached
  per connection settings for an hour like other providers cache model lists. Any failure makes
  the test fail. User choice over documenting the gap.
- **Unknown profile alias silently falls back to the default profile, for every capability.**
  The docs agent found it and the orchestrator confirmed it: `ResolveProfileAsync` in
  `AIChatService`, `AISpeechToTextService` and `AIDecisionService` turns an unknown alias into
  null and then uses the default profile. User decision: leave Decision matching the other
  capabilities in this feature, and fix it for all capabilities as a separate bug fix on both
  lines (an unknown alias should throw "profile not found").
- **Docs ship as one draft PR** in Umbraco.Docs (branch `ai/decision-docs`, 3 themed commits,
  about 60 files across 17/ and 18/), asking for an exception to the 10-article cap on
  AI-assisted PRs. Held until release, like ImageGeneration's. Expect conflicts with the
  ImageGeneration docs PR #8222 on shared pages (`ai-options.md` "Experimental Features" and
  others).

## 25-09-2026 — Decision review (`umb-decision-review`)

- **Auto mode skips the Classifier Chat Profile when a default Decision profile exists.**
  Accepted by the user.
- **Test-only dependencies are fine.** `Microsoft.AspNetCore.TestHost` stays.
- **Providers can have test projects.** The `add-provider` skill is updated on both lines.
- **Automate bindings:** the yes/no `TrueCriteria`/`FalseCriteria` are made bindable now, on both
  lines. Umbraco.Automate only resolves bindings in `string` and `IList<string>` settings, and
  only shows the binding picker on TextBox/TextArea editors. So a key/value options editor with
  bindable pairs, or options taken whole from a previous step, needs Automate changes first. That
  goes in an upstream issue (expected behavior only); the key/value editor is a follow-up waiting
  on it. Score levels aren't made bindable yet, because the list editor has no picker.
- Everything else in the review was accepted as documented.
- **Key/value options editor included now, without bindings** (user, 25-09-2026). It's a
  reusable `Uai.PropertyEditorUi.KeyValueList` in `@umbraco-ai/core`, since Umbraco.AI.Automate
  has no frontend. It replaces the one-`key: description`-per-line text area for "Ask pick-one"
  options. Binding options stays a follow-up that waits on the upstream Automate issue.

## 29-09-2026 — Shared capability gate

- **Decision and ImageGeneration endpoints now block themselves the same way** when their experimental
  flag is off: one internal `[AICapabilityGate(AICapability.X)]` resource filter on each controller base
  (`Common/Filters/`), replacing the Decision-only `DecisionCapabilityGateFilter` and ImageGeneration's
  inline-only check. User decision, raised while reviewing the stacked decision-evaluators PR (#430).
- **Behavior change for ImageGeneration:** with the flag off, a malformed request now gets 404 instead of
  the automatic 400, because the gate runs before model binding (as Decision's always did). The inline
  checks stay as a backup for direct action calls.

## 01-10-2026 — Alignment with the M.E.AI decision abstraction

- **Don't reshape our public API to match M.E.AI yet** (user, 01-10-2026). Microsoft has opened
  dotnet/extensions#7795 (Layer 1: `IDecisionClient`, `DecisionRequest`, `Decision*` questions
  and answers) and #7796 (Layer 2: `AIFunction` and routing helpers), following issue #7764. Both
  are open, experimental (`MEAI001`), and already use different names from the issue, so copying
  them now likely means renaming twice.
- **How the shapes differ today:**
  - M.E.AI sends one JSON state with many id'd questions per request. We ask one typed question
    per call.
  - M.E.AI's client and response are non-generic, with a list of answers. Our
    `IAIDecisionService.AskAsync<TResponse>` returns the matching typed response.
  - Names: `TrueDescription`/`FalseDescription` vs our `TrueCriteria`/`FalseCriteria`;
    `DecisionCandidate(Id, Description)` vs `AIDecisionOption(Key, Description)`;
    `DecisionScoreLevel(Id, Description)` vs our `string` levels; `TrueProbability` vs
    `Probability`; `SelectedCandidateId` vs `Choice`.
  - M.E.AI has no `Confidence`. It adds `ExpectedScore`, `DecisionPrecision`, provenance,
    distribution validation, and `DecisionClientException.IsTransient`.
- **Plan when it lands:** keep `IAIDecisionService` (one typed question per call) as our public
  surface, and make the provider layer (`IAIDecisionClient`) wrap M.E.AI's `IDecisionClient`, as
  we do for `IChatClient`. Our Decision API is `[Experimental]`, so renames at that point don't
  break a promise.
- **Revisit when:** M.E.AI ships the decision abstraction in a release (even as experimental),
  #7795 merges, or a second Decision provider (for example, OpenAI's announced Decision API)
  needs adding.

## 02-10-2026 — Re-designed during `umb-design` (M.E.AI direction)

Revises the 01-10-2026 call. We still don't copy M.E.AI's code, but we change our shapes now
(before #419 merges) where issue #7764 and PR #7795 agree, plus the evaluator feedback on #7764
(comment 5947149171). Replaces "one type per kind" responses and decision 3's "one question
per call, keyed `q`".

- **Batch at the provider layer and on the C# service** (user). `IAIDecisionClient.GetResponseAsync(AIDecisionRequest)`
  takes shared `State` plus id'd questions and returns answers keyed by id.
  `IAIDecisionService.GetDecisionResponseAsync` exposes it. The Management API, TS client and
  Automate stay one question per call.
  *Rejected:* provider layer only (the grader follow-on wants several criteria per call);
  batch everywhere (no consumer yet).
- **One-question `AskAsync` returns `AIDecisionResponse<TAnswer>`** (user): `.Answer` typed, plus
  `ModelId` and `Usage`. *Rejected:* returning just the answer (callers would lose model and
  usage, or need the batch method to get them).
- **`Context` moves off the question and becomes `AIDecisionRequest.State`.** Both #7764 and
  #7795 put the state on the request. Naming it `State` leaves room for a separate reference
  slot later (the feedback measured 64% → 96% accuracy with one), which neither M.E.AI design
  has yet. Plain `string`, not `JsonElement`. Automate keeps "Context" in the UI.
- **Binary: `TrueProbability` and `IsTrue(double threshold = 0.5)`, no `Confidence`** (user).
  The cut-off is the caller's choice. *Rejected:* keeping a fixed-0.5 `Answer` property; no
  helper at all.
- **`Confidence` is optional (`double?`) on choice and score only**, as in #7764.
- **Score levels by position** (user). `AIDecisionScoreLevel(Description)` objects, probabilities
  keyed by index 0..N-1, no label in the Core answer. Fixes a real bug: label-keyed
  probabilities collapsed when two levels shared wording. Automate still outputs the nearest
  level's label. *Rejected:* also returning the label from Core.
- **Strict provider-answer checks** in `AIErrorClassifyingDecisionClient` (inside tracking):
  complete distributions, sum within `max(0.02, 0.005 × count)`, choice in the keys, score in
  0..N-1. The TypeSafe adapter fills omitted zero entries. Tolerance and Jev's behavior to be
  confirmed live.
- **Automate yes/no gets a `Threshold` setting** (0..1, default 0.5), and drops its derived
  `Confidence` output.
- **Kept as is** (the M.E.AI designs disagree or it's unsettled): option `Key` naming,
  `Choice`, text state, feature vectors, enum binding, precision metadata.
- **Undecided ≠ no** (feedback point 1) and **provenance** (point 5) need no change: failures
  already throw typed `AIProviderException`s (with a `Transient` category), and `ModelId` is
  the concrete build.
- **Automate gets a fourth action, "Ask questions"** (user, 02-10-2026), batching several
  questions about one Context into one call. Its question list is a `uui-ref-node` picker with
  a pick-kind modal then a config modal per question, like choosing and configuring Automate
  actions and like the guardrail rule / test grader builders (user's call over an inline
  repeater). Outputs are keyed by question alias through Automate's dynamic output schema. The
  three single-question actions stay, since only they can bind the question text itself.

## 02-10-2026 — Sequenced during `umb-plan` (rework)

- **Rework sequencing.** T29 changes `IAIDecisionClient`, so TypeSafe, Web, Agent and Automate
  stop compiling until T31/T32/T34/T35 land. Tasks commit locally in order; the branch is only
  pushed after T35, so every pushed state builds. *Rejected:* a temporary adapter keeping the
  old `AskAsync` contract alive (throwaway code on an unreleased experimental API).
- **T36 (question-list editor) has no Core dependency** and runs alongside group E; T37 (the
  action) waits on both T35 (shared Automate composer/action files) and T36 (value shape).
- **T38 decides two open numbers live:** whether Jev omits zero-probability entries, and
  whether the `max(0.02, 0.005 × count)` tolerance fits its rounding. If not, T30/T31 are
  adjusted before T39.

## 02-10-2026 — Found during `umb-build-loop` (rework)

- **Every question in a request needs an id, even a single one** (orchestrator, T29 review).
  `GetDecisionResponseAsync` rejects a blank or duplicate id with `ArgumentException`;
  `AskAsync` assigns one (a GUID) before validation, so its callers never set one. *Rejected:*
  allowing a null id on a one-question request, since `Answers` is keyed by id and the answer
  check would have nothing to match.
- **`AskAsync` gives an id-less question an id with one base-class `MemberwiseClone`**
  (`AIDecisionQuestion.WithId`), not a per-kind copy, so custom subclasses and future
  properties survive. A whitespace id is not replaced; validation rejects it.
- **Interim Web mapping (T29 only):** until T32 reworks the wire shape, `decision/ask` keeps
  its old response fields, deriving binary `answer`/`confidence` from `TrueProbability`, score
  `level` by rounding, and reporting a missing choice/score confidence as 0. Not pushed.
- **Sum tolerance stays `max(0.02, 0.005 × count)` until T38** (orchestrator, T30 review). At
  255 options it allows up to 1.275, so the sum check barely bites on large choices. That's
  the honest bound for two-decimal rounding; if T38 shows Jev returns full-precision values, cap
  it (e.g. `min(…, 0.05)`) then. Completeness and range checks still apply at every size.
- **Custom question subclasses get the type-agnostic answer checks only** (ranges, confidence,
  sum); key/option/level checks need the built-in question types.
- **The TypeSafe adapter passes through everything Jev returns** (T31 review): extra choice
  keys, out-of-range score indexes and answers for unasked ids are kept, and only missing
  entries are filled with 0, so Core's answer checks can reject them. *Rejected:* dropping
  them in the adapter, which would hide e.g. a switch to 1-based score indexes behind a
  plausible-looking distribution. Unasked answers are typed by Jev's own `type` field.
- **Jev documents no question-count limit**, so TypeSafe enforces none; per-question limits
  (255 options, 10 levels) are already Core's. Choice/score `confidence` is read as optional.
- **T34 and T35 run before T32/T33** (orchestrator). T32 regenerates the OpenAPI client
  against the running demo site, and the demo site can't build until Agent (T34) and Automate
  (T35) compile again. The push after T35 is still safe: Web keeps its T29 interim mapping.
- **Automate yes/no `Threshold` is checked in `ExecuteAsync`** (T35), not with DataAnnotations:
  Automate only infers required-ness from attributes, and saved settings can skip the editor.
  NaN, below 0 and above 1 fail as `Validation` before any provider call. Score `Level` is the
  nearest level's label, rounding half away from zero.
- **The question config modal embeds editors directly** (T36): the key/value list element and
  CMS `umb-input-multiple-text-string`, inside `umb-property-layout`, as the guardrail rule
  config modal does. `<umb-property>` needs a property dataset context a standalone modal
  doesn't have. Rows are edited by index, since the alias is editable. List limit config is
  `max` (default 20), matching the key/value and multiple-text-string editors.
- **"Ask questions" keeps only Automate-owned checks** (T37 review): 1..20 questions, alias
  format/uniqueness, known kind, threshold. Option/level bounds are Core's, surfacing as a
  mapped `ArgumentException` (now a repo gotcha, `consumers-dont-copy-core-validation`). The
  output schema skips questions with an invalid alias and renders confidence as
  `["number","null"]`, matching the single actions' generated schema.
- **T38 settled the open numbers** (live, real Jev): Jev returns a dense distribution, every
  option and level, two decimals, summing to exactly 1.0 even at 255 options. So the adapter's
  zero-fill is a harmless safeguard, and the `max(0.02, 0.005 × count)` tolerance stays as is;
  no cap needed.

## 05-10-2026 — Pluggable agent selection

- **Copilot auto-mode Decision routing becomes a `DecisionAgentSelector`** in Agent's new
  selector chain (#463, on v18/dev and v17/dev), registered by default before `LLMAgentSelector`
  (user). It returns null to defer whenever Decision can't answer, so behavior matches the
  previous in-service branch. `StickyAgentSelector`'s guidance changes to register it first
  (`Insert<StickyAgentSelector>()`), since `InsertBefore<LLMAgentSelector>` would now place it
  after Decision and Decision would override it (user). *Rejected:* keeping the old guidance
  (sticky silently loses while Decision is set up); registering the Decision selector opt-in
  only (changes behavior for sites already routing with Decision).
- **`DecisionAgentSelector` is not marked `[Experimental]`** (T44 review), matching the public
  Automate Decision actions: it only touches Decision types internally (per-file `#pragma`), and
  constructing it directly already warns through its `IAIDecisionService` parameter.

## 06-10-2026 — M.E.AI follow-up (dotnet/extensions#7764 comments, #7795, #7796)

- **Option keys are documented as opaque and exact** (user). `AIDecisionOption.Key` now says
  providers must return it exactly as sent (ordinal, no case folding or relabelling); the
  checker already rejects anything else. Matches the normative identity point on #7764.
- **Watched, not built** (user). Each stays out until its trigger:
  - **Separate reference slot** (policy/rubric apart from the judged `State`). *Revisit when:*
    #7795 or its successor adds one, or a second provider supports one natively.
  - **"Undecided" as its own outcome** (abstention, per question in a batch), distinct from a no
    and from a failure. Today failures are typed exceptions and graders count them as errors.
    *Revisit when:* M.E.AI defines an answer status, or a provider can abstain.
  - **Provider limits beyond kinds** (`SupportedKinds`, max questions/options/levels). *Revisit
    when:* we add a second Decision provider, or one that lacks a kind.
  - **Multimodal decision input** (images/documents alongside `State`, e.g. judging a media
    item). *Revisit when:* #7795's input contract settles on AIContent or similar, or Jev
    accepts images.
  - **Decision as an agent tool** (#7796's `AsAIFunction`): a possible future Copilot tool;
    new scope, not alignment.
