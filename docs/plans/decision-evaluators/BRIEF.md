# Brief

## Problem

Umbraco AI has two places where a model judges content, and both use a chat model today:

- **Guardrails:** `LLMGuardrailEvaluator` ("LLM Safety Judge") prompts a chat model for a JSON
  `safetyScore`, parses the text, and flags the content when the score is under a threshold.
- **Tests:** `LLMJudgeGrader` ("LLM Judge") does the same for a test output, and passes when
  the score is at or over a threshold.

Both ask a chat model to invent a number and then scrape it out of free text. Parsing can
fail (both treat that as flagged/failed), the number isn't calibrated, and a chat model is an
expensive tool for a yes/no call.

The experimental Decision capability (`v18/feature/decision-capability`, draft PR #419; v17
port draft PR #428, neither merged; see `docs/plans/decision-capability-release/`) gives a
typed, calibrated answer instead: `IAIDecisionService.AskAsync` with an
`AIBinaryDecisionQuestion` returns an `AIBinaryDecisionResponse` with a real `Probability`
(0 to 1). This feature adds the Decision equivalents of the two LLM judges:

- a **Decision Guardrail Evaluator**, and
- a **Decision Test Grader**.

### Who it's for

- **Backoffice admins** who set up guardrails and AI tests, and already know the LLM judges.
  They pick the Decision version from the same evaluator/grader lists and fill in the same
  kind of settings.
- **Developers** who enabled Decision and want a cheaper, typed judge in their guardrails and
  test suites.

Not for: sites that haven't turned on `Umbraco:AI:Experimental:Decision`. For them nothing
changes and neither new option appears.

### Why now

Decision is being built for release now, on both lines. The guardrail and test judges are
the most natural "is this OK?" consumers already in Core, and they prove the Decision API
fits a second internal use beyond Copilot auto mode. Stacking on the unmerged feature branch
lets them ship in the same release round.

### Behavior (decided in explore)

- **Question kind: yes/no for both.** The guardrail asks "is this content safe by these
  criteria?" The grader asks "does this output meet these criteria?" A score grader was
  considered and dropped: with a "pass at level X" setting it collapses back into a yes/no.
- **The probability is the score.** The guardrail flags when the "safe" probability is below
  the threshold, the same direction as the LLM judge's `safetyScore`. The grader passes when
  the probability is at or above the threshold. The result's `Score` field carries the
  probability.
- **Unsure answers are handled by the threshold.** No "uncertain" state and no separate
  confidence setting. A 0.55 "safe" answer is flagged by a 0.7 threshold.
- **No fallback to the LLM judges.** If Decision can't answer (no Decision profile set,
  flag turned off after setup, provider error), the guardrail flags and the test fails, with
  a clear reason (e.g. "No default Decision profile is set"). This matches how the LLM judges
  treat errors today. Someone who picked the Decision judge on purpose shouldn't get a
  silently different judge with a different cost.
- **Settings: the same three as the LLM judges.** A profile picker that lists only Decision
  profiles (empty means the default Decision profile), a criteria text area, and the
  threshold slider. The binary question's `TrueCriteria`/`FalseCriteria` hints are not
  exposed for now.

### Success looks like

1. With the Decision flag on and a Decision profile set up, an admin adds a "Decision" rule
   to a guardrail, and it flags unsafe content and passes safe content, live against
   TypeSafe/Jev, both in a real chat and in the guardrail test path.
2. An admin adds a "Decision" grader to an AI test, runs it, and sees pass/fail with the
   probability as the score, live against Jev.
3. With the flag off (the default), neither appears in the evaluator or grader lists, and
   nothing else changes.
4. A misconfigured judge (no Decision profile, flag turned off with rules still saved)
   flags/fails with a reason that says what to fix. It never throws out of the guardrail or
   test pipeline.
5. Usage is tracked like any other Decision call (the service already does this).
6. Ships on v18 and v17 together (v17 port from `v17/feature/decision-capability`), as
   draft PRs stacked on the Decision PRs.
7. Docs pages for both in the unpushed Decision docs branch
   (`Umbraco.Docs-worktrees/ai-decision-docs`, branch `ai/decision-docs`), v17 and v18,
   next to the existing LLM judge docs.

### Constraints

- **Experimental.** Code touching Decision types suppresses `UMBRACOAI_DECISION` per file
  with `#pragma`, never `NoWarn`. Hidden when `Umbraco:AI:Experimental:Decision` is off.
- **Stacked on unmerged work.** Branches off `v18/feature/decision-capability` and
  `v17/feature/decision-capability`. If those change before merge, this rebases/merges
  along.
- **Public API rules** still apply to anything outside the experimental surface. A public
  controller constructor change keeps the old one as `[Obsolete]` ("Will be removed in v20"
  on v18, v19 on v17) and marks the new one `[ActivatorUtilitiesConstructor]`.
- **Targets:** `net10.0`, CMS 18.x on v18 and CMS 17.x on v17.

### Checked during explore

- **Deploy: no change.** A guardrail rule's config (including a profile id) travels as one
  JSON blob in `AIGuardrailArtifact`, with no profile dependency tracked. The LLM evaluator
  has the same behavior, so there's no new gap. Tests aren't deployed.
- **Evaluator and grader lists aren't flag-aware today.** `AllGuardrailEvaluatorsController`
  and `AllTestGradersController` map their whole collection. Something has to hide the
  Decision entries when the flag is off. How is a design question.
- **The profile picker already filters by capability** (`Uai.PropertyEditorUi.ProfilePicker`
  takes a `capability` config value).

### Riskiest unknowns

1. **How to hide them when the flag is off.** Compose-time exclusion (like the Automate
   actions) vs. runtime filtering in the collections/controllers. The flag is read through
   `IOptionsMonitor`, so it can change at runtime. Also: what the guardrail/test editors show
   for an already-saved rule whose evaluator is now hidden. TODO: design.
2. **Recursion guard.** `LLMGuardrailEvaluator` sets `IsGuardrailEvaluation` on the runtime
   context so its own chat call doesn't trigger guardrails again. Unknown whether a Decision
   call can pass through guardrail middleware at all, so whether the new evaluator needs the
   same guard. TODO: design.
3. **Guardrail content length and shape.** The evaluator gets the full response text plus
   conversation history. Unknown whether Jev has an input size limit, and whether the history
   should go in the question's `Context`. TODO: design.
4. **Profile picker "Decision" capability value.** Whether the picker's `capability` filter
   already accepts `Decision`, and what it does when the flag is off. TODO: design.
5. **v17 differences** in the guardrail/test APIs between lines. TODO: check at port time.

### Smallest version worth shipping

Both judges, on both lines, with docs. Each is one class plus flag-hiding, so splitting them
saves little.

## Non-goals

- **A score (or pick-one) grader.** A pass level turns it back into a yes/no. Can come later
  if someone asks.
- **An "uncertain" result state** or a separate confidence setting. The threshold covers it.
- **Falling back to the LLM judges.** Fails safe with a reason instead.
- **Exposing `TrueCriteria`/`FalseCriteria`.** Can be added later without breaking anything.
- **Changing the LLM judges.** They stay as they are.
- **Deploy changes.** None needed (see above).
- **Redaction.** The Decision evaluator only flags. It doesn't implement
  `IAIRedactableGuardrailEvaluator`.
