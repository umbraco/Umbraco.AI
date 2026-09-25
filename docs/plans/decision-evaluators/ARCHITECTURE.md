# Architecture

## Extension points

Both new types plug into existing Core extension points, discovered by attribute through
`TypeLoader` (no composer registration needed):

| New type | Extension point | Sibling it mirrors |
|----------|-----------------|--------------------|
| `DecisionGuardrailEvaluator` + `DecisionGuardrailEvaluatorConfig` | `[AIGuardrailEvaluator]` on `AIGuardrailEvaluatorBase<TConfig>`, collected by `AIGuardrailEvaluatorCollectionBuilder` | `LLMGuardrailEvaluator` |
| `DecisionJudgeGrader` + `DecisionJudgeGraderConfig` | `[AITestGrader]` on `AITestGraderBase<TConfig>`, collected by `AITestGraderCollectionBuilder` | `LLMJudgeGrader` |

Files:

- `Umbraco.AI/src/Umbraco.AI.Core/Guardrails/Evaluators/DecisionGuardrailEvaluator.cs`
  (config class in the same file, like the LLM sibling)
- `Umbraco.AI/src/Umbraco.AI.Core/Tests/Graders/DecisionJudgeGrader.cs` (same)
- `Umbraco.AI/src/Umbraco.AI.Core/Models/AIRequiresCapabilityAttribute.cs` (new, generic)
- `Umbraco.AI/src/Umbraco.AI.Core/Extensions/AIRequiresCapabilityExtensions.cs` (new, generic)

Both classes use `IAIDecisionService` and suppress `UMBRACOAI_DECISION` with a per-file
`#pragma warning disable UMBRACOAI_DECISION`, never `NoWarn`.

| | Guardrail evaluator | Test grader |
|--|--|--|
| Id | `decision-judge` | `decision-judge` |
| Name | Decision Safety Judge | Decision Judge |
| Type | `AIGuardrailEvaluatorType.ModelBased` | `AIGraderType.ModelBased` |
| Question | `AIBinaryDecisionQuestion`: "is this content safe and compliant according to the criteria?" | `AIBinaryDecisionQuestion`: "does this output meet the criteria?" |
| `Instructions` | fixed lead-in + `EvaluationCriteria` | fixed lead-in + `EvaluationCriteria` |
| `Context` | the content being evaluated | `outcome.OutputValue` |
| Result | `Flagged = Probability < SafetyThreshold` | `Passed = Probability >= PassThreshold` |
| `Score` | `Probability` | `Probability` |
| Decision alias | `guardrail-decision-evaluator` | `test-decision-judge-grader` |

Config (both): `ProfileId` (`Guid?`, `Uai.PropertyEditorUi.ProfilePicker` with
`capability: Decision`, empty = default Decision profile), `EvaluationCriteria` (text area,
same defaults as the LLM siblings), threshold (`Umb.PropertyEditorUi.Slider` +
`SliderDoubleJsonConverter`, 0-1, step 0.1, default **0.7**). Field names match the LLM
siblings (`SafetyThreshold` / `PassThreshold`).

The call goes through the builder overload
`AskAsync(b => b.WithAlias(...).WithProfile(id?), question, ct)`, so the call is named in
tracking/audit like the LLM siblings' chat calls.

## Hiding when the flag is off

A new generic marker, `[AIRequiresCapability(AICapability.Decision)]`, goes on both classes
(`AllowMultiple = true`, class targets, not experimental: it names no Decision type).
`AIRequiresCapabilityExtensions.AreRequiredCapabilitiesEnabled(this Type, IAIExperimentalFeatures)`
reads the attributes from the type and returns true when every required capability
is enabled (true when there are none).

Three listing controllers filter with it at request time:

- `AllGuardrailEvaluatorsController` (`GET guardrail-evaluators`)
- `AllTestGradersController` (`GET test-graders`)
- `ByIdTestGraderController` (`GET test-graders/{id}`, 404 when hidden)

The collections themselves are **not** filtered. The guardrail pipeline
(`AIGuardrailChatClient`) skips an evaluator id it can't find, so removing the evaluator from
the collection would make a saved rule fail *open*. Keeping it in the collection lets the
evaluator's own flag check fail it *safe*.

## Runtime behavior

Order inside `EvaluateAsync` / `GradeAsync`:

1. Resolve config (`ResolveConfig(...) ?? new()`), as the siblings do.
2. **Flag check.** If `IsCapabilityEnabled(AICapability.Decision)` is false, return
   flagged / failed, `Score = 0`, reason "Decision is turned off
   (Umbraco:AI:Experimental:Decision), so this rule can't run." No Decision call.
