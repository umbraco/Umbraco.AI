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

### C. Umbraco Automate core — THEIRS (confirmation only, likely no code)
The mechanism exists; an Automate-rooted session need only **confirm** these behaviours of the existing infra (and flag any small gaps):
1. **`WaitForInput` output data is durably persisted** and available to the step when it re-executes on resume (so we can stash + rehydrate the conversation). If size-limited, what's the cap?
2. **The resume event payload (the approval decision) is delivered to the resumed step** — how does the re-executing action read the submitted decision (event data binding)?
3. **A single step can suspend more than once** across its lifetime (re-`WaitForInput` after a resume) — does WorkflowCore/our wiring support repeated suspensions of the same step, or must each approval be a distinct step?
4. **The pending-approvals dashboard renders our custom prompt/payload** (tool name + args), or needs an extension point to do so.
5. **Event key shape** — is `{RunId}:{StepId}:{callId}` acceptable, and does the decision API key by run+step only (which would need extending for per-call keys)?

## Recommendation (revises the earlier "who's better placed" answer)

- **Mostly us.** The decisive infra (suspend/resume/human-task) already exists in Automate, and `RunAgentAction` is our code, so the real work is the **transport-agnostic approval primitive in the agent layer (A)** plus the **`RunAgentAction` glue (B)** — both in our repos.
- **A small confirmation pass in the Automate codebase (C)** answers the five questions above; it's verification, not design. Best done by a Claude session rooted in the Automate repo, handed this brief.
- **Sequencing:** build (A) with/after the AG-UI HITL plan so the primitive is shared; then (B); run (C) in parallel to de-risk the persistence/resume-payload assumptions before building (B).
- This is **post-v17** work (it rides the HITL feature, which is targeted opt-in→v18 per `project_v17_alignment_breaking_changes`). No v17 blocker.
