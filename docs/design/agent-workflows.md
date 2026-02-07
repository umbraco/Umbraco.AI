# Agent Workflows Design

> **Status:** Draft
> **Date:** 2026-02-07
> **Author:** AI Design Session

## Table of Contents

1. [Overview](#overview)
2. [Product Positioning](#product-positioning)
3. [Core Concepts](#core-concepts)
4. [Domain Model](#domain-model)
5. [Backend Architecture](#backend-architecture)
6. [Umbraco Integration — Inputs & Outputs](#umbraco-integration--inputs--outputs)
7. [Frontend UI](#frontend-ui)
8. [AG-UI Streaming & Real-Time Feedback](#ag-ui-streaming--real-time-feedback)
9. [Management API](#management-api)
10. [Database Schema](#database-schema)
11. [Implementation Phases](#implementation-phases)

---

## Overview

Agent Workflows extends `Umbraco.AI.Agent` with orchestrated multi-step AI processes built on the [Microsoft Agent Framework (MAF) workflow system](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/overview). Where a single agent handles one conversational task, a workflow chains multiple agents (and deterministic functions) into a graph that processes Umbraco content through defined stages — review, translate, enrich, publish — with human approval gates along the way.

### Goals

- Allow backoffice users to **visually compose** multi-step AI workflows from existing agents
- Build on **MAF's WorkflowBuilder / Executor / Edge** abstractions so workflows are portable and testable
- Integrate deeply with **Umbraco content** as both input and output
- Stream execution progress to the backoffice via **AG-UI events**
- Support **human-in-the-loop** approval at any step

### Non-Goals

- Replacing Umbraco's existing content workflow/approval system (we complement it)
- Building a general-purpose BPM engine
- Long-running durable workflows that survive server restarts (phase 1)

---

## Product Positioning

Workflows are a **feature within `Umbraco.AI.Agent`**, not a new standalone product. The rationale:

| Consideration | Decision |
|---|---|
| Dependency | Workflows compose *agents* — they can't exist without them |
| Domain overlap | Workflows reuse agent scopes, contexts, profiles, and the AG-UI streaming infrastructure |
| Frontend | The workflow editor lives alongside the agent editor in the same backoffice section |
| Packaging | Ships as part of `Umbraco.AI.Agent` — no extra NuGet install needed |

The workflow system adds new domain entities (`AIWorkflow`, `AIWorkflowStep`, `AIWorkflowEdge`, `AIWorkflowRun`) and a new scope (`workflow`) to the existing agent add-on.

---

## Core Concepts

### Mapping to MAF

The design maps Umbraco concepts onto the MAF workflow engine:

| Umbraco Concept | MAF Concept | Description |
|---|---|---|
| **AIWorkflow** | `Workflow` (via `WorkflowBuilder`) | The full graph definition |
| **AIWorkflowStep** | `Executor<TIn, TOut>` | A node in the graph — either an agent or a deterministic function |
| **AIWorkflowEdge** | `Edge` (simple, conditional, fan-out/fan-in) | A directed connection between steps |
| **AIWorkflowRun** | `StreamingRun` | A single execution of a workflow |
| **Step Types** | `Executor` subclasses | Agent step, transform step, condition step, approval step |
| **Shared State** | `IWorkflowContext` | Data passed between steps via scoped state |

### Step Types

Workflows support these step types:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Step Types                               │
├─────────────┬───────────────────────────────────────────────────┤
│ AgentStep   │ Runs an existing AIAgent against the workflow     │
│             │ input. Configurable: which agent, override        │
│             │ profile, additional context IDs.                  │
├─────────────┼───────────────────────────────────────────────────┤
│ TransformStep│ Deterministic data transformation (map fields,  │
│             │ extract properties, merge results). No LLM call. │
├─────────────┼───────────────────────────────────────────────────┤
│ ConditionStep│ Evaluates a predicate against step output to    │
│             │ choose a branch. Powers conditional edges.        │
├─────────────┼───────────────────────────────────────────────────┤
│ ApprovalStep│ Pauses the workflow and waits for a human to      │
│             │ approve, reject, or provide feedback. Uses the    │
│             │ AG-UI interrupt/resume protocol.                  │
├─────────────┼───────────────────────────────────────────────────┤
│ ContentStep │ Reads from or writes to Umbraco content. Used     │
│             │ as workflow entry/exit points.                    │
├─────────────┼───────────────────────────────────────────────────┤
│ SubWorkflowStep│ Embeds another workflow as a single step      │
│             │ (MAF's workflow-as-agent pattern).                │
└─────────────┴───────────────────────────────────────────────────┘
```

### Execution Model

Workflows execute using MAF's **Pregel / Bulk Synchronous Parallel** model:

1. Input enters the starting step
2. Steps in the same "superstep" execute concurrently
3. Output is routed along edges to downstream steps
4. Approval steps **pause** execution until human resumes
5. The run completes when all terminal steps finish

```
Content Input ──▶ [Agent: Review] ──▶ [Condition: Quality Check]
                                           │           │
                                      (pass)       (fail)
                                           │           │
                                           ▼           ▼
                                   [Agent: Translate]  [Agent: Rewrite] ──┐
                                           │                              │
                                           ▼                              │
                                   [Approval: Editor Review] ◀────────────┘
                                           │
                                           ▼
                                   [Content: Publish]
```

---

## Domain Model

### AIWorkflow

```csharp
public sealed class AIWorkflow : IAIVersionableEntity
{
    public Guid Id { get; internal set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// The step that receives initial input when the workflow starts.
    /// </summary>
    public Guid StartingStepId { get; set; }

    /// <summary>
    /// All steps in this workflow.
    /// </summary>
    public IReadOnlyList<AIWorkflowStep> Steps { get; set; } = [];

    /// <summary>
    /// All edges connecting steps.
    /// </summary>
    public IReadOnlyList<AIWorkflowEdge> Edges { get; set; } = [];

    /// <summary>
    /// Input schema defining what data the workflow expects.
    /// JSON Schema format — used by the UI to render input forms.
    /// </summary>
    public string? InputSchema { get; set; }

    /// <summary>
    /// Scope IDs that categorize this workflow (e.g., "content-review", "translation").
    /// </summary>
    public IReadOnlyList<string> ScopeIds { get; set; } = [];

    /// <summary>
    /// Context IDs injected into every agent step in this workflow.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    public bool IsActive { get; set; } = true;
    public int Version { get; internal set; } = 1;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }
}
```

### AIWorkflowStep

```csharp
public sealed class AIWorkflowStep
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required AIWorkflowStepType Type { get; set; }

    /// <summary>
    /// Type-specific configuration serialized as JSON.
    /// See step type sections below for schemas.
    /// </summary>
    public required string Configuration { get; set; }

    /// <summary>
    /// Visual editor position (for the canvas UI).
    /// </summary>
    public AIWorkflowStepPosition Position { get; set; } = new(0, 0);
}

public enum AIWorkflowStepType
{
    Agent,
    Transform,
    Condition,
    Approval,
    ContentRead,
    ContentWrite,
    SubWorkflow
}

public record AIWorkflowStepPosition(double X, double Y);
```

### Step Configuration Schemas

Each step type has a typed configuration object serialized into `AIWorkflowStep.Configuration`:

```csharp
// Agent step — runs an existing agent definition
public sealed class AgentStepConfiguration
{
    /// <summary>
    /// Reference to an existing AIAgent by ID or alias.
    /// </summary>
    public required string AgentIdOrAlias { get; set; }

    /// <summary>
    /// Optional profile override. When null, uses the agent's own profile.
    /// </summary>
    public Guid? ProfileIdOverride { get; set; }

    /// <summary>
    /// Additional context IDs merged with the workflow-level contexts.
    /// </summary>
    public IReadOnlyList<Guid> AdditionalContextIds { get; set; } = [];

    /// <summary>
    /// Template for the user message sent to the agent.
    /// Supports {{variable}} placeholders resolved from workflow state.
    /// </summary>
    public string? InputTemplate { get; set; }

    /// <summary>
    /// Key in workflow state where this step's output is stored.
    /// </summary>
    public string OutputStateKey { get; set; } = "result";
}

// Transform step — deterministic data mapping
public sealed class TransformStepConfiguration
{
    /// <summary>
    /// Mapping expressions: state key → JMESPath or simple property path.
    /// </summary>
    public Dictionary<string, string> Mappings { get; set; } = new();
}

// Condition step — evaluates a predicate for branching
public sealed class ConditionStepConfiguration
{
    /// <summary>
    /// The state key to evaluate.
    /// </summary>
    public required string StateKey { get; set; }

    /// <summary>
    /// Expression to evaluate (e.g., "value > 0.8", "value == 'approved'").
    /// </summary>
    public required string Expression { get; set; }
}

// Approval step — pauses for human review
public sealed class ApprovalStepConfiguration
{
    /// <summary>
    /// Message displayed to the reviewer.
    /// Supports {{variable}} placeholders.
    /// </summary>
    public required string ReviewMessage { get; set; }

    /// <summary>
    /// State keys whose values are shown to the reviewer for context.
    /// </summary>
    public IReadOnlyList<string> DisplayStateKeys { get; set; } = [];

    /// <summary>
    /// Umbraco user group aliases that can approve this step.
    /// Empty = any backoffice user.
    /// </summary>
    public IReadOnlyList<string> ApproverGroups { get; set; } = [];
}

// Content read step — loads Umbraco content into workflow state
public sealed class ContentReadStepConfiguration
{
    /// <summary>
    /// How the content is identified: "input" (from workflow input),
    /// "id" (hardcoded), "xpath" (query).
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// Content ID when Source is "id".
    /// </summary>
    public Guid? ContentId { get; set; }

    /// <summary>
    /// Property aliases to extract. Empty = all properties.
    /// </summary>
    public IReadOnlyList<string> PropertyAliases { get; set; } = [];

    /// <summary>
    /// Key in workflow state where content data is stored.
    /// </summary>
    public string OutputStateKey { get; set; } = "content";
}

// Content write step — writes workflow results back to Umbraco
public sealed class ContentWriteStepConfiguration
{
    /// <summary>
    /// Action: "save" (draft), "saveAndPublish", "schedule".
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Mapping of content property alias → state key containing the value.
    /// </summary>
    public Dictionary<string, string> PropertyMappings { get; set; } = new();

    /// <summary>
    /// Whether to create a new version or update in-place.
    /// </summary>
    public bool CreateNewVersion { get; set; } = true;
}
```

### AIWorkflowEdge

```csharp
public sealed class AIWorkflowEdge
{
    public Guid Id { get; set; }
    public required Guid SourceStepId { get; set; }
    public required Guid TargetStepId { get; set; }
    public required AIWorkflowEdgeType Type { get; set; }

    /// <summary>
    /// For conditional edges: which branch this represents.
    /// "true" / "false" for condition steps, or named branches.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Display label on the edge in the visual editor.
    /// </summary>
    public string? Label { get; set; }
}

public enum AIWorkflowEdgeType
{
    /// <summary>Always activated — output flows unconditionally.</summary>
    Simple,

    /// <summary>Activated when the source condition step evaluates to the matching branch.</summary>
    Conditional,

    /// <summary>Fan-out: distributes input to multiple targets in parallel.</summary>
    FanOut,

    /// <summary>Fan-in: waits for all sources before activating target.</summary>
    FanIn
}
```

### AIWorkflowRun (Execution State)

```csharp
public sealed class AIWorkflowRun
{
    public Guid Id { get; set; }
    public required Guid WorkflowId { get; set; }

    /// <summary>
    /// Who or what triggered this run.
    /// </summary>
    public required AIWorkflowRunTrigger Trigger { get; set; }

    /// <summary>
    /// Current execution status.
    /// </summary>
    public AIWorkflowRunStatus Status { get; set; } = AIWorkflowRunStatus.Running;

    /// <summary>
    /// Per-step execution records.
    /// </summary>
    public IReadOnlyList<AIWorkflowStepExecution> StepExecutions { get; set; } = [];

    /// <summary>
    /// The shared state bag — accumulated output from all completed steps.
    /// Serialized as JSON.
    /// </summary>
    public string State { get; set; } = "{}";

    /// <summary>
    /// Input data provided when the run was started.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Final output from the workflow's terminal steps.
    /// </summary>
    public string? Output { get; set; }

    public DateTime DateStarted { get; set; } = DateTime.UtcNow;
    public DateTime? DateCompleted { get; set; }
    public Guid? StartedByUserId { get; set; }
}

public sealed class AIWorkflowStepExecution
{
    public Guid Id { get; set; }
    public required Guid StepId { get; set; }
    public required string StepName { get; set; }
    public AIWorkflowStepExecutionStatus Status { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public DateTime DateStarted { get; set; }
    public DateTime? DateCompleted { get; set; }
}

public enum AIWorkflowRunStatus
{
    Running,
    WaitingForApproval,
    Completed,
    Failed,
    Cancelled
}

public enum AIWorkflowStepExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    WaitingForApproval,
    Approved,
    Rejected
}

public sealed class AIWorkflowRunTrigger
{
    /// <summary>
    /// "manual", "contentPublish", "contentSave", "schedule", "api"
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// For content triggers: the content node ID that triggered execution.
    /// </summary>
    public Guid? ContentId { get; set; }

    /// <summary>
    /// For content triggers: the culture that triggered execution.
    /// </summary>
    public string? Culture { get; set; }
}
```

---

## Backend Architecture

### Service Layer

```
┌──────────────────────────────────────────────────────────────────┐
│                     IAIWorkflowService                           │
│  CRUD for workflow definitions (+ version tracking)              │
│  GetWorkflowAsync, SaveWorkflowAsync, DeleteWorkflowAsync       │
├──────────────────────────────────────────────────────────────────┤
│                     IAIWorkflowRunService                        │
│  Start, monitor, cancel, resume workflow runs                    │
│  StartWorkflowRunAsync, ResumeWorkflowRunAsync,                 │
│  CancelWorkflowRunAsync, GetWorkflowRunAsync                    │
├──────────────────────────────────────────────────────────────────┤
│                     IAIWorkflowEngine                            │
│  Compiles AIWorkflow → MAF WorkflowBuilder → StreamingRun       │
│  Maps step types to MAF Executors                                │
│  Handles event bridging (MAF events → AG-UI events)             │
└──────────────────────────────────────────────────────────────────┘
```

### Workflow Engine — Compilation

The engine compiles an `AIWorkflow` definition into a MAF workflow at runtime:

```csharp
public class AIWorkflowEngine : IAIWorkflowEngine
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly IContentService _contentService;

    public async Task<StreamingRun> CompileAndStartAsync(
        AIWorkflow workflow,
        string input,
        AGUIEventEmitter emitter,
        CancellationToken ct)
    {
        // 1. Create executors for each step
        var executors = new Dictionary<Guid, IExecutor>();
        foreach (var step in workflow.Steps)
        {
            executors[step.Id] = step.Type switch
            {
                AIWorkflowStepType.Agent => await CreateAgentExecutorAsync(step, emitter, ct),
                AIWorkflowStepType.Transform => CreateTransformExecutor(step),
                AIWorkflowStepType.Condition => CreateConditionExecutor(step),
                AIWorkflowStepType.Approval => CreateApprovalExecutor(step, emitter),
                AIWorkflowStepType.ContentRead => CreateContentReadExecutor(step),
                AIWorkflowStepType.ContentWrite => CreateContentWriteExecutor(step),
                AIWorkflowStepType.SubWorkflow => await CreateSubWorkflowExecutorAsync(step, emitter, ct),
                _ => throw new NotSupportedException($"Step type {step.Type} is not supported")
            };
        }

        // 2. Build the workflow graph
        var startingExecutor = executors[workflow.StartingStepId];
        var builder = new WorkflowBuilder(startingExecutor);

        foreach (var edge in workflow.Edges)
        {
            var source = executors[edge.SourceStepId];
            var target = executors[edge.TargetStepId];

            switch (edge.Type)
            {
                case AIWorkflowEdgeType.Simple:
                    builder.AddEdge(source, target);
                    break;
                case AIWorkflowEdgeType.Conditional:
                    builder.AddEdge(source, target,
                        condition: msg => EvaluateBranch(msg, edge.Branch));
                    break;
                case AIWorkflowEdgeType.FanOut:
                    // Collected into groups and added via AddFanOutEdge
                    break;
                case AIWorkflowEdgeType.FanIn:
                    // Collected into groups and added via AddFanInEdge
                    break;
            }
        }

        // 3. Mark terminal steps as outputs
        var terminalStepIds = FindTerminalStepIds(workflow);
        foreach (var id in terminalStepIds)
            builder.WithOutputFrom(executors[id]);

        // 4. Build and stream
        var wf = builder.Build();
        return await InProcessExecution.StreamAsync(wf, input);
    }
}
```

### Agent Step Executor

The most important executor — it bridges an existing `AIAgent` definition into a MAF workflow step:

```csharp
internal sealed class AgentStepExecutor : Executor<WorkflowStepInput, WorkflowStepOutput>
{
    private readonly AIAgent _agentDefinition;
    private readonly AgentStepConfiguration _config;
    private readonly IAIAgentFactory _agentFactory;
    private readonly AGUIEventEmitter _emitter;

    public override async ValueTask<WorkflowStepOutput> HandleAsync(
        WorkflowStepInput input,
        IWorkflowContext context,
        CancellationToken ct)
    {
        // 1. Emit step started via AG-UI
        _emitter.EmitStepStarted(Name);

        // 2. Resolve input template with workflow state
        var userMessage = ResolveTemplate(_config.InputTemplate, input.State);

        // 3. Build the agent via the existing factory
        var scopedAgent = await _agentFactory.CreateAgentAsync(
            _agentDefinition,
            contextItems: BuildContextItems(input, context),
            ct: ct);

        // 4. Run the agent and collect streamed output
        var messages = new List<ChatMessage> { new(ChatRole.User, userMessage) };
        var result = new StringBuilder();

        await foreach (var update in scopedAgent.RunStreamingAsync(messages, ct: ct))
        {
            // 5. Bridge agent streaming events → AG-UI events
            if (update.Text is not null)
            {
                _emitter.EmitTextChunk(update.Text);
                result.Append(update.Text);
            }
        }

        // 6. Store result in workflow state
        await context.QueueStateUpdateAsync(
            _config.OutputStateKey, result.ToString(), scopeName: Name);

        // 7. Emit step finished
        _emitter.EmitStepFinished(Name);

        return new WorkflowStepOutput(result.ToString(), input.State);
    }
}
```

### Approval Step Executor

Pauses execution and waits for human input using the AG-UI interrupt protocol:

```csharp
internal sealed class ApprovalStepExecutor : Executor<WorkflowStepInput, WorkflowStepOutput>
{
    private readonly ApprovalStepConfiguration _config;
    private readonly AGUIEventEmitter _emitter;
    private TaskCompletionSource<ApprovalResult> _approvalTcs = new();

    public override async ValueTask<WorkflowStepOutput> HandleAsync(
        WorkflowStepInput input,
        IWorkflowContext context,
        CancellationToken ct)
    {
        _emitter.EmitStepStarted(Name);

        // 1. Build the review context from workflow state
        var reviewData = ExtractReviewData(input.State, _config.DisplayStateKeys);
        var reviewMessage = ResolveTemplate(_config.ReviewMessage, input.State);

        // 2. Emit an interrupt event — pauses the frontend run
        _emitter.EmitInterrupt(new AGUIInterruptInfo
        {
            Id = Guid.NewGuid().ToString(),
            Reason = "approval_required",
            Payload = new
            {
                stepName = Name,
                message = reviewMessage,
                reviewData,
                approverGroups = _config.ApproverGroups
            }
        });

        // 3. Wait for human to resume (via POST /runs/{runId}/resume)
        var approval = await _approvalTcs.Task.WaitAsync(ct);

        // 4. Route based on approval outcome
        _emitter.EmitStepFinished(Name);

        return approval.Approved
            ? new WorkflowStepOutput(approval.Feedback ?? "approved", input.State)
            : throw new WorkflowApprovalRejectedException(Name, approval.Feedback);
    }

    /// <summary>
    /// Called by the run service when a resume event is received for this step.
    /// </summary>
    public void SetApprovalResult(ApprovalResult result) => _approvalTcs.SetResult(result);
}
```

---

## Umbraco Integration — Inputs & Outputs

### Content as Workflow Input

Workflows integrate with Umbraco content at three levels:

#### 1. Manual Trigger (Backoffice Action)

A content editor selects a content node and triggers a workflow from a content app or action menu:

```
┌──────────────────────────────────────────┐
│  Blog Post: "Getting Started with AI"    │
│  ┌──────────────────────────────────┐    │
│  │ [Run Workflow ▾]                 │    │
│  │   ├─ Content Review Pipeline     │    │
│  │   ├─ Multi-Language Translation  │    │
│  │   └─ SEO Optimization            │    │
│  └──────────────────────────────────┘    │
└──────────────────────────────────────────┘
```

The selected content node's properties are serialized into the workflow's input state:

```json
{
  "contentId": "a1b2c3d4-...",
  "contentType": "blogPost",
  "culture": "en-US",
  "properties": {
    "title": "Getting Started with AI",
    "bodyText": "<p>Artificial intelligence is...</p>",
    "summary": "A beginner's guide to AI concepts",
    "tags": ["ai", "beginner"]
  }
}
```

#### 2. Event-Driven Trigger (Notifications)

Workflows can subscribe to Umbraco content notifications to auto-trigger:

```csharp
public class WorkflowContentNotificationHandler :
    INotificationAsyncHandler<ContentPublishingNotification>,
    INotificationAsyncHandler<ContentSavingNotification>
{
    private readonly IAIWorkflowRunService _runService;
    private readonly IAIWorkflowService _workflowService;

    public async Task HandleAsync(
        ContentPublishingNotification notification,
        CancellationToken ct)
    {
        foreach (var content in notification.PublishedEntities)
        {
            // Find workflows configured to trigger on this content type
            var workflows = await _workflowService
                .GetWorkflowsByTriggerAsync("contentPublish", content.ContentType.Alias, ct);

            foreach (var workflow in workflows)
            {
                await _runService.StartWorkflowRunAsync(workflow.Id, new AIWorkflowRunTrigger
                {
                    Type = "contentPublish",
                    ContentId = content.Key,
                    Culture = notification.Messages.FirstOrDefault()?.Culture
                }, ct);
            }
        }
    }
}
```

Supported Umbraco triggers:

| Trigger | Umbraco Event | Use Case |
|---|---|---|
| `contentSave` | `ContentSavingNotification` | Auto-review on save |
| `contentPublish` | `ContentPublishingNotification` | Pre-publish quality check |
| `contentCreated` | `ContentSavedNotification` (new) | Auto-generate metadata for new content |
| `mediaUploaded` | `MediaSavedNotification` | Auto-tag images, generate alt text |

#### 3. API Trigger

External systems trigger workflows via the Management API:

```http
POST /umbraco/ai/management/api/v1/workflows/{workflowIdOrAlias}/runs
Content-Type: application/json

{
  "trigger": { "type": "api" },
  "input": {
    "contentId": "a1b2c3d4-...",
    "culture": "en-US"
  }
}
```

### Content as Workflow Output

The `ContentWrite` step writes results back to Umbraco content:

```
Workflow State                          Umbraco Content
┌─────────────────────┐                ┌─────────────────────┐
│ translatedTitle ─────┼──────────────▶│ title (fr-FR)       │
│ translatedBody ──────┼──────────────▶│ bodyText (fr-FR)    │
│ seoDescription ──────┼──────────────▶│ metaDescription     │
│ generatedTags ───────┼──────────────▶│ tags                │
└─────────────────────┘                └─────────────────────┘
```

The content write step uses Umbraco's `IContentService` to save/publish:

```csharp
internal sealed class ContentWriteExecutor : Executor<WorkflowStepInput, WorkflowStepOutput>
{
    private readonly IContentService _contentService;
    private readonly ContentWriteStepConfiguration _config;

    public override async ValueTask<WorkflowStepOutput> HandleAsync(
        WorkflowStepInput input,
        IWorkflowContext context,
        CancellationToken ct)
    {
        var contentId = input.State.GetValue<Guid>("contentId");
        var content = _contentService.GetById(contentId);

        // Map workflow state to content properties
        foreach (var (propertyAlias, stateKey) in _config.PropertyMappings)
        {
            var value = input.State.GetValue<string>(stateKey);
            content.SetValue(propertyAlias, value);
        }

        // Execute the configured action
        switch (_config.Action)
        {
            case "save":
                _contentService.Save(content);
                break;
            case "saveAndPublish":
                _contentService.SaveAndPublish(content);
                break;
        }

        return new WorkflowStepOutput("content_updated", input.State);
    }
}
```

### Example: Content Translation Workflow

A complete workflow that translates a content node to another language:

```
[ContentRead]          Read source content properties (en-US)
     │
     ▼
[Agent: Translate]     "Translate the following content to French: {{content.bodyText}}"
     │
     ▼
[Agent: Review]        "Review this translation for accuracy: {{translatedBody}}"
     │
     ▼
[Condition]            quality_score > 0.8 ?
     │           │
  (pass)      (fail)
     │           │
     ▼           ▼
[Approval]    [Agent: Translate]  ←── retry loop (max 2)
     │
     ▼
[ContentWrite]         Save translated properties to fr-FR variant
```

---

## Frontend UI

### Workflow Editor — Visual Canvas

The workflow editor is a node-based visual canvas built with Lit web components. Users drag-and-drop steps onto a canvas and connect them with edges.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Workflow: Content Translation Pipeline                    [Save] [Run] │
├──────────┬──────────────────────────────────────────────────────────────┤
│          │                                                              │
│ Steps    │   ┌──────────────┐                                          │
│          │   │ ContentRead  │─────▶┌────────────────┐                  │
│ ▸ Agent  │   │ "Load Source"│      │  Agent          │                 │
│ ▸ Trans. │   └──────────────┘      │  "Translate"    │──┐              │
│ ▸ Cond.  │                         └────────────────┘  │              │
│ ▸ Approv.│                                              ▼              │
│ ▸ Content│                         ┌────────────────┐                  │
│ ▸ SubWF  │                         │  Condition      │                 │
│          │                         │  "Quality ≥ 80%"│                 │
│──────────│                         └───┬────────┬───┘                 │
│          │                        pass │        │ fail                 │
│ Config   │                             ▼        ▼                      │
│          │                  ┌──────────┐  ┌──────────┐                 │
│ [Step    │                  │ Approval │  │ Agent    │                 │
│  config  │                  │ "Review" │  │ "Rewrite"│─── loops back   │
│  panel]  │                  └────┬─────┘  └──────────┘                 │
│          │                       │                                      │
│          │                       ▼                                      │
│          │                  ┌──────────────┐                           │
│          │                  │ ContentWrite  │                           │
│          │                  │ "Publish FR"  │                           │
│          │                  └──────────────┘                           │
└──────────┴──────────────────────────────────────────────────────────────┘
```

#### Canvas Architecture

The canvas is built as a Lit element with:

- **SVG layer** for edges (lines/arrows between nodes)
- **HTML layer** for step nodes (positioned absolutely via CSS transform)
- **Interaction layer** for drag-to-connect, pan, zoom

```typescript
@customElement('uai-workflow-canvas')
export class UaiWorkflowCanvas extends LitElement {
  @property({ type: Object })
  workflow?: AIWorkflow;

  @state()
  private _selectedStepId?: string;

  @state()
  private _viewport = { x: 0, y: 0, zoom: 1 };

  render() {
    return html`
      <div class="canvas-viewport" style="transform: translate(${this._viewport.x}px, ${this._viewport.y}px) scale(${this._viewport.zoom})">
        <svg class="edges-layer">
          ${this.workflow?.edges.map(edge => this._renderEdge(edge))}
        </svg>
        <div class="steps-layer">
          ${this.workflow?.steps.map(step => html`
            <uai-workflow-step-node
              .step=${step}
              .selected=${step.id === this._selectedStepId}
              @step-selected=${this._onStepSelected}
              @step-moved=${this._onStepMoved}
              @port-drag-start=${this._onPortDragStart}
              @port-drag-end=${this._onPortDragEnd}
              style="transform: translate(${step.position.x}px, ${step.position.y}px)"
            ></uai-workflow-step-node>
          `)}
        </div>
      </div>
    `;
  }
}
```

#### Step Node Component

Each node shows its type icon, name, and connection ports:

```typescript
@customElement('uai-workflow-step-node')
export class UaiWorkflowStepNode extends LitElement {
  @property({ type: Object }) step!: AIWorkflowStep;
  @property({ type: Boolean }) selected = false;

  render() {
    const icon = STEP_TYPE_ICONS[this.step.type];
    return html`
      <div class="step-node ${this.selected ? 'selected' : ''}" draggable="true">
        <div class="step-header">
          <uui-icon name=${icon}></uui-icon>
          <span class="step-name">${this.step.name}</span>
        </div>
        <div class="step-ports">
          <div class="port port-in" @mouseup=${this._onInputPortDrop}></div>
          ${this.step.type === 'Condition' ? html`
            <div class="port port-out port-true" data-branch="true">T</div>
            <div class="port port-out port-false" data-branch="false">F</div>
          ` : html`
            <div class="port port-out" @mousedown=${this._onOutputPortDrag}></div>
          `}
        </div>
      </div>
    `;
  }
}
```

#### Step Configuration Panel

When a step is selected, a side panel shows its type-specific configuration:

```typescript
@customElement('uai-workflow-step-config')
export class UaiWorkflowStepConfig extends LitElement {
  @property({ type: Object }) step!: AIWorkflowStep;

  render() {
    return html`
      <uui-box headline=${this.step.name}>
        <uui-form>
          <uui-form-layout-item>
            <uui-label slot="label">Name</uui-label>
            <uui-input .value=${this.step.name} @change=${this._onNameChange}></uui-input>
          </uui-form-layout-item>

          ${this._renderTypeSpecificConfig()}
        </uui-form>
      </uui-box>
    `;
  }

  private _renderTypeSpecificConfig() {
    switch (this.step.type) {
      case 'Agent':
        return html`<uai-workflow-agent-step-config .config=${this.step.configuration}></uai-workflow-agent-step-config>`;
      case 'Approval':
        return html`<uai-workflow-approval-step-config .config=${this.step.configuration}></uai-workflow-approval-step-config>`;
      case 'ContentRead':
        return html`<uai-workflow-content-read-step-config .config=${this.step.configuration}></uai-workflow-content-read-step-config>`;
      // ... etc
    }
  }
}
```

#### Agent Step Config

The agent step config lets users pick an existing agent and configure how it receives input:

```typescript
@customElement('uai-workflow-agent-step-config')
export class UaiWorkflowAgentStepConfig extends LitElement {
  @property({ type: Object }) config!: AgentStepConfiguration;

  render() {
    return html`
      <!-- Agent picker (existing agent definitions) -->
      <uui-form-layout-item>
        <uui-label slot="label">Agent</uui-label>
        <uai-agent-picker
          .value=${this.config.agentIdOrAlias}
          @change=${this._onAgentChange}
        ></uai-agent-picker>
      </uui-form-layout-item>

      <!-- Optional profile override -->
      <uui-form-layout-item>
        <uui-label slot="label">Profile Override</uui-label>
        <uai-profile-picker
          .value=${this.config.profileIdOverride}
          @change=${this._onProfileChange}
        ></uai-profile-picker>
      </uui-form-layout-item>

      <!-- Input template with variable insertion -->
      <uui-form-layout-item>
        <uui-label slot="label">Input Template</uui-label>
        <uai-template-editor
          .value=${this.config.inputTemplate}
          .availableVariables=${this._getUpstreamStateKeys()}
          @change=${this._onTemplateChange}
        ></uai-template-editor>
        <div slot="description">
          Use {{variable}} to reference values from upstream steps.
        </div>
      </uui-form-layout-item>

      <!-- Output state key -->
      <uui-form-layout-item>
        <uui-label slot="label">Output Key</uui-label>
        <uui-input
          .value=${this.config.outputStateKey}
          @change=${this._onOutputKeyChange}
        ></uui-input>
        <div slot="description">
          State key where this agent's output will be stored.
        </div>
      </uui-form-layout-item>
    `;
  }
}
```

### Workflow Run Monitor

When a workflow is executing, the canvas shows real-time progress:

```
┌─────────────────────────────────────────────────────────────────┐
│  Run: Content Translation Pipeline                    [Cancel]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────────────┐                                              │
│   │ ✅ ContentRead│─────▶┌─────────────────┐                    │
│   │ "Load Source" │      │ ⏳ Agent          │                   │
│   │ 0.3s          │      │ "Translate"       │──┐                │
│   └──────────────┘      │ Processing...     │  │                │
│                          │ ████████░░ 80%   │  │                │
│                          └─────────────────┘  │                │
│                                                ▼                │
│                          ┌──────────────────┐                   │
│                          │ ○ Condition       │                   │
│                          │ "Quality ≥ 80%"   │                   │
│                          │ Waiting...         │                   │
│                          └──────────────────┘                   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Step Output: "Translate"                                        │
│  L'intelligence artificielle est une technologie qui permet...   │
│  ████████████████████░░░░░░ streaming...                         │
└─────────────────────────────────────────────────────────────────┘
```

Step node status indicators:

| Icon | Status | Visual |
|---|---|---|
| `○` | Pending | Grey, dimmed |
| `⏳` | Running | Blue, pulsing border |
| `✅` | Completed | Green |
| `❌` | Failed | Red |
| `⏸` | Waiting for Approval | Amber, with "Approve/Reject" buttons |

### Workflow Listing in Backoffice

Workflows appear as a new section within the AI settings area:

```
Umbraco Backoffice → Settings → AI
├── Connections
├── Profiles
├── Prompts
├── Agents
└── Workflows    ◀── New section
    ├── Content Translation Pipeline
    ├── SEO Review Workflow
    └── Multi-Channel Content Distribution
```

### Content App Integration

A "Workflows" content app on content nodes shows:

1. **Available workflows** — filtered by content type triggers
2. **Run history** — past executions on this content node with status and output
3. **Active runs** — currently executing workflows with live progress

```
┌─────────────────────────────────────────────────────┐
│  Blog Post: "Getting Started with AI"               │
│  ┌─────┬──────────┬──────────┬───────────┐          │
│  │ Content│ Info   │ Workflows │ History  │          │
│  └─────┴──────────┴──────────┴───────────┘          │
│                                                      │
│  Available Workflows                                 │
│  ┌─────────────────────────────────────────┐        │
│  │ 🔄 Content Translation Pipeline         │        │
│  │    Translates content to selected        │        │
│  │    languages with quality review         │        │
│  │    [Run ▾] → Select target language      │        │
│  ├─────────────────────────────────────────┤        │
│  │ 📝 SEO Review Workflow                   │        │
│  │    Reviews and optimizes SEO metadata    │        │
│  │    [Run]                                 │        │
│  └─────────────────────────────────────────┘        │
│                                                      │
│  Recent Runs                                         │
│  ┌──────┬────────────────────┬────────┬──────┐      │
│  │ Time │ Workflow            │ Status │      │      │
│  │ 2h   │ Translation (FR)   │ ✅ Done │ View │      │
│  │ 1d   │ SEO Review         │ ✅ Done │ View │      │
│  │ 3d   │ Translation (DE)   │ ❌ Failed│ View │      │
│  └──────┴────────────────────┴────────┴──────┘      │
└─────────────────────────────────────────────────────┘
```

---

## AG-UI Streaming & Real-Time Feedback

Workflow execution streams events to the frontend using the existing AG-UI infrastructure. The workflow engine bridges MAF events to AG-UI events.

### Event Flow

```
MAF Workflow Engine                    AG-UI Event Stream                    Frontend
───────────────────                    ──────────────────                    ────────

WorkflowBuilder.Build()
InProcessExecution.StreamAsync()
      │
      ├─ Run starts              ──▶   RunStartedEvent                 ──▶  Show run monitor
      │
      ├─ Executor starts         ──▶   StepStartedEvent               ──▶  Highlight step node
      │     (ContentRead)                { stepName: "Load Source" }          as "running"
      │
      ├─ Executor completes      ──▶   StepFinishedEvent              ──▶  Mark step as done
      │                                  { stepName: "Load Source" }
      │
      ├─ Executor starts         ──▶   StepStartedEvent               ──▶  Highlight next step
      │     (Agent: Translate)           { stepName: "Translate" }
      │
      │  ├─ Agent streaming      ──▶   TextMessageChunkEvent          ──▶  Show streaming text
      │  │                               { delta: "L'intelligence" }         in output panel
      │  ├─ Agent streaming      ──▶   TextMessageChunkEvent
      │  │                               { delta: " artificielle" }
      │  └─ Agent done           ──▶   StepFinishedEvent
      │
      ├─ Executor starts         ──▶   StepStartedEvent
      │     (Approval)                   { stepName: "Editor Review" }
      │
      ├─ Approval needed         ──▶   CustomEvent                    ──▶  Show approval UI
      │                                  { name: "workflow.approval",         with approve/reject
      │                                    data: { stepName, content } }      buttons
      │
      │  ... user clicks approve ...
      │
      │  Frontend sends resume   ◀──   POST /runs/{id}/resume         ◀──  User clicks "Approve"
      │                                  { approved: true }
      │
      ├─ Executor completes      ──▶   StepFinishedEvent
      │                                  { stepName: "Editor Review" }
      │
      ├─ Executor starts         ──▶   StepStartedEvent
      │     (ContentWrite)               { stepName: "Publish FR" }
      │
      ├─ Content updated         ──▶   StepFinishedEvent
      │
      └─ Run completes           ──▶   RunFinishedEvent               ──▶  Show completion
                                         { outcome: "success" }              summary
```

### Custom Workflow Events

The workflow system emits custom AG-UI events for workflow-specific state:

```csharp
// Emitted when a workflow step needs approval
_emitter.EmitCustomEvent("workflow.approval_required", new
{
    stepId = step.Id,
    stepName = step.Name,
    reviewMessage = resolvedMessage,
    reviewData = displayData,
    approverGroups = config.ApproverGroups
});

// Emitted when workflow state changes
_emitter.EmitStateDelta(new[]
{
    new JsonPatchOperation("add", $"/steps/{step.Name}/output", result)
});

// Emitted with full workflow progress
_emitter.EmitActivitySnapshot(new AGUIActivity
{
    Type = "workflow_progress",
    Content = JsonSerializer.Serialize(new
    {
        workflowId,
        runId,
        steps = stepStatuses  // { name, status, output?, duration }
    })
});
```

### Streaming Endpoint

The workflow run streams via the same SSE pattern used by agent chat:

```
GET /umbraco/ai/management/api/v1/workflows/{workflowIdOrAlias}/runs/{runId}/stream
Accept: text/event-stream

data: {"type":"RUN_STARTED","timestamp":1738934400000}

data: {"type":"STEP_STARTED","stepName":"Load Source","timestamp":1738934400100}

data: {"type":"STEP_FINISHED","stepName":"Load Source","timestamp":1738934400500}

data: {"type":"STEP_STARTED","stepName":"Translate","timestamp":1738934400600}

data: {"type":"TEXT_MESSAGE_CHUNK","delta":"L'intelligence","timestamp":1738934401000}

data: {"type":"STEP_FINISHED","stepName":"Translate","timestamp":1738934405000}

data: {"type":"CUSTOM","name":"workflow.approval_required","data":{...},"timestamp":1738934405100}

... (paused, waiting for approval) ...

data: {"type":"STEP_FINISHED","stepName":"Editor Review","timestamp":1738934500000}

data: {"type":"RUN_FINISHED","outcome":"success","timestamp":1738934510000}
```

---

## Management API

### Workflow Definition Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/workflows` | List workflows (paged, filterable) |
| GET | `/workflows/{idOrAlias}` | Get workflow definition |
| POST | `/workflows` | Create workflow |
| PUT | `/workflows/{idOrAlias}` | Update workflow |
| DELETE | `/workflows/{idOrAlias}` | Delete workflow |

### Workflow Run Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/workflows/{idOrAlias}/runs` | Start a new run |
| GET | `/workflows/{idOrAlias}/runs` | List runs (paged) |
| GET | `/workflows/{idOrAlias}/runs/{runId}` | Get run status/details |
| GET | `/workflows/{idOrAlias}/runs/{runId}/stream` | SSE stream of run events |
| POST | `/workflows/{idOrAlias}/runs/{runId}/resume` | Resume paused run (approval) |
| DELETE | `/workflows/{idOrAlias}/runs/{runId}` | Cancel a running workflow |

### Content App Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/content/{contentId}/workflows` | Workflows available for this content type |
| GET | `/content/{contentId}/workflow-runs` | Run history for this content node |

All endpoints follow the existing `IdOrAlias` convention and share the `ai-management` Swagger group.

---

## Database Schema

### Tables

```sql
-- Workflow definitions
CREATE TABLE UmbracoAIAgent_Workflow (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Alias           NVARCHAR(255)    NOT NULL UNIQUE,
    Name            NVARCHAR(255)    NOT NULL,
    Description     NVARCHAR(MAX)    NULL,
    StartingStepId  UNIQUEIDENTIFIER NOT NULL,
    InputSchema     NVARCHAR(MAX)    NULL,
    ScopeIds        NVARCHAR(MAX)    NULL,   -- JSON array
    ContextIds      NVARCHAR(MAX)    NULL,   -- JSON array
    IsActive        BIT              NOT NULL DEFAULT 1,
    Version         INT              NOT NULL DEFAULT 1,
    DateCreated     DATETIME2        NOT NULL,
    DateModified    DATETIME2        NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    ModifiedByUserId UNIQUEIDENTIFIER NULL
);

-- Steps within a workflow (stored as JSON in the parent, or normalized)
CREATE TABLE UmbracoAIAgent_WorkflowStep (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    WorkflowId      UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_Workflow(Id) ON DELETE CASCADE,
    Name            NVARCHAR(255)    NOT NULL,
    Type            INT              NOT NULL,  -- enum AIWorkflowStepType
    Configuration   NVARCHAR(MAX)    NOT NULL,  -- JSON
    PositionX       FLOAT            NOT NULL DEFAULT 0,
    PositionY       FLOAT            NOT NULL DEFAULT 0
);

-- Edges connecting steps
CREATE TABLE UmbracoAIAgent_WorkflowEdge (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    WorkflowId      UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_Workflow(Id) ON DELETE CASCADE,
    SourceStepId    UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_WorkflowStep(Id),
    TargetStepId    UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_WorkflowStep(Id),
    Type            INT              NOT NULL,  -- enum AIWorkflowEdgeType
    Branch          NVARCHAR(255)    NULL,
    Label           NVARCHAR(255)    NULL
);

-- Workflow run execution records
CREATE TABLE UmbracoAIAgent_WorkflowRun (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    WorkflowId      UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_Workflow(Id),
    TriggerType     NVARCHAR(50)     NOT NULL,
    TriggerData     NVARCHAR(MAX)    NULL,      -- JSON
    Status          INT              NOT NULL,   -- enum AIWorkflowRunStatus
    State           NVARCHAR(MAX)    NOT NULL DEFAULT '{}',
    Input           NVARCHAR(MAX)    NULL,
    Output          NVARCHAR(MAX)    NULL,
    DateStarted     DATETIME2        NOT NULL,
    DateCompleted   DATETIME2        NULL,
    StartedByUserId UNIQUEIDENTIFIER NULL
);

-- Per-step execution log within a run
CREATE TABLE UmbracoAIAgent_WorkflowStepExecution (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    RunId           UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_WorkflowRun(Id) ON DELETE CASCADE,
    StepId          UNIQUEIDENTIFIER NOT NULL,
    StepName        NVARCHAR(255)    NOT NULL,
    Status          INT              NOT NULL,   -- enum AIWorkflowStepExecutionStatus
    Output          NVARCHAR(MAX)    NULL,
    Error           NVARCHAR(MAX)    NULL,
    DateStarted     DATETIME2        NOT NULL,
    DateCompleted   DATETIME2        NULL
);

-- Workflow triggers (which content types/events trigger which workflows)
CREATE TABLE UmbracoAIAgent_WorkflowTrigger (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    WorkflowId      UNIQUEIDENTIFIER NOT NULL REFERENCES UmbracoAIAgent_Workflow(Id) ON DELETE CASCADE,
    TriggerType     NVARCHAR(50)     NOT NULL,  -- "contentPublish", "contentSave", etc.
    ContentTypeAlias NVARCHAR(255)   NULL,       -- NULL = any content type
    IsEnabled       BIT              NOT NULL DEFAULT 1
);
```

Migration prefix: `UmbracoAIAgent_` (shared with existing agent migrations).

---

## Implementation Phases

### Phase 1 — Foundation (MVP)

**Goal:** Basic sequential workflows with agent steps and manual triggers.

- [ ] Domain models: `AIWorkflow`, `AIWorkflowStep`, `AIWorkflowEdge`
- [ ] `IAIWorkflowService` with CRUD operations
- [ ] `IAIWorkflowEngine` — compiles workflow to MAF `WorkflowBuilder`
- [ ] `AgentStepExecutor` — runs existing agents within a workflow
- [ ] `TransformStepExecutor` — basic data mapping between steps
- [ ] `IAIWorkflowRunService` — start/monitor/cancel runs
- [ ] `AIWorkflowRun` persistence with step execution log
- [ ] SSE streaming endpoint bridging MAF events → AG-UI
- [ ] Management API: workflow CRUD + run start/stream
- [ ] EF Core migrations for all tables
- [ ] Frontend: basic workflow list view
- [ ] Frontend: simple sequential workflow editor (no canvas — just ordered step list)
- [ ] Frontend: run monitor with step progress and streaming output

**NuGet dependency:** `Microsoft.Agents.AI.Workflows`

### Phase 2 — Visual Editor & Branching

**Goal:** Full visual canvas editor with conditional branching and parallel execution.

- [ ] `ConditionStepExecutor` — evaluates predicates for branching
- [ ] Conditional and fan-out/fan-in edges
- [ ] Frontend: full canvas-based workflow editor (SVG edges, draggable nodes)
- [ ] Frontend: edge drawing via drag-from-port interaction
- [ ] Frontend: step configuration side panel
- [ ] Frontend: template editor with `{{variable}}` autocomplete from upstream state keys
- [ ] Workflow validation (cycle detection, unreachable steps, missing configs)

### Phase 3 — Content Integration & Approvals

**Goal:** Deep Umbraco content integration and human-in-the-loop approval.

- [ ] `ContentReadExecutor` — loads content node properties into state
- [ ] `ContentWriteExecutor` — writes state back to content properties
- [ ] `ApprovalStepExecutor` — pauses for human review via AG-UI interrupts
- [ ] Resume endpoint for approval responses
- [ ] Content app showing available workflows and run history
- [ ] Event-driven triggers via Umbraco notifications
- [ ] `WorkflowTrigger` configuration (content type + event → workflow)
- [ ] Approval notification system (backoffice alerts, optional email)

### Phase 4 — Advanced Features

**Goal:** Production-grade features for complex workflows.

- [ ] `SubWorkflowStepExecutor` — embed workflows within workflows
- [ ] Workflow version history and rollback (existing versioning pattern)
- [ ] Checkpointing via MAF `CheckpointManager` for fault tolerance
- [ ] Run retry/resume from last checkpoint
- [ ] Scheduled workflow triggers (cron-style)
- [ ] Workflow templates / presets (pre-built workflows users can import)
- [ ] Run analytics (duration, success rate, cost tracking per step)
- [ ] Bulk operations (run workflow on multiple content nodes)

---

## Open Questions

1. **State size limits** — Workflow state accumulates output from every step. For content-heavy workflows (e.g., translating long articles), should we store large outputs in blob storage and keep only references in state?

2. **Concurrent run limits** — Should there be a configurable max concurrent runs per workflow to prevent resource exhaustion? Per-site? Per-user?

3. **MAF version pinning** — MAF is in preview targeting Q1 2026 GA. Should we wait for GA or build against preview packages with an abstraction layer for API changes?

4. **Declarative workflows** — MAF supports YAML/JSON declarative workflow definitions. Should we support importing/exporting workflows in this format for portability?

5. **Agent tool calls within workflows** — When an agent step calls tools (MCP, frontend), should those tool calls be visible in the workflow monitor? Or should each agent step be treated as a black box?

6. **Content locking** — When a workflow is writing to a content node, should we lock the node to prevent concurrent edits? Umbraco doesn't have native content locking, so this would need a custom implementation.
