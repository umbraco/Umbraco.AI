# Requirements brief: agent tool HITL approval in headless / Umbraco Automate runs

**Status:** Requirements brief (handoff artifact) · **Date:** 2026-06-16 · **Owner:** Umbraco.AI
**Related:** `docs/internal/plans/2026-06-16-backend-tool-hitl-approval.md` (the AG-UI/interactive HITL plan), `project_v17_alignment_breaking_changes`, `project_hitl_approval_architecture`

## Purpose

The backend-tool HITL approval plan covers the **AG-UI streaming** path (interrupt → user approves in chat → resume). It does **not** cover **headless** agent runs — chiefly `RunAgentAction` in `Umbraco.AI.Automate`, which runs an agent inside an Umbraco Automate workflow with no human at a chat UI. This brief defines what's needed so a destructive backend tool inside such a run can pause for human approval and resume — and who builds what.

## Feasibility finding (probe of the Umbraco Automate codebase, 2026-06-16)

**Umbraco Automate already has a complete, generic human-approval mechanism. This is integration glue, not new engine capability.**

- `ActionResult.WaitForInput(string eventName, string eventKey, object output)` — any action returns this to **suspend** the workflow until an external event arrives (backed by WorkflowCore `WaitForEvent`). Output data is persisted with the suspension. (`Umbraco.Automate.Core/Actions/ActionResult.cs`; exposed to actions via `ActionBase`.)
- Shipping `RequestApprovalAction` (`Actions/BuiltIn/RequestApprovalAction.cs`) is the exact template: `WaitForInput("approval", "{RunId}:{StepId}", output)`.
- Resolved by a decision API (`postApprovalsByRunIdStepsByStepIdDecision`) + a pending-approvals dashboard (`getApprovalsPending`) — the human-task UI exists (frontend finishing under Automate's own `phase2-hitl-branching-plan.md`).
- Persisted/resumable runs: `AutomationRunStatus.Suspended`, `SuspendRunController`/`ResumeRunController`, `EFCoreWorkflowPersistenceProvider`, run↔WorkflowCore-instance link.
- **`RunAgentAction` is in our package** (`Umbraco.AI.Automate`) and extends `ActionBase`, so it can call `WaitForInput(...)` today — no Automate-core change is required for the suspension mechanism itself.

## The integration model

```
Agent run (inside RunAgentAction) hits a destructive tool needing approval
  └─ agent run returns an "approval needed" outcome (toolCallId, toolName, args, conversation-so-far)
       └─ RunAgentAction returns ActionResult.WaitForInput("approval", "{RunId}:{StepId}:{callId}", prompt+payload)
            └─ Automate suspends the run; the pending approval appears in the existing approval dashboard
                 └─ human approves/rejects via the existing decision API → WorkflowCore "approval" event fires
                      └─ RunAgentAction step re-executes: rehydrate conversation + inject the decision
                           └─ re-invoke the agent run, which continues (executes or skips the tool)
                                └─ if another approval is needed, suspend again; else complete the step
```

The human-task half (suspend, dashboard, decision API, resume event) is **reused as-is** from Automate. The new work is (a) making the *agent run* suspendable/resumable on approval in a transport-agnostic way, and (b) the `RunAgentAction` glue that maps between the two.

## Ownership — who builds what

### A. Agent layer — `Umbraco.AI` (the bulk; OURS)
A **transport-agnostic approval suspend/resume** capability on `IAIAgentService.RunAgentAsync`, so callers other than AG-UI can drive it. Today the HITL plan builds this only for AG-UI (interrupt outcome + client replay + `ExtractToolResultsFromResume`). Generalize it:
1. **Suspend outcome:** when a destructive tool requires approval, the non-streaming run returns an "interrupted — approval required" result carrying `{ toolCallId, toolName, arguments, conversation }` instead of deadlocking. (This is the same pause the AG-UI path surfaces as a `human_approval` interrupt — factor it so both transports share it.)
2. **Resume entry point:** an overload/option to resume a run with a `ToolApprovalResponseContent(approved)` injected for a given `toolCallId`, replaying the prior conversation — the transport-agnostic form of the AG-UI resume mapping.
3. **Multiple approvals per run:** one agent run may need several approvals; the suspend/resume must be repeatable within a single logical run.

This is shared infrastructure: doing it here means AG-UI and Automate (and any future caller) use one mechanism. **Recommend building this as part of, or immediately after, the AG-UI HITL plan** rather than bespoke per transport.

### B. `RunAgentAction` glue — `Umbraco.AI.Automate` (OURS)
- Run the agent with `ApprovalPolicy = Interactive` (see the HITL plan's headless policy options) so approvals surface rather than auto-deny.
- On an "approval needed" outcome: return `WaitForInput("approval", "{RunId}:{StepId}:{callId}", payload)` where `payload` carries the tool name, arguments, and a human-readable prompt for the dashboard, **plus** enough state (the conversation-so-far + pending call) to rehydrate on resume. (WorkflowCore persists the `WaitForInput` output, so it survives the suspension.)
- On resume (step re-executes when the `approval` event fires): rehydrate the conversation + pending call from persisted state, read the decision from the event payload, re-invoke `RunAgentAsync` with the `ToolApprovalResponseContent` injected, and continue — suspending again if a further approval is needed, else completing with the agent's final output.
- Use a `callId`-qualified event key (`{RunId}:{StepId}:{callId}`) so multiple approvals in one run don't collide.

### C. Umbraco Automate core — THEIRS (a real workstream, NOT just confirmation)
These were the five questions for the confirmation pass; the answers (below) show that beyond output persistence, the agent-specific needs require genuine Automate-core changes.

## Confirmation pass results (Automate codebase probe, 2026-06-16)

A read-only investigation of `D:\Work\Umbraco\Umbraco.Automate` answered the five questions. **Correction to the optimistic framing above:** the suspension *primitive* exists and output persistence is solid, but everything that makes an *agent* approval differ from a single static `RequestApproval` requires Automate-core changes — concentrated in `ActionStepBody` (resume handling) and the Approval Web API.

| # | Concern | Verdict | Needs Automate-core change |
|---|---------|---------|----------------------------|
| 1 | Output persists across suspension | **Yes** | None. Stored in StepRun + WorkflowInstance (`nvarchar(max)`/TEXT, no cap), rehydrated via `BindingDataBuilder`. Caveat: payload must round-trip cleanly through WorkflowCore's Newtonsoft `TypeNameHandling.All` + the unwrap step — keep it plain-JSON, no `JsonElement`/polymorphic reliance. |
| 2 | Resume payload delivery | **Partial** | `EventData` reaches the resuming pointer, but `ActionStepBody.HandleResumeAsync` hardcodes deserialization to `ApprovalDecision` (approve/reject) and discards the rest. Need a per-action resume hook (e.g. `IResumableAction.ResumeAsync(context, eventData)`) to pass the raw decision to the agent. |
| 3 | Repeated suspension of one step | **No (biggest gap)** | `HandleResumeAsync` is terminal — always returns `Next()`, never re-invokes the action or returns `WaitForEvent`. The action's `ExecuteAsync` is not re-entered on resume at all. A dynamic, agent-driven count of approvals on one `RunAgentAction` step is impossible today. Needs resume→re-dispatch→honor-`WaitForEvent` re-subscription in `ActionStepBody`. |
| 4 | Dashboard custom prompt/payload | **Partial → effectively No** | Pending-approvals listing query is hardcoded to `RequestApproval`'s alias (`PendingApprovalsController` → `GetStepRunsByStatusAsync(RequestApprovalAction.ApprovalActionAlias, …)`), so a custom action never lists. Response model has no payload field; `Prompt` isn't even populated today. No extension point — needs API model + query + frontend changes. |
| 5 | Event key shape | **Partial** | Event name/key are unconstrained strings (`EventKey` col is 200 chars — room for `:{callId}`), but the decision API keys by run+step only and the resume lookup assumes one wait per step. Per-call keying needs API + resume-disambiguation changes. |

Also noted: `SubmitApprovalController` leaves `ApprovedByUserKey = null` (TODO) — approver-identity audit is currently unimplemented (relevant for governance of agent approvals).

**Net:** Q2 and Q3 are really one coherent Automate-core change — generalize `ActionStepBody` resume handling to (a) dispatch raw `EventData` to a resumable-action hook and (b) honor a re-returned `WaitForEvent` for re-suspension. Q4 (Approval API/dashboard generalization beyond the single alias + payload surfacing) and Q5 (per-call keying) are additional Web/API changes.

## Recommendation (corrected by the confirmation pass)

- **This is a genuine collaborative cross-product feature, not "mostly glue."** Both sides have real work:
  - **Ours (`Umbraco.AI` + `Umbraco.AI.Automate`):** the transport-agnostic approval suspend/resume on `RunAgentAsync` (A) and the `RunAgentAction` glue (B).
  - **Theirs (Umbraco Automate core):** the `ActionStepBody` resume re-dispatch + re-subscription (Q2+Q3 — the linchpin), plus Approval Web API/dashboard generalization (Q4) and per-call event keying (Q5). Best designed/built by an Automate-rooted effort — it's deep in their `ActionStepBody`/WorkflowCore wiring.
- **The Q3 limitation is the gating design fact.** Until one step can re-suspend, an agent run needing a dynamic number of approvals can't be modelled as a single `RunAgentAction`. Either the Automate-core resume re-dispatch lands first, or the feature is scoped to **one approval per agent run** as a v1 (acceptable interim — most agent turns do at most one destructive op).
- **Sequencing:** (1) decide v1 scope (single-approval interim vs. wait for Q2+Q3); (2) Automate team scopes the `ActionStepBody` resume change; (3) we build the transport-agnostic primitive (A) with the AG-UI HITL plan; (4) `RunAgentAction` glue (B) once the Automate hook exists.
- This remains **post-v17** work (rides the HITL feature, targeted opt-in→v18). No v17 blocker — but it's a larger effort than the headless-approval section of the HITL plan implied.
