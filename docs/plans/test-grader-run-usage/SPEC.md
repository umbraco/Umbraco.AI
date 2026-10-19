# Spec

## Grader-visible contract (C#, `Umbraco.AI.Core.Tests`)

### `AITestUsage` (new sealed class, on `AITestOutcome.Usage`)

| Member | Type | Meaning |
|--------|------|---------|
| `InputTokens` | `int` | Sum of input tokens over every tracked call in the run that reported usage |
| `OutputTokens` | `int` | Same, output tokens |
| `TotalTokens` | `int` | Same, total tokens. Uses the provider's total when given, else input + output |
| `CallCount` | `int` | Number of tracked AI calls in the run |
| `UnreportedCallCount` | `int` | Calls that returned no usage. `> 0` means the totals are a lower bound |
| `FailedCallCount` | `int` | Calls that failed. A failed call also counts in `CallCount`, and in `UnreportedCallCount` when it reported no usage |
| `DurationMs` | `long` | Summed duration of every tracked call. Overlapping calls are summed, so this is AI time, not wall-clock |
| `Breakdown` | `List<AITestUsageEntry>` (never null) | One entry per distinct (capability, provider, model, profile, feature type, feature ID). Graders sum the entries they care about |

### `AITestUsageEntry` (new sealed class)

`Capability` (`AICapability`), `ProviderId` (`string?`), `ModelId` (`string?`), `ProfileId`
(`Guid?`), `ProfileAlias` (`string?`), `FeatureType` (`string?`), `FeatureId` (`Guid?`),
`FeatureAlias` (`string?`, first alias seen for the entry), `FailedCallCount`, `DurationMs`, `InputTokens`, `OutputTokens`, `TotalTokens`,
`CallCount`, `UnreportedCallCount`.

### Behavior

1. After a successful feature execution, `AITestOutcome.Usage` is set before any grader
   runs, and graders receive it through the existing `outcome` argument.
2. A run with N tracked model calls reports the sum of all N in the totals, and `CallCount == N`.
3. A tool-calling loop inside one chat call is counted once, at the summed value the function
   invoker reports (no double counting).
4. Calls to two different models in one run produce two `Breakdown` entries, each with its own
   totals. The top-level totals equal the sum of the entries.
5. A call that returns no usage adds to `CallCount` and `UnreportedCallCount` only.
6. A run that makes no tracked call has `Usage == null`.
7. Model calls made by graders during grading are not counted.
8. Usage is collected whether or not usage analytics is enabled.
9. Two runs executing at the same time never see each other's usage.
10. An errored run (feature throws) behaves as today: status `Error`, no outcome.
11. Existing graders compile and pass unchanged. `IAITestGrader` and `AITestGraderBase` are not
    modified.
12. Run data saved before this change still loads. Its `Breakdown` list is empty and new counts are 0.
13. Calls to the same model from two features (for example the prompt and an LLM guardrail judge)
    produce two `Breakdown` entries, told apart by `FeatureType`, `FeatureId` and `FeatureAlias`.
14. A call that fails adds to `FailedCallCount` and `CallCount`. Every call's measured duration adds
    to `DurationMs` (the same value usage analytics records).
15. `AITestOutcome.TokenUsage` stays null for every run. It and `AITestTokenUsage` keep their
    original shape and are `[Obsolete]`.
16. Existing databases are upgraded by a rename migration; no stored data is lost.

## Management API surface

No new routes. Additive change to the existing test run response:

- `outcome.usage` (new, `TestUsageResponseModel`, on the existing test run endpoints) carries
  `inputTokens`, `outputTokens`, `totalTokens`, `callCount`, `unreportedCallCount`, `failedCallCount`, `durationMs` and `breakdown`
  (array of `TestUsageEntryResponseModel`: `capability`, `providerId`, `modelId`,
  `profileId`, `profileAlias`, `featureType`, `featureId`, `featureAlias`, `failedCallCount`, `durationMs`, `inputTokens`, `outputTokens`, `totalTokens`, `callCount`,
  `unreportedCallCount`).
- `outcome.tokenUsage` (`TestTokenUsageResponseModel`) keeps its shape, is always null and is
  `[Obsolete]` (removal in v20). Other existing fields keep their names and meaning. Auth is unchanged (same endpoints, same policy).
- The generated TypeScript client in `Umbraco.AI.Web.StaticAssets` is regenerated.

## Frontend components

One line changed: the test run detail view (`test-run-detail.element.ts`) renders
`outcome.usage` (was `outcome.tokenUsage`) as formatted JSON whenever it is present. A proper
table is follow-up #523.

Backoffice UX check: the only visible change is data appearing in an existing read-only field.
First run, mistake, wait, finish and return spots don't apply (no new input or action).
