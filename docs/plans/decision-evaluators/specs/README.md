# Pending specs

These spec files are staged here, not in the test project, because they reference types and
constructors that don't exist until their task lands. Placed in
`Umbraco.AI/tests/Umbraco.AI.Tests.Unit`, they would break the build for every earlier task (C#
test projects compile every `.cs` file in the folder).

**Each file's path under `specs/` is its final repo-relative path.** The task that makes a file
pass moves it into place, removes the `Skip = "Pending T<n>"` from its facts, and commits it
**in the same commit as the production code** (see `umb-build-loop-gotchas`).

Specs were written against the names in `ARCHITECTURE.md`/`SPEC.md`. Each file's header lists the
production signatures it assumes. They were compile-checked against temporary stubs: the only
errors left were the missing production types and the new controller constructors. Where a
builder finds a real signature differs, fix the spec to the real signature, but keep the behavior
it asserts and its one-assertion shape.

| Task | Story | Files |
|------|-------|-------|
| T1 | DE-3 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Extensions/AIRequiresCapabilityExtensionsTests.cs` |
| T2 | DE-1 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Guardrails/Evaluators/DecisionGuardrailEvaluatorTests.cs` |
| T3 | DE-2 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Tests/Graders/DecisionJudgeGraderTests.cs` |
| T4 | DE-4 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Guardrail/AllGuardrailEvaluatorsControllerTests.cs`, `.../Api/Management/Test/TestGraderControllersTests.cs` |

## Covered elsewhere, not by a staged spec

- **DE-4 AC1, AC2, AC5-AC7 against the real judges, and SPEC API-6 (OpenAPI unchanged):** the T4
  specs use fake marked evaluators/graders. The real judges in the real lists are proven live in
  wire task T5a. OpenAPI stability is a T4 acceptance check, not a spec.
- **DE-5, DE-6:** wire and port tasks (T5a, T5, T6, T7), not unit specs.

## Builder requirements the specs impose

- `DecisionGuardrailEvaluator(IAIDecisionService, IAIExperimentalFeatures, IAIGuardrailEvaluatorInfrastructure)`
  and `DecisionJudgeGrader(IAIDecisionService, IAIExperimentalFeatures, IAITestGraderInfrastructure)`.
  The parameter order is a guess. If it differs, only each file's `Harness` constructor call changes.
- The flag check calls `IAIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision)`.
- The Decision call uses the builder overload
  `AskAsync<AIBinaryDecisionResponse>(Action<AIDecisionBuilder>, AIBinaryDecisionQuestion, CancellationToken)`.
  The specs check the profile by running the captured action on a real `AIDecisionBuilder` and
  reading its internal `ProfileId`.
- `Metadata` keys are camelCase (`probability`, `answer`, `confidence`, `threshold`, `modelId`).
- Config fields sort in the order Profile, Evaluation Criteria, threshold, by `SortOrder`. Labels
  are `Profile`, `Evaluation Criteria`, `Safety Threshold` / `Pass Threshold`. The profile picker's
  `EditorConfig` contains `{"alias":"capability","value":"Decision"}`.
- Controllers: the new constructor adds `IAIExperimentalFeatures` as the last parameter. The old
  constructor stays `[Obsolete]` and resolves it through `StaticServiceProvider.Instance`. The AC9
  specs set `StaticServiceProvider.Instance` to a mock for the duration of the scenario, in an
  xUnit collection with parallelization turned off.