3. Build the question and call `IAIDecisionService`.
4. Map the `AIBinaryDecisionResponse` to the result. `Metadata` carries `probability`,
   `answer`, `confidence`, `threshold`, `modelId` (camelCase via
   `Constants.DefaultJsonSerializerOptions`).
5. **Any exception** (except cancellation, below) → flagged / failed, `Score = 0`, reason
   `"Decision safety evaluation failed: {message}"` / `"Decision judge evaluation failed: {message}"`.
   The service's own messages already say what to fix (e.g. no default Decision profile, not
   a Decision profile).
6. **Cancellation** (`OperationCanceledException` while the caller's token is cancelled) is
   rethrown, not turned into a verdict. Deliberate deviation from the LLM siblings, which
   swallow it: a cancelled request isn't a judgment.

No LLM fallback, no "uncertain" state (see BRIEF).

## Data model & persistence

None. Rule and grader config are the existing `JsonElement` blobs on `AIGuardrailRule.Config`
and `AITestGraderConfig`.

## Connected systems

| System | Applies? | Why |
|--------|----------|-----|
| Deploy | No | Rule config travels as a blob in `AIGuardrailArtifact`, same as the LLM evaluator (no profile dependency tracked there either). Tests aren't deployed. |
| Usage tracking / audit / OTel | Already covered | `IAIDecisionService` runs the Decision middleware (tracking + OTel). Nothing new. |
| Guardrail recursion guard | No | `IsGuardrailEvaluation` only matters to `AIGuardrailChatClient`, which is chat middleware. The Decision pipeline has no guardrail middleware, so there's nothing to recurse into. |
| Management API shape | No new endpoints or DTOs | Only filtering on three existing endpoints. OpenAPI output unchanged, so no client regeneration. |
| Frontend | No change | Evaluator/grader lists render whatever the API returns. A saved rule whose evaluator is hidden already falls back to showing the raw `evaluatorId` (`rule-config-builder.element.ts`). The profile picker already supports `capability: Decision` (used by the Automate actions). |
| `GuardrailGrader` | Inherits | It runs any evaluator by id, so it can run the Decision evaluator and gets the same fail-safe behavior. |
| Extension usage telemetry | No | Counts collection entries; unaffected. |
| Localization | No | Evaluator/grader names and field labels come from C# attributes, as for every sibling. |
| Docs | Yes | Pages in `Umbraco.Docs-worktrees/ai-decision-docs` (v17 + v18). Separate task. |
| v17 | Yes | Port after v18 is verified. Obsolete messages say v19 there. |

## Key decisions

1. **Yes/no question for both** (explore). Rejected: score grader, since a pass level
   collapses it back to yes/no.
2. **Probability is the score; threshold handles unsure answers** (explore). Rejected: an
   "uncertain" state or a separate confidence setting.
3. **No fallback to the LLM judges; fail safe with a reason** (explore).
4. **Hide from the listing endpoints at request time, keep in the collections.** Rejected:
   compose-time exclusion like the Automate actions. It would make saved rules fail open (the
   guardrail pipeline skips unknown evaluator ids), and needs a restart to flip. User choice.
5. **Generic `[AIRequiresCapability]` marker** instead of a Decision-specific check in each
   controller. A future ImageGeneration-based grader hides the same way with no controller
   change. Rejected: adding a member to `IAIGuardrailEvaluator`/`IAITestGrader` (breaks
   implementers) or a property on the existing attributes (can't express "none" for an enum
   without a sentinel).
6. **Default threshold 0.7**, matching the LLM judges and leaning toward safety. Rejected:
   0.5, which lets a coin-flip answer through. User choice.
7. **Content goes in `Context`, criteria in `Instructions`.** That's what the Decision API's
   docs say each field is for. Conversation history isn't sent, matching the LLM evaluator,
   which ignores it too.
8. **Cancellation is rethrown**, not swallowed like the LLM siblings do.
9. **Controller constructors:** each of the three controllers gains an
   `IAIExperimentalFeatures` parameter. The old constructor stays, marked
   `[Obsolete("Will be removed in v20")]` (v19 on v17) and proxying via
   `StaticServiceProvider.Instance.GetRequiredService<IAIExperimentalFeatures>()`. The new
   one gets `[ActivatorUtilitiesConstructor]` (see `.claude/memory/controller-ctor-change-keep-obsolete.md`).
