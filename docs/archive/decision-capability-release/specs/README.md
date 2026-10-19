# Pending specs

> **Status:** Archived 09-10-2026. Shipped: merged to `v18/dev` (#419) and `v17/dev` (#428) on 09-10-2026, together with the follow-on `decision-evaluators` plan (#430, #431). Only T26 (the public Umbraco.Docs PR, branch `ai/decision-docs`) was still open at archive time.

This folder stages each task's pending spec files at their final repo-relative paths until the
task that makes them pass moves them into its real test project (see DECISION-LOG, "Pending specs
are staged in `specs/`"). They reference types that don't exist until their task lands, so placed
in the real test projects they would break the build for every earlier task (C# test projects
compile every `.cs` file in the folder).

**Each file's path under `specs/` is its final repo-relative path.** The task that makes a file
pass moves it into place, removes the `Skip`/`it.skip`, and commits it **in the same commit as the
production code** (see `umb-build-loop-gotchas`).

Specs were written against the names in `ARCHITECTURE.md`/`SPEC.md` without compiling. Each
file's header lists its guesses. Where a builder finds a real signature differs, fix the spec to
the real signature but keep the behavior it asserts and its one-assertion shape.

The first round (T2-T22) is fully moved. Everything below is the M.E.AI-direction rework
(T29-T37, DECISION-LOG 02-10-2026).

## Staged files

Paths below are relative to `specs/`.

| Task | File | Stories / ACs |
|------|------|---------------|

T29's five staged files (`AskTypedDecisionAnswerTests.cs`, `DecisionBatchTests.cs`,
`DecisionTrackingAndChecksHarness.cs`, `DecisionBatchUsageTests.cs`, and
`AIOpenTelemetryDecisionBatchTests.cs`) have moved into their real paths. T30 moved
`ProviderAnswerChecksTests.cs` into place and unskipped it, along with `DecisionBatchTests.cs`'s two
`GivenAProviderThatSkipsAQuestion`/`GivenAProviderThatAnswersAnUnaskedId` cases. T31 moved
`TypeSafeDecisionClientBatchRequestTests.cs` and `TypeSafeDecisionClientGapFillTests.cs` into place
and unskipped them — the real `TypeSafeTestHost.CreateClientAsync`/`GetResponseAsync` signatures
matched the staged assumption exactly, so no spec-side fixes were needed. T35 moved
`DecisionActionsReworkTests.cs` into place and unskipped it — the staged
`AskAsync<TAnswer>(Action<AIDecisionBuilder>, AIDecisionQuestion<TAnswer>, string? state,
CancellationToken)` call shape matched exactly, so no spec-side fixes were needed either. T33
moved `decision.server.data-source.state.test.ts` into place and unskipped it unchanged — the
staged `state` forwarded as the request's top-level field matched the implementation exactly.
`decision.controller.result-shapes.test.ts` moved into place too, but review found it mocked the
repository with results already in the public shape, so its result-shape assertions
(`trueProbability`, score `probabilities` keyed by index, no `answer`/`level`, `confidence`
omitted rather than `undefined`) only read back their own input — moved those into
`decision.server.data-source.test.ts` instead, where the mock is the real wire boundary
(`DecisionService.ask`). The file's forwarding assertions (`state`, `profileIdOrAlias`) were
genuine and got folded into `decision.controller.test.ts`'s existing options-forwarding scenario;
the now-empty file was deleted. T36 moved both its files into place and unskipped them. The
list-editor spec's `select` helper constructed `UaiSelectedEvent` with one positional arg; fixed to
the real two-arg `new UaiSelectedEvent(value, item)` signature the picker itself dispatches with.
Its placeholder `Question` type (`{ kind: string; ... }`) was swapped for the real
`UaiDecisionQuestionListItem` so the test file type-checks under `tsc -p tsconfig.api.json` (which
includes `src/**/*.test.ts`). The config-modal spec needed no signature fixes — its assumed `data`/
`modalContext` shape and `#alias`/`#instructions`/`#btn-submit` ids matched exactly. T37 moved
`AskDecisionsActionTests.cs` into place and unskipped it — every assumption in its header matched
the real signatures exactly. One fix was needed: `SchemaAsync()` goes through
`IStepType.GetOutputSchemaAsync`, which resolves settings via `ActionInfrastructure.ModelResolver`;
the bare `Mock<IEditableModelResolver>` the other decision action test files use (they never
exercise this path) returns `null` for an unconfigured `ResolveModel<T>` call, so the schema tests
got a resolver stub that round-trips through `System.Text.Json` the way the real (internal, not
visible outside `Umbraco.Automate.Core`) `EditableModelResolver` does. Also added: a settings
round-trip test proving a JSON payload in the editor's exact camelCase shape deserializes
correctly using Automate's settings JSON convention (camelCase, case-insensitive). Review found
the action was duplicating Core's per-kind bounds (choice 2..255, score 2..10) instead of relying
on `ValidatingDecisionClient`'s `ArgumentException` the way `AskChoiceDecisionAction` does — fixed
by deleting that duplication and moving "a pick-one with one option" out of `InvalidQuestionSets`
into its own test where the mocked `IAIDecisionService` throws `ArgumentException`, mirroring
`DecisionActionsTests.AskChoice_WithOneOption_FailsWithValidation`. Also added six more
`InvalidQuestionSets` cases for Automate-owned rules (bad alias shapes, an unknown kind, threshold
1.5/NaN), and the dynamic output schema now skips a question whose alias fails the same format
check as validation, and renders `confidence` as a nullable number (`["number", "null"]`) to match
how `Json.Schema.Generation` renders the single-question actions' own `double? Confidence`.

This folder now has nothing pending — every staged spec has moved into its real test project.

## Existing tests each task must update or delete

These assert the old shapes. A task adapts them to the new API, keeping the behavior they pin, or
deletes the cases a staged spec above supersedes.

- **T29 (done):** `FakeDecisionClient`, `AskTypedDecisionTests.cs` (per-kind scenarios superseded by
  `AskTypedDecisionAnswerTests`), `AIDecisionResponseTests.cs` (deleted — the answer types have no
  `Answer`/`Confidence` derivations to pin), `ValidatingDecisionClientTests.cs`,
  `AIDecisionClientFactoryTests.cs`, `AITrackingDecisionClientTests.cs`,
  `AIOpenTelemetryDecisionMiddlewareTests.cs` (trimmed to `Apply_ReturnsWrappedClient`),
  `Services/AIDecisionServiceTests.cs`, `Services/AIDecisionServiceRealPipelineTests.cs`,
  `Providers/CapabilitySettingsRoundTripTests.cs`, `Providers/DeclaredSettingsEnforcementTests.cs`,
  and the Web minimum-compile-fix (`AskDecisionController.cs` + its three test files) all updated to
  `GetResponseAsync`/`AIDecisionRequest`/answer types. `DecisionPipelineHarness.cs`,
  `Api/Management/Common/AICapabilityGateFilterTests.cs` needed no changes (no direct `AskAsync`
  calls).
- **T30 (done):** `Decision/AIDecisionClientFactoryTests.cs` needed no changes — its mismatch tests
  already go through `DecisionAnswerChecker`'s kind check and still assert "recorded as failure".
- **T31 (done):**
  - `TypeSafeDecisionClientRequestTests.cs`: deleted the `"q"`-keyed body lookups and the Context
    cases, superseded by `TypeSafeDecisionClientBatchRequestTests`. Kept the binary, choice and
    score criteria and model-id cases, sent through a one-question request (each question now
    carries an explicit `Id = "q"`, since `AIDecisionRequest` keys by id rather than a fixed `"q"`
    dictionary entry).
  - `TypeSafeDecisionClientResponseTests.cs`: deleted every Level-focused case (`GivenAScoreAnswer`'s
    label-keyed-probabilities assertion, the legend-disagrees-with-levels case, and the three
    clamping cases) — `AIScoreDecisionAnswer` has no `Level` anymore, superseded by
    `TypeSafeDecisionClientGapFillTests` for the gap-fill behavior. Kept
    `GivenAScoreAnswerWithAProbabilityKeyOutsideTheLevels` (ported to the new `Answers["q"]`
    navigation) since it pins a distinct, still-live behavior — dropping an out-of-range
    probability index — not covered by the gap-fill spec. `Probability` became `TrueProbability`.
  - `TypeSafeDecisionClientRetryTests.cs`: calls `GetResponseAsync` with a one-question
    `AIDecisionRequest` instead of `AskAsync`.
  - `TypeSafeProvider.EnsureConnectionValidAsync`'s probe (not a test file, but the one other
    caller of the old single-question `AskAsync`) now builds a one-question `AIDecisionRequest`
    and calls `GetResponseAsync`.
