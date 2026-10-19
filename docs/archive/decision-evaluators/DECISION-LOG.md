# Decision Log

> **Status:** Archived 09-10-2026. Shipped: merged with the Decision capability into `v18/dev` (#430 via #419) and `v17/dev` (#431 via #428) on 09-10-2026.

## 25-09-2026 — Scoped during `umb-explore`

- Stacked on `v18/feature/decision-capability` (not `v18/dev`), since Decision isn't merged.
  v17 port branches from `v17/feature/decision-capability`.
- Yes/no questions for both the evaluator and the grader. A score grader was dropped: a
  "pass at level X" setting turns it back into a yes/no anyway. User choice.
- Unsure answers are handled by the threshold on the yes/no probability. No "uncertain"
  state, no separate confidence setting. User choice.
- No fallback to the LLM judges. Fail safe (flag / fail) with a clear reason, matching the
  LLM judges' error handling. A silent switch to a chat model would change cost and behavior
  without telling the admin. User choice.
- Same three settings as the LLM judges (Decision-only profile picker, criteria, threshold).
  `TrueCriteria`/`FalseCriteria` not exposed yet. User choice.
- Guardrail question is phrased as "is this content safe?", so a high probability means safe
  and it flags below the threshold, like the LLM judge's `safetyScore`. User confirmed.
- No Deploy change: rule config already travels as a blob, same as the LLM evaluator.

## 25-09-2026 — Designed during `umb-design`

- Hide the Decision evaluator/grader from the three listing endpoints at request time, but
  keep them in their collections. Rejected compose-time exclusion (the Automate approach):
  the guardrail pipeline skips unknown evaluator ids, so saved rules would fail open, and a
  flag flip would need a restart. User choice.
- A generic `[AIRequiresCapability]` marker drives the filtering, so a future capability-bound
  evaluator/grader hides with no controller change.
- Default threshold 0.7 for both, matching the LLM judges. Rejected 0.5 (a coin-flip passes).
  User choice.
- Content in the question's `Context`, criteria in `Instructions`. No conversation history,
  matching the LLM evaluator.
- No recursion guard: the Decision pipeline has no guardrail middleware.
- Cancellation is rethrown rather than turned into a flagged/failed verdict (the LLM siblings
  swallow it).
- No frontend change, no OpenAPI change, no Deploy change.

## 25-09-2026 — Sequencing decided during `umb-plan`

- The capability marker (T1) lands first; the evaluator, grader, and controller filtering
  (T2-T4) all depend on it and then run in parallel. The controller specs use a fake marked
  type, so T4 doesn't wait on T2/T3.
- One wire task (T5a) covers both judges and the listing endpoints on the demo site, since
  they share the same setup (TypeSafe connection, Decision profile, flag toggling).
- Docs (T5) and the v17 port (T6) wait for T5a, so neither is written against unproven
  behavior.
- Pending specs staged in `specs/` in this folder, same approach as
  `decision-capability-release`.

## 25-09-2026 — Found during `umb-build-loop`

- **T1: `[AIRequiresCapability]` is `Inherited = true`** (siblings like `[AIGuardrailEvaluator]` are
  not), so a subclass can't silently drop its base type's requirement. Locked in by a spec.
- **T1: added a `this Type` overload** of `AreRequiredCapabilitiesEnabled` beside the planned
  `this object` one, so callers without an instance can check a type. Reviewer suggestion.
- **T1: the attribute only hides from listings; it never blocks execution.** Its docs say so, and
  that implementations must check the flag themselves to fail safe.
- **T2: the judges pin `TrueCriteria`/`FalseCriteria` internally** (private constants, not settings), so
  "yes = safe/meets criteria" doesn't flip when an admin writes criteria like "Flag anything that...".
  The three user-facing settings are unchanged. Applies to the grader too (T3).
- **T2: Instructions say "the content provided as context"**, not "the following content", so they
  don't assume where a provider places `Context`.
- **T2: only the caller's own cancellation is rethrown.** Any other `OperationCanceledException` (e.g. a
  provider timeout) fails safe like other errors.
