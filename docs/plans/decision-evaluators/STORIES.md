# Stories

Derived from `SPEC.md`. Story ids `DE-n` ("decision evaluators") are referenced by specs and
`PLAN.md`. Don't renumber.

Same Definition of Ready/Done as `decision-capability-release`:

> **Ready:** role, capability, value stated; Given/When/Then covers the happy path;
> out-of-scope explicit.
> **Done:** every AC passes as an executable spec (happy and sad path);
> `dotnet build Umbraco.AI/Umbraco.AI.slnx` and its tests pass; every real entry point proven
> through a wire task on the demo site; reviewer PASS; v17 port tracked (DE-6).

"Flag on/off" = `Umbraco:AI:Experimental:Decision`. "Judge" = the Decision Safety Judge
evaluator (DE-1) or the Decision Judge grader (DE-2). "Probability" =
`AIBinaryDecisionResponse.Probability`.

---

## DE-1 — Guard AI output with a Decision Safety Judge

As a **backoffice admin setting up guardrails**,
I want a guardrail rule that asks a Decision yes/no question about the response,
so that unsafe content is flagged by a calibrated answer instead of a number scraped from
chat text.

Covers SPEC DG-1 to DG-9, SH-1.

**Happy path**

- AC1 — asks a binary question about the content
  Given the flag is on
  When the evaluator evaluates `"some content"` with criteria `"No profanity"`
  Then it asks exactly one `AIBinaryDecisionQuestion`
- AC2 — content goes in Context
  Given the flag is on
  When it evaluates `"some content"`
  Then the question's `Context` is `"some content"`
- AC3 — criteria go in Instructions
  Given the flag is on and criteria `"No profanity"`
  When it evaluates content
  Then the question's `Instructions` contain `"No profanity"`
- AC4 — configured profile used
  Given `ProfileId` is set to a profile id
  When it evaluates content
  Then the Decision call is made against that profile id
