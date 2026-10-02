# Spec

## Core behavior

### Decision Safety Judge (`decision-judge` guardrail evaluator)

- **DG-1** With the flag on and a Decision profile available, it asks one
  `AIBinaryDecisionQuestion` whose `Context` is the evaluated content and whose
  `Instructions` contain the configured `EvaluationCriteria`.
- **DG-2** It uses `ProfileId` when set, and the default Decision profile when empty.
- **DG-3** `Flagged` is true exactly when `Probability < SafetyThreshold`. `Score` equals
  `Probability`. `EvaluatorId` is `decision-judge`.
- **DG-4** When flagged, `Reason` names the probability and threshold
  (e.g. "Safety probability 0.42 below threshold 0.70"). When not flagged, `Reason` is null.
- **DG-5** `Metadata` holds `probability`, `answer`, `confidence`, `threshold`, `modelId`.
- **DG-6** With the flag off, it returns flagged, `Score = 0`, a reason that says Decision is
  turned off, and makes no Decision call.
- **DG-7** If the Decision call throws (no default profile, not a Decision profile, provider
  error), it returns flagged, `Score = 0`, and a reason containing the exception message. It
  never throws out of `EvaluateAsync`, except:
- **DG-8** When the caller's token is cancelled, the `OperationCanceledException` propagates.
- **DG-9** Defaults: threshold 0.7, criteria the same text as the LLM evaluator's default.

### Decision Judge (`decision-judge` test grader)

- **DJ-1** With the flag on, it asks one `AIBinaryDecisionQuestion` whose `Context` is
  `outcome.OutputValue` (empty string when null) and whose `Instructions` contain
  `EvaluationCriteria`.
- **DJ-2** Profile: `ProfileId` when set, else the default Decision profile.
- **DJ-3** `Passed` is true exactly when `Probability >= PassThreshold`. `Score` equals
  `Probability`. `ActualValue` is the output, `ExpectedValue` is the criteria, `GraderId`
  is the grader config's id.
- **DJ-4** When failed, `FailureMessage` names the score and threshold. When passed, null.
- **DJ-5** `Metadata` holds `probability`, `answer`, `confidence`, `threshold`, `modelId`.
- **DJ-6** Flag off → failed, `Score = 0`, message says Decision is turned off, no call.
- **DJ-7** Decision call throws → failed, `Score = 0`, message contains the exception
  message. Never throws out of `GradeAsync`, except:
- **DJ-8** caller-token cancellation propagates.
- **DJ-9** Defaults: threshold 0.7, criteria the same text as the LLM grader's default.

### Shared

- **SH-1** Both configs show three fields in this order: Profile (profile picker, Decision
  profiles only), Evaluation Criteria (text area), threshold (slider 0-1, step 0.1).
- **SH-2** `[AIRequiresCapability]` on a type makes the `Type` overload of
  `AreRequiredCapabilitiesEnabled` return false while any listed capability is disabled,
  and true when all are enabled or none are listed.

## Management API surface

No new routes or DTOs. Behavior change on three existing routes (all under the existing
`SectionAccessAI` policy):

- **API-1** `GET {AI management API}/v1/guardrail-evaluators`: leaves out any
  evaluator whose required capabilities aren't all enabled. With the Decision flag off,
  `decision-judge` is absent. With it on, present, with its three config fields.
- **API-2** `GET .../test-graders`: same rule. `decision-judge` absent when the flag is off.
- **API-3** `GET .../test-graders/{id}`: returns 404 for a hidden grader, exactly as for an
  unknown id.
- **API-4** Every other evaluator and grader is listed exactly as before, flag on or off.
- **API-5** Toggling the flag at runtime (config reload) changes the lists on the next
  request, with no restart.
- **API-6** OpenAPI output is unchanged.

## Frontend components

None changed.

- **FE-1** (observable, no code change) With the flag on, "Decision Safety Judge" appears in
  the guardrail rule evaluator picker and "Decision Judge" in the test grader picker, and
  their config forms show the three fields, with the profile picker listing only Decision
  profiles.
- **FE-2** (observable, no code change) With the flag off, a guardrail that already has a
  `decision-judge` rule still loads, showing the rule under its id.

## Live checks (demo site, TypeSafe key)

- **LV-1** A guardrail with a Decision Safety Judge rule (Block) flags a clearly unsafe
  response and passes a safe one, in a real chat.
- **LV-2** An AI test with a Decision Judge grader passes a good output and fails a bad one,
  with the probability shown as the score.
- **LV-3** Flag off: neither appears in the pickers; a saved rule fails safe with the
  "turned off" reason.
- **LV-4** No default Decision profile and no `ProfileId`: the rule flags / the grader fails
  with the service's "no default Decision profile" message.
