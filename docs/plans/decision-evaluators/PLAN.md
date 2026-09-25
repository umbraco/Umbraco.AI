# Plan

Task checklist for `umb-build-loop`. v18 work happens on `v18/feature/decision-evaluators`
(worktree `.claude/worktrees/v18-decision-evaluators`), stacked on
`v18/feature/decision-capability` (draft PR #419). v17 is ported after v18 is fully done and
wired (T6). Docs go to Umbraco.Docs (T5).

Read `umb-build-loop-gotchas`, `add-ai-capability`, and `.claude/memory/` (especially
`controller-ctor-change-keep-obsolete.md`, `otel-test-shared-activity-source.md`) before
starting. Pending specs are staged in `specs/` in this folder at their final repo-relative
paths; each task moves its own spec files into place and commits them with the production code
that makes them pass (see `specs/README.md`).

Build/test gate for every code task:
`dotnet build Umbraco.AI/Umbraco.AI.slnx` and `dotnet test Umbraco.AI/Umbraco.AI.slnx`.

## Setup

- [ ] **T0** — story: none. Commit this plan folder to `v18/feature/decision-evaluators` and
  push it with `-u origin v18/feature/decision-evaluators` (the branch currently tracks the
  Decision feature branch; it must get its own remote branch).
  Acceptance: `git ls-remote origin v18/feature/decision-evaluators` shows the commit.
  depends-on: none.

## Core

- [ ] **T1** — story: DE-3 (AC1-AC5). Add `AIRequiresCapabilityAttribute`
  (`Umbraco.AI.Core/Models/`, `AttributeTargets.Class`, `AllowMultiple = true`) and
  `AIRequiresCapabilityExtensions.AreRequiredCapabilitiesEnabled(this object, IAIExperimentalFeatures)`
  (`Umbraco.AI.Core/Extensions/`, namespace `Umbraco.AI.Extensions`). Not experimental.
  Specs: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Extensions/AIRequiresCapabilityExtensionsTests.cs`.
  Acceptance: build + Core tests green, DE-3 specs pass.
  depends-on: T0.

- [ ] **T2** — story: DE-1 (AC1-AC24). Add `DecisionGuardrailEvaluator` +
  `DecisionGuardrailEvaluatorConfig` per ARCHITECTURE "Extension points" and "Runtime
  behavior", marked `[AIRequiresCapability(AICapability.Decision)]`, per-file
  `#pragma warning disable UMBRACOAI_DECISION`. Specs:
  `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Guardrails/Evaluators/DecisionGuardrailEvaluatorTests.cs`.
  Acceptance: build + Core tests green, DE-1 specs pass.
  depends-on: T1. parallel-group: B

- [ ] **T3** — story: DE-2 (AC1-AC21). Add `DecisionJudgeGrader` + `DecisionJudgeGraderConfig`,
  same shape as T2. Specs:
  `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Tests/Graders/DecisionJudgeGraderTests.cs`.
  Acceptance: build + Core tests green, DE-2 specs pass.
  depends-on: T1. parallel-group: B

## Management API

- [ ] **T4** — story: DE-4 (AC1-AC10). Filter `AllGuardrailEvaluatorsController`,
  `AllTestGradersController`, and `ByIdTestGraderController` with
  `AreRequiredCapabilitiesEnabled`. Each gets an `IAIExperimentalFeatures` constructor
  parameter; old constructor kept `[Obsolete("Will be removed in v20")]`, proxying via
  `StaticServiceProvider`; new one `[ActivatorUtilitiesConstructor]`. Collections stay
  unfiltered. Specs use a fake evaluator/grader marked with the attribute, so this doesn't
  wait on T2/T3:
  `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Guardrail/AllGuardrailEvaluatorsControllerTests.cs`,
  `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Test/TestGraderControllersTests.cs`.
  Acceptance: build + Core tests green, DE-4 specs pass, OpenAPI output unchanged (no
  generated-client diff needed).
  depends-on: T1. parallel-group: B

## Wire

- [ ] **T5a** — story: DE-5 (AC1-AC4), DE-4 (AC1, AC2, AC5-AC7). **wire: judges into the
  running demo site.** On `demos/v18/` (port from `git config --worktree --get wdp.port`;
  stop any other demo site first) with a TypeSafe connection using
  `$Umbraco:AI:Secrets:TypeSafeApiKey` (never printed) and a Decision profile:
  1. Flag on: `GET guardrail-evaluators` / `GET test-graders` include `decision-judge`; both
     show in the backoffice pickers with the three fields and a Decision-only profile picker.
  2. A guardrail with a Decision Safety Judge rule (Block) flags a clearly unsafe response and
     passes a safe one in a real chat.
  3. An AI test with a Decision Judge grader passes a good output and fails a bad one, score =
     probability.
  4. No default Decision profile and no `ProfileId`: rule flags / grader fails with the
     service's message.
  5. Flag off (config reload or restart): both absent from the lists; the saved rule fails
     safe with the "turned off" reason.
  Acceptance: each step observed on the running site and recorded in BUILD-LOG.md.
  Any code fix found here goes back through a builder (see gotchas).
  depends-on: T2, T3, T4.

- [ ] **T5** — story: DE-5 (AC5). Docs pages for both judges in
  `/Users/matt/Documents/Work/Umbraco/Umbraco.Docs-worktrees/ai-decision-docs`
  (branch `ai/decision-docs`), v17 and v18, next to the existing LLM judge / guardrail
  evaluator / grader pages. Mark experimental. Committed locally on that branch, not pushed.
  Acceptance: pages exist on both version trees, linked from the evaluator/grader lists and
  SUMMARY.md where siblings are; matches what T5a observed.
  depends-on: T5a.

## v17

- [ ] **T6** — story: DE-6 (AC1, AC2). Port to v17 with the `backport` skill: new worktree
  (absolute path, from the main repo root) on `v17/feature/decision-evaluators` branched from
  `origin/v17/feature/decision-capability`. Obsolete messages say `v19`. Adapt to any CMS 17
  API differences.
  Acceptance: `dotnet build` + `dotnet test Umbraco.AI/Umbraco.AI.slnx` green on v17.
  depends-on: T5a.

- [ ] **T7** — story: DE-6 (AC3). **wire: judges into the v17 demo site.** Repeat T5a on
  `demos/v17/`.
  Acceptance: recorded in BUILD-LOG.md.
  depends-on: T6.

After T7: `describe-pr` (runs `decision-review`) for each line, then draft PRs against
`v18/feature/decision-capability` and `v17/feature/decision-capability` (DE-6 AC4). Never
merged.