- AC5 — default profile when none configured
  Given `ProfileId` is empty
  When it evaluates content
  Then the Decision call names no profile (the service's default Decision profile applies)
- AC6 — safe answer passes
  Given probability 0.9 and threshold 0.7
  When it evaluates content
  Then `Flagged` is false
- AC7 — unsafe answer flags
  Given probability 0.4 and threshold 0.7
  When it evaluates content
  Then `Flagged` is true
- AC8 — boundary is not flagged
  Given probability 0.7 and threshold 0.7
  When it evaluates content
  Then `Flagged` is false
- AC9 — score is the probability
  Given probability 0.4
  When it evaluates content
  Then `Score` is 0.4
- AC10 — reason names probability and threshold when flagged
  Given probability 0.4 and threshold 0.7
  When it evaluates content
  Then `Reason` contains `"0.40"` and `"0.70"`
- AC11 — no reason when not flagged
  Given probability 0.9
  When it evaluates content
  Then `Reason` is null
- AC12 — metadata carries the answer detail
  Given probability 0.4, model `jev-1`, threshold 0.7
  When it evaluates content
  Then `Metadata` has `probability` 0.4, `answer` false, `confidence` 0.6, `threshold` 0.7,
  `modelId` `"jev-1"`
- AC13 — evaluator id
  When it evaluates content
  Then `EvaluatorId` is `"decision-judge"`
- AC14 — defaults
  Given no config
  Then the threshold is 0.7 and the criteria equal the LLM Safety Judge's default criteria
- AC15 — config fields
  When the evaluator's config schema is read
  Then it has three fields in order: Profile (profile picker, `capability` = `Decision`),
  Evaluation Criteria (text area), Safety Threshold (slider)
- AC16 — registered as model-based
  When the evaluator collection is built
  Then it contains `decision-judge` with type `ModelBased`

**Sad path**

- AC17 — flag off flags without calling Decision
  Given the flag is off
  When it evaluates content
  Then `Flagged` is true
- AC18 — flag off makes no call
  Given the flag is off
  When it evaluates content
  Then `IAIDecisionService` is not called
- AC19 — flag off reason
  Given the flag is off
  When it evaluates content
  Then `Reason` says Decision is turned off and names `Umbraco:AI:Experimental:Decision`
- AC20 — flag off score
  Given the flag is off
  Then `Score` is 0
- AC21 — Decision error flags
  Given the Decision call throws `InvalidOperationException("No default Decision profile")`
  When it evaluates content
  Then `Flagged` is true
- AC22 — Decision error reason carries the message
  Given the same error
  Then `Reason` contains `"No default Decision profile"`
- AC23 — Decision error score
  Given the same error
  Then `Score` is 0
- AC24 — cancellation propagates
  Given the caller's token is cancelled and the Decision call throws
  `OperationCanceledException`
  When it evaluates content
  Then `EvaluateAsync` throws `OperationCanceledException`

Out of scope: redaction, conversation history, LLM fallback, TrueCriteria/FalseCriteria.

---

## DE-2 — Grade AI test output with a Decision Judge

As a **backoffice admin writing AI tests**,
I want a grader that asks a Decision yes/no question about the test output,
so that subjective checks pass or fail on a calibrated answer instead of parsed chat text.

Covers SPEC DJ-1 to DJ-9, SH-1.

**Happy path**

- AC1 — asks a binary question about the output
  Given the flag is on and output `"hello"`
  When the grader grades
  Then it asks exactly one `AIBinaryDecisionQuestion`
- AC2 — output goes in Context
  Given output `"hello"`
  Then the question's `Context` is `"hello"`
- AC3 — null output becomes empty context
  Given the outcome's `OutputValue` is null
  Then the question's `Context` is `""`
- AC4 — criteria go in Instructions
  Given criteria `"Is polite"`
  Then the question's `Instructions` contain `"Is polite"`
- AC5 — configured profile used
  Given `ProfileId` is set
  Then the Decision call is made against that profile id
- AC6 — default profile when none configured
  Given `ProfileId` is empty
  Then the Decision call names no profile
- AC7 — good answer passes
  Given probability 0.9 and threshold 0.7
  Then `Passed` is true
- AC8 — bad answer fails
  Given probability 0.4 and threshold 0.7
  Then `Passed` is false
- AC9 — boundary passes
  Given probability 0.7 and threshold 0.7
  Then `Passed` is true
- AC10 — score is the probability
  Given probability 0.4
  Then `Score` is 0.4
- AC11 — result fields
  Given output `"hello"`, criteria `"Is polite"`, grader config id G
  Then `ActualValue` is `"hello"`, `ExpectedValue` is `"Is polite"`, `GraderId` is G
- AC12 — failure message names score and threshold
  Given probability 0.4 and threshold 0.7
  Then `FailureMessage` contains `"0.40"` and `"0.70"`
- AC13 — no failure message when passed
  Given probability 0.9
  Then `FailureMessage` is null
- AC14 — metadata carries the answer detail
  Given probability 0.4, model `jev-1`, threshold 0.7
  Then `Metadata` has `probability`, `answer`, `confidence`, `threshold`, `modelId`
- AC15 — defaults
  Given no config
  Then the threshold is 0.7 and the criteria equal the LLM Judge's default criteria
- AC16 — config fields
  When the grader's config schema is read
  Then it has three fields in order: Profile (profile picker, `capability` = `Decision`),
  Evaluation Criteria (text area), Pass Threshold (slider)
- AC17 — registered as model-based
  When the grader collection is built
  Then it contains `decision-judge` with type `ModelBased`

**Sad path**

- AC18 — flag off fails without calling Decision
  Given the flag is off
  Then `Passed` is false, `Score` is 0, and `IAIDecisionService` is not called
  (three specifications)
- AC19 — flag off message
  Given the flag is off
  Then `FailureMessage` says Decision is turned off and names
  `Umbraco:AI:Experimental:Decision`
- AC20 — Decision error fails
  Given the Decision call throws `InvalidOperationException("No default Decision profile")`
  Then `Passed` is false, `Score` is 0, and `FailureMessage` contains the message
  (three specifications)
- AC21 — cancellation propagates
  Given the caller's token is cancelled and the call throws `OperationCanceledException`
  Then `GradeAsync` throws `OperationCanceledException`

Out of scope: score/choice graders, LLM fallback.

---

## DE-3 — Mark an evaluator or grader as needing a capability

As a **package developer writing an evaluator or grader**,
I want to mark it as needing a capability,
so that it's hidden while that capability is turned off, without touching any controller.

Covers SPEC SH-2.

**Happy path**

- AC1 — no marker means available
  Given a type with no `[AIRequiresCapability]`
  When `AreRequiredCapabilitiesEnabled` is called
  Then it returns true
- AC2 — enabled capability means available
  Given a type marked `[AIRequiresCapability(AICapability.Decision)]` and Decision enabled
  Then it returns true
- AC3 — every listed capability must be enabled
  Given a type marked with Decision and ImageGeneration, both enabled
  Then it returns true

**Sad path**

- AC4 — disabled capability means hidden
  Given a type marked with Decision and Decision disabled
  Then it returns false
- AC5 — one disabled capability is enough to hide
  Given a type marked with Decision and ImageGeneration, only Decision enabled
  Then it returns false

---

## DE-4 — Only offer Decision judges when Decision is on

As a **backoffice admin**,
I want the Decision judges to appear in the evaluator and grader pickers only when Decision
is turned on,
so that I'm never offered something that can't run.

Covers SPEC API-1 to API-6.

**Happy path**

- AC1 — evaluator listed when on
  Given the flag is on
  When I `GET guardrail-evaluators`
  Then the list contains `decision-judge`
- AC2 — grader listed when on
  Given the flag is on
  When I `GET test-graders`
  Then the list contains `decision-judge`
- AC3 — grader by id when on
  Given the flag is on
  When I `GET test-graders/decision-judge`
  Then I get 200 with the grader
- AC4 — others unaffected
  Given the flag is off
  When I list evaluators and graders
  Then every non-Decision evaluator and grader is still listed
- AC5 — no restart needed
  Given the flag is flipped on at runtime
  When I list again
  Then `decision-judge` appears on the next request

**Sad path**

- AC6 — evaluator hidden when off
  Given the flag is off
  When I `GET guardrail-evaluators`
  Then `decision-judge` is absent
- AC7 — grader hidden when off
  Given the flag is off
  When I `GET test-graders`
  Then `decision-judge` is absent
- AC8 — grader by id 404 when off
  Given the flag is off
  When I `GET test-graders/decision-judge`
  Then I get 404
- AC9 — obsolete constructors still work
  Given a controller built through its old constructor
  When it lists
  Then it filters the same way (resolves `IAIExperimentalFeatures` via the service locator)
- AC10 — saved rules keep running
  Given the flag is off and a guardrail has a `decision-judge` rule
  When the guardrail pipeline resolves the rule's evaluator
  Then it finds it (the collection is not filtered), so the rule fails safe instead of being
  skipped

---

## DE-5 — Prove it live and document it

As a **developer evaluating Decision**,
I want the judges proven against a real TypeSafe key and documented,
so that I can trust and set them up.

Covers SPEC LV-1 to LV-4, FE-1, FE-2. Proven by wire tasks, not unit specs.

- AC1 — live guardrail: unsafe content flagged, safe content passes, in a real chat.
- AC2 — live grader: a good output passes and a bad one fails, probability shown as score.
- AC3 — flag off: neither judge in the pickers; a saved rule fails safe with the "turned off"
  reason.
- AC4 — no default Decision profile and no `ProfileId`: flags / fails with the service's
  message.
- AC5 — docs pages for both judges exist on v17 and v18 in the `ai/decision-docs` branch.

---

## DE-6 — Ship on v17 too

As a **v17 site developer**,
I want the same judges on the v17 line,
so that Decision releases with the same features on both lines.

- AC1 — v17 branch from `origin/v17/feature/decision-capability` builds and its tests pass.
- AC2 — obsolete messages say `v19`.
- AC3 — DE-5 AC1-AC4 re-proven on the v17 demo site.
- AC4 — draft PR opened against `v17/feature/decision-capability`.
