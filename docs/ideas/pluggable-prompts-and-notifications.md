# Pluggable prompts and missing notifications

> **Status:** Idea / review report (01-10-2026). Written while building
> `docs/plans/pluggable-agent-selection/`. Nothing here is scoped yet, except the three items
> marked **Fixed**, which shipped in #459 (v18) and #462 (v17).

The question: the Copilot "Auto" agent router is becoming a pluggable selector chain. Where else
do we ship a **fixed prompt** that a developer can't change if they disagree with it, and where
would a **notification** help people watch or react to what the AI is doing?

All paths are on `v18/dev`. Line numbers are approximate.

---

## Part 1: Fixed prompts

### Summary

| # | Prompt | Where | Override today | Priority |
|---|--------|-------|----------------|----------|
| 1 | Profile `SystemPromptTemplate` (stored, never applied) | `Core/Profiles/AIChatProfileSettings.cs:21` | Now `AIProfileSystemPromptChatMiddleware` (remove/replace via `AIChatMiddleware()`) | **Fixed** |
| 2 | Prompt add-on format + retry instructions | `Prompt.Core/Prompts/AIPromptService.cs:317-341`, `:637-665` | Replace the whole `IAIPromptService` (`internal sealed`) | **High** |
| 3 | Guardrail LLM judge wrapper | `Core/Guardrails/Evaluators/LLMGuardrailEvaluator.cs:91-107` | Criteria only. Wrapper + JSON contract fixed; subclass the evaluator | **Medium** |
| 4 | Test "LLM Judge" grader wrapper | `Core/Tests/Graders/LLMJudgeGrader.cs:89-106` | Criteria only. Subclass the grader | **Medium** |
| 5 | Entity context block ("this page" instructions, schema rules) | `Core/EntityAdapter/Adapters/CmsEntityFormatHelper.cs:41-62`, `:93-114`; `GenericEntityAdapter.cs:32-48` | `internal static` helper. Exclude adapter + write your own, or replace `IAIEntityContextHelper` | **Medium** |
| 6 | Runtime context parts (`## Current User`, `## Current Section`, `Context: ...`) | `Core/RuntimeContext/Contributors/*` | Remove/replace via `AIRuntimeContextContributors()`, but classes are `internal sealed` so you rewrite them | Low |
| 7 | Context resource headings (`## Context`, on-demand resource list) | `Core/Contexts/AIContextProcessor.cs:37-80` | Replace `IAIContextProcessor` | Low |
| 8 | Headless approval-denied tool result | `Agent.Core/Chat/ApprovalDeniedAIFunction.cs:33` | None (`internal sealed`) | Low |
| 9 | DeepSeek JSON-mode system message | `DeepSeek/DeepSeekChatCapability.cs:102-107` | None (private nested class) | Low |
| 10 | Tool descriptions (backend + Copilot frontend tools) | `Core/Tools/**`, `Search/.../SemanticSearchTool.cs`, `Automate/Tools/*`, `Copilot/.../tools/entity/manifests.ts` | Subclass + `Exclude`/`Add` in `AITools()`; frontend via manifest re-registration | Low |

Not found anywhere: conversation title generation, chat history summarising, search query
rewriting, image prompt wrapping. No prompt lives in an embedded resource file; all are inline
strings.

### Details and suggestions

