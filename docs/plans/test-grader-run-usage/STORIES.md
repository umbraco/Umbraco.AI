# Stories

Source: [SPEC.md](./SPEC.md). Issue [#516](https://github.com/umbraco/Umbraco.AI/issues/516).

> ASSUMPTION: Definition of Ready / Done (proposed, correct if the team disagrees).
> **Ready:** role, capability and value are stated; Given/When/Then covers happy and sad paths;
> out-of-scope is explicit. **Done:** every criterion passes as an executable spec, the touched
> products build, the demo site shows populated usage on a real test run, and the change is
> backported to v17.

## Epic: Cost-aware test grading

### S1 — Run token totals for graders (M)

As a **developer writing a code-based test grader**,
I want **the run's total token usage on the outcome my grader receives**,
so that **I can pass or fail a run against a token or cost budget**.

Out of scope: errored runs, cost/CO2e maths, a built-in budget grader.

**Happy path**

AC1.1 — Usage is ready before grading
  Given a test whose feature makes one tracked model call that reports 100 input and 20 output tokens
  When the run executes
  Then the outcome a grader receives has `Usage` with InputTokens 100, OutputTokens 20, TotalTokens 120

AC1.2 — Multiple calls are summed
  Given a test whose feature makes three tracked model calls that each report usage
  When the run executes
  Then `Usage` totals equal the sum of the three calls

AC1.3 — Call count
  Given a test whose feature makes three tracked model calls
  When the run executes
  Then `Usage.CallCount` is 3

AC1.4 — Collected with analytics switched off
  Given usage analytics is disabled
  When a run makes a tracked model call that reports usage
  Then `Usage` still holds that call's tokens

**Sad path / edges**

AC1.5 — Unreported usage is not zero
  Given a run with one call that reports usage and one that reports none
  When the run executes
  Then `UnreportedCallCount` is 1

AC1.6 — Unreported usage leaves totals as a lower bound
  Given a run with one call that reports 50 total tokens and one that reports none
  When the run executes
  Then `TotalTokens` is 50

AC1.7 — No tracked calls
  Given a test whose feature makes no tracked model call
  When the run executes
  Then `Usage` is null

AC1.8 — Grader calls are excluded
  Given a grader that itself makes a tracked model call while grading
  When the run executes
  Then that call is not counted in `Usage`

AC1.9 — Concurrent runs are isolated
  Given two runs executing at the same time, each making its own tracked calls
  When both complete
  Then each run's `Usage` holds only its own calls

AC1.10 — Errored run unchanged
  Given a feature that makes a tracked call and then throws
  When the run executes
  Then the run status is Error and its outcome is null

AC1.11 — Nested collection scopes
  Given a collection scope opened inside another collection scope
  When a tracked call completes in the inner scope and the inner scope is disposed
  Then only the inner collector holds that call, and the outer collector is current again

### S2 — Per-model breakdown for graders (S)

As a **developer writing a grader that compares or prices models**,
I want **the provider, model and resolved profile for each model the run used**,
so that **I can pick the right price or emission factor and tell variations apart**.

**Happy path**

AC2.1 — Model identity
  Given a run whose calls use profile "p1" on provider "openai" with model "gpt-x"
  When the run executes
  Then `Usage.Breakdown` has one entry with that ProviderId, ModelId, ProfileId and ProfileAlias

AC2.2 — Two models, two entries
  Given a run that calls two different models
  When the run executes
  Then `Usage.Breakdown` has two entries

AC2.3 — Entry totals
  Given a run that calls two different models
  When the run executes
  Then each entry holds only its own model's tokens and call count

AC2.4 — Top-level equals sum of entries
  Given a run that calls two different models
  When the run executes
  Then the top-level totals equal the sum of the entries' totals

AC2.5 — Capability tagged
  Given a run that makes one chat call and one embedding call
  When the run executes
  Then each entry carries its own Capability

**Sad path / edges**

AC2.6 — Unreported per model
  Given a model whose call reports no usage
  When the run executes
  Then that entry's `UnreportedCallCount` is 1

### S3 — Compatible contract, persisted and exposed (S)

As a **maintainer of an existing grader or a backoffice user viewing past runs**,
I want **the new data to be added without breaking anything and to survive a save and reload**,
so that **existing graders keep working and run details show usage**.

**Happy path**

AC3.1 — Round-trips through persistence
  Given a run whose outcome has a `Usage` with two model entries
  When it is saved and loaded again
  Then the loaded `Usage` has the same totals and the same two entries

AC3.2 — Exposed through the Management API
  Given a run whose outcome has a populated `Usage`
  When it is mapped to `TestRunResponseModel`
  Then `outcome.usage` carries callCount, unreportedCallCount and the breakdown list

AC3.3 — Visible on a real run
  Given the demo site with a working chat profile and a prompt or agent test
  When the test is run from the backoffice
  Then the run detail view shows non-null token usage with a models entry

**Sad path / edges**

AC3.4 — Old rows still load
  Given stored token usage JSON written before this change (no new fields)
  When it is loaded
  Then `Breakdown` is an empty list and `CallCount` is 0

AC3.5 — Grader contract unchanged
  Given the built-in graders and `IAITestGrader` / `AITestGraderBase`
  When the solution builds
  Then no grader signature has changed
