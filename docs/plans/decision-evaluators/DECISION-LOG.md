# Decision Log

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