- **T32 (done):** `Api/Management/Decision/AskDecisionControllerTests.cs`:
  - Mocked `GetDecisionResponseAsync`.
  - Deleted the binary `Answer`/`Confidence` and score `Level` assertions, superseded by
    `AskDecisionReworkedShapeTests`.
  - Levels became `DecisionScoreLevelModel`.
  - `AskDecisionResponseFormattingTests.cs`/`AskDecisionRequestFormattingTests.cs`: updated their
    test controllers' payloads.
- **T33 (done):** `decision/controllers/decision.controller.test.ts`, `decision/repository/decision.server.data-source.test.ts`:
  updated the binary and score result fixtures to the new shapes; the result-shape assertions
  moved to the data-source file (see above), and `decision.controller.test.ts` gained a `state`
  forwarding assertion.
- **T34 (done):** `Umbraco.AI.Agent/tests/.../Agents/DecisionAgentSelectionTests.cs`:
  - Mock `AskAsync(question, state)`.
  - DR-10 AC2's "Context is the user's message" becomes "state is the user's message".
  - Read `response.Answer.Choice`.
- **T35 (done):** `Umbraco.AI.Automate/tests/.../Actions/DecisionActionsTests.cs`:
  - Mocked the new `AskAsync` (with `state`).
  - Deleted `AskYesNo_OutputsConfidence`, superseded by `DecisionActionsReworkTests`.
  - Score `Level` is now derived from the question's levels (computed by the action, not the
    provider response).

## Covered elsewhere, not by a staged spec

- **Wire ACs** (DR-2 AC17, DR-4 AC12, DR-9 AC10, DR-10 AC9, DR-15 AC10, DR-16 AC10) are proven
  by wire tasks T38-T40 and T43, not by specs.
- **DR-16 AC9, flag off at startup** (action missing from the picker): Automate's compose-time
  `Exclude<T>()` has no unit seam, as for the first three actions. Proven by T39. The run-time
  guard is specced in `AskDecisionsActionTests`.
- **DR-14 AC3 for the Guid and builder overloads:** only the alias overload is specced. The
  others share its resolution path, as `AskAsync`'s overloads did.
