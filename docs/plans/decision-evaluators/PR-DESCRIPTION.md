Stacked on #419 | [Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v18/feature/decision-evaluators/docs/plans/decision-evaluators) | v17 port: #431 | Follow-up: #429

## Why the change

Guardrails and AI tests can now judge content with a typed, calibrated Decision yes/no answer instead of asking a chat model for a number and parsing it out of free text.

## Special things to note

- `AreRequiredCapabilitiesEnabled` is a `this Type` extension only. An earlier `this object` overload was dropped before shipping so the extension doesn't attach to every object.
- The judges pin the yes/no meaning internally: fixed, non-configurable `TrueCriteria`/`FalseCriteria` ("meets every criterion and is safe" vs "breaks at least one criterion"), so an admin writing criteria like "Flag anything that..." can't flip what "yes" means. The three user-facing settings are unchanged. Found in review, not in the original design.
- `[AIRequiresCapability]` is `Inherited = true` (siblings like `[AIGuardrailEvaluator]` aren't), so a subclass can't drop its base type's requirement. Locked in by a spec.
- Only the caller's own cancellation is rethrown. Any other `OperationCanceledException`, such as a provider timeout, fails safe like other errors. The LLM judges swallow all cancellation.
- **Turning the Decision flag off with a saved Block rule blocks every response on that guardrail.** That's the fail-safe design (no fallback to the LLM judge), confirmed live. The docs say so plainly.
- A negated grader turns a fail-safe failure (e.g. "Decision is turned off") into a pass. This is existing test-runner behavior and hits `llm-judge` too, so it's tracked separately in #429 and the grader docs warn about it.
- The obsolete-constructor specs swap `StaticServiceProvider.Instance` inside one shared non-parallel xUnit collection (`StaticServiceProviderTestCollection`) and restore it in `Dispose`. The `controller-ctor-change-keep-obsolete` memory note now points at this pattern.
- Public API: three Management API controllers gain an `IAIExperimentalFeatures` constructor parameter. The old constructors are kept `[Obsolete("... Will be removed in v20.")]` and the new ones are `[ActivatorUtilitiesConstructor]`. No route, DTO, or OpenAPI change, so no client regeneration.
- No Deploy, migration, or frontend change. Rule and grader config are the existing JSON blobs.
- Live-checked on the v18 and v17 demo sites against TypeSafe (`jev-1.13.0`): listings with the flag on and off, safe/unsafe evaluation, a real chat blocked by a Decision rule, real agent test runs, no default profile, and flag off. Details are in `BUILD-LOG.md`. A runtime flag flip without a restart is covered by unit specs only.
- Docs: a local commit on the unpushed Umbraco.Docs `ai/decision-docs` branch, next to the LLM judges on v17 and v18.

## Change outline

Four new Core types and one generic marker; three existing listing endpoints learn to filter.

```diff
 Umbraco.AI/src/
 ├── Umbraco.AI.Core/
+│   ├── Models/AIRequiresCapabilityAttribute.cs        # "hide me from listings while capability X is off"
+│   ├── Extensions/AIRequiresCapabilityExtensions.cs   # AreRequiredCapabilitiesEnabled(this Type, flags)
 │   ├── Guardrails/Evaluators/
+│   │   └── DecisionGuardrailEvaluator.cs              # "decision-judge" — Decision Safety Judge (+ config)
 │   └── Tests/Graders/
+│       └── DecisionJudgeGrader.cs                     # "decision-judge" — Decision Judge (+ config)
 └── Umbraco.AI.Web/Api/Management/
     ├── Guardrail/Controllers/AllGuardrailEvaluatorsController.cs   # filters by marker
     └── Test/Controllers/
         ├── AllTestGradersController.cs                             # filters by marker
         └── ByIdTestGraderController.cs                             # hidden grader → 404
```

What each judge does per call (the grader is the same shape, with `Passed = probability >= threshold`):

```
DecisionGuardrailEvaluator.EvaluateAsync(content, _, config)
  if Decision flag off          → Flagged, Score 0, "Decision is turned off (...)"   (no call)
  question = AIBinaryDecisionQuestion
      Instructions = "Decide whether the content provided as context is safe and compliant,
                      based on these criteria:\n\n{EvaluationCriteria}"
      Context      = content
      True/FalseCriteria = pinned constants
  response = IAIDecisionService.AskAsync(b => b.WithAlias("guardrail-decision-evaluator")
                                              .WithProfile(ProfileId?), question)
  Flagged = response.Probability < SafetyThreshold (default 0.7)
  Score   = response.Probability
  Metadata = { probability, answer, confidence, threshold, modelId }
  on caller cancellation        → rethrow
  on any other exception        → Flagged, Score 0, "Decision safety evaluation failed: {message}"
```

Settings mirror the LLM judges, with a Decision-only profile picker:

```diff
-ProfileId            "Judge Profile ID"   ProfilePicker (any profile)
+ProfileId            "Profile"            ProfilePicker, capability: Decision (empty = default Decision profile)
 EvaluationCriteria   TextArea             same default text as the LLM sibling
-SafetyThreshold / PassThreshold  Slider 0–1, default 0.7   (compared to a parsed chat "score")
+SafetyThreshold / PassThreshold  Slider 0–1, default 0.7   (compared to the Decision probability)
```

Hiding while the flag is off happens per request at the listing endpoints, not in the collections. The guardrail pipeline skips evaluator ids it can't find, so filtering the collection would make saved rules fail open.

```diff
 GET guardrail-evaluators
-  _evaluators → map
+  _evaluators.Where(e => e.GetType().AreRequiredCapabilitiesEnabled(_experimentalFeatures)) → map

 GET test-graders/{id}
-  grader is null → 404
+  grader is null || !grader.GetType().AreRequiredCapabilitiesEnabled(_experimentalFeatures) → 404

 AIGuardrailChatClient (unchanged)
   _evaluators.GetById(rule.EvaluatorId)   # still finds decision-judge → evaluator fails it safe
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_017uGkW6dBRpdnra2aE3gDnX