- **T3: known, not fixed here: a negated grader turns a fail-safe into a pass.** `AITestRunner` flips
  `Passed` when a grader has `Negate = true`, so "Decision is turned off" or an error becomes a pass.
  `LLMJudgeGrader` has the same problem on errors. Pre-existing runner behavior; raised with the user.
- **T3: cancellation stops at the test runner.** The grader rethrows the caller's cancellation, but
  `AITestRunner` catches all exceptions per grader and records a failed result. Pre-existing.
- **T4: obsolete-constructor specs swap `StaticServiceProvider.Instance`** inside one shared
  non-parallel xUnit collection (`StaticServiceProviderTestCollection`), restored in `Dispose`. The
  `controller-ctor-change-keep-obsolete` memory note now points at this pattern.
- **T5a: turning the Decision flag off with a saved Block rule blocks every chat reply** on that guardrail. This
  is the chosen fail-safe working as designed; the docs should say so plainly.
- **Negate issue logged as umbraco/Umbraco.AI#429** (user decision, 25-09-2026): fixed separately for every
  grader, not in this feature. The grader docs warn about it meanwhile.
- **`this object` overload of `AreRequiredCapabilitiesEnabled` removed before shipping** (user decision, 25-09-2026,
  raised in PR review): it attached to every object for anyone importing `Umbraco.AI.Extensions`, and as stable
  public API it would be hard to remove later. Only the `this Type` overload remains; callers pass `GetType()`.

## 05-10-2026 — Upgraded to the reworked Decision API (merge of decision-capability rework)

Rebased onto `v18/feature/decision-capability` after its 02-10-2026 M.E.AI-aligned rework (see
that feature's DECISION-LOG, same date). Both judges' calls and tests follow.

- `Context` is gone from the question; the judged content now goes through
  `AskAsync(..., state: <content>, ...)` (named args), matching the Automate actions
  (`AskYesNoDecisionAction` etc.) this same merge already updated.
- `AskAsync` now returns `AIDecisionResponse<AIBinaryDecisionAnswer>`. `Probability` becomes
  `Answer.TrueProbability`; the old bare `bool Answer` becomes `Answer.IsTrue(threshold)` —
  the threshold is the same one each judge already uses for `Flagged`/`Passed`, so no new
  setting needed. There is no `Confidence` on `AIBinaryDecisionAnswer` (the probability itself
  is the distribution), so the `confidence` metadata key is dropped from both judges rather
  than kept as a stale/faked field (orchestrator instruction during this upgrade).
- **Fixed, not just ported: `DecisionJudgeGrader` now sets `IsError = true`** on both its
  "Decision is turned off" branch and its catch-all exception branch (including the new
  `AIProviderException` Core now throws for an inconsistent/undecided provider answer). Before
  this upgrade neither branch set it, unlike `GuardrailGrader`/`LLMJudgeGrader`'s established
  convention — effectively the same `Negate`-flips-a-fail-safe-into-a-pass gap logged as T3's
  note above and fixed project-wide in #429, just never retrofitted onto this grader. No
  change needed on the guardrail evaluator side: `AIGuardrailResult` has no `IsError` concept,
  and flagging on any exception (already true before this upgrade) is already the fail-safe
  behavior.
- Instructions wording changed from "the content provided as context" to "the content
  provided", since `Context` is no longer a property name on the question — a doc-sync detail,
  not a behavior change.
- Not changed in this upgrade: neither judge batches multiple criteria into one Decision call
  (each still asks a single `AIBinaryDecisionQuestion`). The rework's `AIDecisionRequest` now
  supports several id'd questions over shared `state` in one call, which would let an
  evaluator/grader ask multiple criteria at once — noted as a follow-up opportunity, not done
  here to keep this upgrade to a pure API-shape migration.