**1. `SystemPromptTemplate` was a dead setting. Fixed (#459 / #462).** It is now sent as the first
system message on every chat call through the profile, by the outermost
`AIProfileSystemPromptChatMiddleware`. It goes first because it is fixed text: putting it ahead of
per-request system text keeps the provider's cached prompt prefix stable. Leftovers: the text is
sent as-is (no template variables), and the UI hint still says "template".

**2. Prompt add-on format instructions.** The `OptionCount == 1` "Return ONLY the value" block, the
multi-option JSON block, and the retry block are local strings inside a sealed internal service.
Today a site that dislikes them must replace all of `IAIPromptService`. Suggest pulling them
behind a small replaceable service (e.g. `IAIPromptFormatInstructionBuilder` with
`BuildSingleValue`, `BuildOptions(int count)`, `BuildRetry(int count, string error)`). Also see
the bug below; touching this code is a good moment to fix it.

**3 + 4. LLM judges.** Both expose only the "criteria" sentence. The persona line, output JSON
shape and scoring rules are fixed. Suggest a `protected virtual string BuildJudgmentPrompt(...)` on
each (smallest change, fits the existing subclass route), or an optional full-template config
field with `{{criteria}}` / `{{content}}` tokens if admins should be able to change it without code.
The JSON contract must stay fixed because the parser depends on it, so document that the
template may change wording but not the output shape.

**5. Entity context.** This is the biggest block of fixed text the model sees on every Copilot and
Prompt request. It includes behaviour rules ("when the user says 'this page'...", "use the schema
as the source of truth when calling set_value"). It also drives token cost. Suggest a public
`IAIEntityContextFormatter` (default = today's helper) so a site can trim or reword it without
rewriting the adapters.

**6-10.** Override routes already exist or the text is plumbing. Worth one cheap tidy-up: make the
built-in runtime contributors public and non-sealed so people can subclass instead of rewrite.

### A shared pattern?

The agent selector is a **decision chain** (who answers). Most items above are **text** (what we
say). A chain is overkill for text. Two options:

- **Per-feature seams** (a virtual method or small builder interface per prompt). Simple, typed,
  matches how each area works today. Recommended.
- **One keyed template registry** (`IAIPromptTemplateProvider`, keys like
  `umbraco.guardrail.llm-judge`). One concept to learn, but untyped tokens and a new global
  abstraction for about four real prompts. Not worth it yet; revisit if the count grows.

A third option sits alongside the seams rather than replacing them: a mutable **"ing"
notification** that broadcasts what we are about to send and lets handlers change it first. See
below.

### Mutable "ing" notifications vs replaceable services

Umbraco already does "let handlers alter it before it goes out":
`SendingContentNotification` lets code change the content model just before the backoffice gets
it. A prompt-level equivalent fits the same pattern. The question is when it beats a replaceable
service.

**An "ing" notification is the better fit when:**

- **The change is small and additive.** For example "also use British spelling". The handler
  adds a line, and our default text stays in charge.
- **Several packages may want in at once.** Five packages can each add a rule. A replaceable
  service has only one winner.
- **The goal is a veto.** "Don't run this tool for this user" is a classic cancelable `*ing`.
- **The change depends on the request.** The handler sees the user, page and culture, and decides
  per call.

**A replaceable service (or virtual method) is the better fit when:**

- **The whole thing is being rewritten.** Someone dislikes our wording entirely and wants to own
  it.
- **The output shape is coupled to our code.** The guardrail and test judges must return the
  exact JSON our parser reads. A handler editing that prompt could break parsing silently, so
  override there should be explicit and tested.
- **Order matters.** Notification handlers have no dependable order. If order matters, use an
  ordered collection (like the agent selectors).
- **There must be one answer.** This is why agent selection has no cancelable `Selecting`
  notification: two ways to decide would conflict.

**Two traps to design around:**

- **Our wording becomes an API.** If handlers find-and-replace on our text, every wording change
  we make breaks someone. Pass **named parts** instead (e.g. `FormatRules`, `EntityContext`,
  `ProfilePrompt`) that handlers add to, remove or reorder. Never pass one flat string.
- **Provider prompt caching.** Providers cache the request prefix. A handler that puts changing
  text (today's date, the user's name) into a fixed part breaks the cache on every call. Fixed
  parts must stay first, and the event's docs must say which parts are fixed and which are
  per-request. Same rule as `AIProfileSystemPromptChatMiddleware` (#459 / #462).

**How this applies to the items above:**

| Item | Best fit | Why |
|------|----------|-----|
| 2. Prompt format rules | `*ing` notification, plus a replaceable builder | Most people want to add a rule; a few want to own it all |
| 5. Entity context ("this page" block) | `*ing` notification | Strongest use case: strip personal data or secret fields before they reach the model (helps with GDPR) |
| 1. Profile system prompt | `*ing` notification | Add per-site or per-language text; per-request text must not go in the fixed part |
| 3 + 4. Guardrail and test judges | Overridable method | Output format is tied to our parser |
| Tool calls (Part 2, #3) | Cancelable `*ing` notification | Veto is the main need |
| Agent selection | Selector chain (as planned) | Order and a single answer matter |

**Recommendation:** one `*ing` notification per feature, fired where the messages are built and
carrying named parts. Pair it with a replaceable builder for anyone who wants full control. That
covers both groups without making our exact wording a public contract.

**Gotcha for the Prompt add-on:** `AIPromptExecutingNotification` already exists, but it fires
before the messages are built (`AIPromptService.cs` ~:226, messages ~:303-344), so it can't serve
as the editing hook. Options: add a second notification after message building (e.g.
`AIPromptSendingNotification`), or add the message parts to a new, later event. Don't move the
existing one: it is the cheap early cancel point, before any template work.

---

## Part 2: Notifications

### What exists today

- CRUD (`Saving`/`Saved`/`Deleting`/`Deleted`) for Connection, Profile, Context, Guardrail, Test,
  Agent, Prompt; `Saving`/`Saved` for Settings.
- `RollingBack`/`RolledBack` for Connection, Profile, Context, Guardrail, Test.
- `Executing`/`Executed` for Chat, Embedding, Image generation, Speech-to-text, Agent, Prompt.
- Being added: `AIAgentSelectedNotification`.
- Search, Automate, Deploy and providers define none.

### Suggestions, highest value first

| # | Notification | Why | Natural spot |
|---|--------------|-----|--------------|
| 1 | **`AIAgentExecutedNotification` accuracy** (add a status: Succeeded / Failed / Cancelled / Interrupted) | The worst case is **fixed**: AG-UI runs ending in `RUN_ERROR` now report failure (#459 / #462). Still open: approval pauses report success, and cancelled looks the same as failed | `AIAgentService.StreamAgentAGUIAsync` ~:486; `AGUIStreamingService.cs:80-85` |
| 2 | **Per model call** `AIModelCallExecuting` / `Executed` (any capability, any caller) | Today's capability notifications fire only at the service entry. Agent turns, the agent classifier, Prompt and Search (passthrough builders) raise none. Payload: capability, profile, alias, usage, duration, error | A new `IAIOperationRecorder` in `Core/Observability/` (see `docs/plans/tracking-recorders/`): recorders already get every tracked call's start and its single outcome (status, usage, duration, error) |
| 3 | **Tool** `AIToolExecuting` (cancelable) / `AIToolExecuted` | Lets sites block, log or rate-limit specific tools without writing middleware. Highest-value governance hook | `Core/Chat/Middleware/AIFunctionInvokingChatMiddleware.cs:20` (M.E.AI `FunctionInvoker`) rather than each function |
| 4 | **Approval** `AIToolApprovalRequested` / `AIToolApprovalResolved` (approved / denied / auto-denied) | Audit trail and alerts for human-in-the-loop decisions | `Agent.Core/AGUI/AGUIStreamingService.cs:209-225`, `:473-492`; `ApprovalDeniedAIFunction.cs:29` |
| 5 | **Guardrail** `AIGuardrailEvaluated` (phase, result, action: pass / warn / redact / block) | Security teams want to see blocks and redactions. Today only an exception escapes | `Core/Guardrails/Middleware/AIGuardrailChatClient.cs` `EvaluateRulesAsync` ~:327-348 |
| 6 | **Prompt failure** on `AIPromptExecutedNotification` | `Executed` only fires on success (no try/finally), unlike Agent and capabilities | `Prompt.Core/Prompts/AIPromptService.cs:561-563` |
| 7 | **Agent + Prompt rollback** `RollingBack` / `RolledBack` | The other five versioned entities have them. Agent and Prompt only fire `Saving`/`Saved` on rollback | `AIAgentVersionableEntityAdapter.cs:209`, `AIPromptVersionableEntityAdapter.cs:202`; copy `AIProfileService.RollbackProfileAsync` |
| 8 | **Test run** `AITestRunExecuting` / `Executed` (+ grader verdicts in payload) | CI-style alerts when a test regresses | `Core/Tests/AITestService.cs:286`, `AITestRunner.cs:30` |
| 9 | **Profile resolving** (mutable) | Overlaps Part 2 of `docs/ideas/pluggable-routing.md`. Decide there whether routing is a chain or a notification; don't build both | `Chat/AIChatService.cs:311` and siblings; `AIAgentFactory.cs:180` |
| 10 | **Search** `AIVectorIndexed` / `AIVectorSearched` | Lower demand. Check first whether Umbraco.Cms.Search already raises its own | `Search.Core/Search/AIVectorIndexer.cs:51`, `AIVectorSearcher.cs:41` |

Smaller consistency points:

- All `Deleted` notifications carry only the ID, so handlers can't see what was deleted. Consider
  adding the entity (additive, non-breaking).
- Chat notifications live in namespace `Umbraco.AI.Core.InlineChat` but are published from
  `Chat/AIChatService.cs`.
- Today's `Executing` notifications carry no messages/options, so handlers can't inspect input.
  Worth enriching when #2 is built.

---

## Bugs found along the way

Both **fixed** in #459 (v18) and #462 (v17), with unit tests.

1. **Prompt retry deletes the wrong message.** `AIPromptService.cs:663` does `messages.RemoveAt(0)`
   to drop "the old system message". But the format instruction is appended at the **end**
   (`:327`/`:340`). So the retry removes the context system message, or, when there is no entity
   context, **the user's actual prompt**. The retry then runs without the user's content.
2. **AG-UI agent errors report success.** `AGUIStreamingService` catches stream exceptions and
   emits `RUN_ERROR` without rethrowing, so `streamCompleted` becomes `true` and
   `AIAgentExecutedNotification` fires with `IsSuccess = true`. Automate's "agent run failed"
   trigger will not fire for these runs.

