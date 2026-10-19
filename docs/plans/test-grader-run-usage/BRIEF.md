# Brief

Source: issue [#516](https://github.com/umbraco/Umbraco.AI/issues/516), triaged 08-10-2026.

## Problem

Test graders can't judge a run by what it cost, because they never get the data.

1. **Token usage is never set.** `AITestOutcome.TokenUsage` exists, but `AITestRunner`
   (`ExecuteSingleRunAsync`) always builds the outcome with `TokenUsage = null`. Every grader
   sees no input, output or total token counts.
2. **Graders don't know which model ran.** `IAITestGrader.GradeAsync(transcript, outcome,
   graderConfig, ct)` carries no provider, model or resolved profile. With test variations that
   use different profiles, a grader can't tell runs apart.

**What a developer does today:** nothing useful. A grader can only look at the text or events a
run produced. The Prompt test feature happens to put a single call's usage into
`transcript.FinalOutput`, but it is undocumented, Prompt-only, has no model, and misses
multi-call runs. Agent test runs expose no usage at all.

### Who it's for

- Developers writing code-based graders, built-in or third-party.
- Concretely: graders that judge cost or efficiency. A max output or total tokens budget, a cost
  budget from price per token, an estimated CO2e budget (Umbraco.Community.AI.Carbon needs the
  model to pick the right EcoLogits factor), or comparing variations on cost as well as quality.
- Not the audience: backoffice editors. They may see token counts on the run detail screen as a
  side effect, but they are not who this is built for.

### Why now

The Carbon community package hit this gap directly. The test framework is new enough that
adding this now is cheaper than after third-party graders have worked around it.

### Success criteria

- `AITestOutcome.TokenUsage` holds the run's input, output and total tokens, **summed across
  every model call in the run**. Multi-turn agent runs and tool-calling loops count everything.
- A grader can read the provider ID and model ID for the run, and the resolved profile. If one
  run calls more than one model, the grader gets a per-model breakdown.
- A unit test proves a multi-call run reports the summed total, and an agent test run on the
  demo site shows non-null token usage.
- Existing built-in and third-party graders compile and behave unchanged. No public API break
  (the repo's backwards-compatibility rule applies).

> ASSUMPTION: "Run" means one `ExecuteSingleRunAsync` call (one feature execution for one
> variation and one repetition). Usage from graders that themselves call a model, such as an
> LLM-judge grader, is **not** counted in the run's usage, because that is the cost of grading,
> not of the thing under test.

> ASSUMPTION: Counting only chat calls is enough for v1. Embedding or other capability calls
> made during a run are not included unless they come for free from the same mechanism.

### Constraints

- Both active lines: build on `v18/dev`, then backport to `v17/dev`.
- If persistence changes need a migration, the migration IDs must be identical on v17 and v18.
- Must not depend on usage analytics being switched on. The analytics recorder exits early when
  analytics is disabled, but test graders still need the numbers.

### Facts checked while refining

- The chat tracking client (`AITrackingChatClient`) wraps every profile-backed chat call and
  reports `UsageDetails` through the internal `IAIOperationTracker`.
- At that point the runtime context already holds provider ID, model ID, profile ID and profile
  alias (`AIUsageContext.ExtractFromRuntimeContext`). So the per-call data graders need already
  exists, it just never reaches the test runner.
- `IAITestFeature.ExecuteAsync` returns only an `AITestTranscript`, so there is no channel today
  for usage or model data to flow back to the runner.
- Persistence (`AITestRunFactory`, `OutcomeTokenUsageJson`) and the Management API mapping
  (`TestTokenUsageResponseModel`) already handle `TokenUsage`.

### Riskiest unknowns

- **Does the Agent test path go through the tracking client?** `AgentTestFeature` runs the agent
  via the AG-UI streaming path with a profile override. If any agent call bypasses the tracked
  profile chat client, its tokens would be missed silently. Must be confirmed in design.
- **Ambient scope across async and streaming.** Usage on streamed calls is only known when the
  stream finishes. A per-run collector has to survive the agent's streaming loop and tool calls
  without leaking into concurrent runs (test runs can execute in parallel).
- **Providers that report no usage.** Some providers or models return no `UsageDetails`. The
  outcome must tell "zero tokens" apart from "unknown".

### Smallest version worth shipping

> ASSUMPTION: Populate `TokenUsage` (summed) plus the per-model breakdown (provider, model,
> profile, tokens) on the outcome, available to graders in memory. Showing the breakdown in the
> run detail UI, and persisting it, can follow if design finds it costly.

### Kill criteria

> ASSUMPTION: Drop the per-run collector approach if it can't be done without changing a public
> interface in a breaking way, or if it can't isolate concurrent runs reliably.

## Non-goals

- Calculating cost or CO2e. Graders do that from the inputs this supplies.
- A built-in "token budget" or "cost budget" grader. Possible follow-up, not part of this.
- Counting usage spent by graders themselves (see the assumption above).
- Changes to the usage analytics dashboard.
