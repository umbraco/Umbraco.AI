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

### Key Insight: All Orchestrations Are Workflows

In MAF, orchestration patterns (Sequential, Concurrent, Handoff, Magentic) are **not a separate API** — they are factory methods that produce `Workflow` objects via `WorkflowBuilder`. Every orchestration compiles down to the same graph of executors and edges.

This means our design doesn't need separate runtime code paths for each pattern. Instead, each orchestration type is a different **compilation strategy** that produces a MAF `Workflow`:

```
AIWorkflow (Umbraco definition)
    │
    ├─ Mode: Graph      ──▶  WorkflowBuilder (manual edges)
    ├─ Mode: Sequential  ──▶  AgentWorkflowBuilder.BuildSequential()
    ├─ Mode: Concurrent  ──▶  AgentWorkflowBuilder.BuildConcurrent()
    ├─ Mode: Handoff     ──▶  HandoffsWorkflowBuilder
    └─ Mode: Magentic    ──▶  GroupChatWorkflowBuilder + MagenticManager
                                    │
                                    ▼
                              MAF Workflow  ──▶  InProcessExecution.StreamAsync()
```

### Mapping to MAF

| Umbraco Concept | MAF Concept | Description |
|---|---|---|
| **AIWorkflow** | `Workflow` (via builders) | The full graph definition |
| **AIWorkflowStep** | `Executor<TIn, TOut>` | A node in the graph — agent or deterministic function |
| **AIWorkflowEdge** | `Edge` (simple, conditional, fan-out/fan-in) | A directed connection between steps (Graph mode) |
| **AIWorkflowOrchestrationMode** | Builder selection | Which factory method compiles the workflow |
| **AIWorkflowRun** | `StreamingRun` | A single execution of a workflow |
| **Shared State** | `IWorkflowContext` | Data passed between steps via scoped state |

### Orchestration Modes

Each workflow has an **orchestration mode** that determines its topology, editing UI, and compilation strategy:

```
┌──────────────┬──────────────────────────────────────────────────────────────────────┐
│ Mode         │ Description                                                          │
├──────────────┼──────────────────────────────────────────────────────────────────────┤
│              │                                                                      │
│ Graph        │ Full visual canvas editor. User defines steps and edges manually.    │
│              │ Supports all step types (agent, transform, condition, approval,      │
│              │ content, sub-workflow). Maximum flexibility.                          │
│              │                                                                      │
│              │ MAF: WorkflowBuilder with manual AddEdge / AddFanOutEdge /           │
│              │       AddFanInEdge / AddSwitch calls.                                │
│              │                                                                      │
│              │ UI: Node canvas with drag-to-connect edges.                          │
│              │                                                                      │
├──────────────┼──────────────────────────────────────────────────────────────────────┤
│              │                                                                      │
│ Sequential   │ Pipeline: each agent processes in order, passing output as input     │
│              │ to the next. Previous agent's output becomes the next agent's user   │
│              │ message (via MAF's ReassignOtherAgentsAsUsers).                      │
│              │                                                                      │
│              │ MAF: AgentWorkflowBuilder.BuildSequential(agents)                    │
│              │                                                                      │
│              │ UI: Simple ordered list — drag to reorder agents.                    │
│              │                                                                      │
│              │ Example: Content Review → Translation → Quality Check → Publish      │
│              │                                                                      │
├──────────────┼──────────────────────────────────────────────────────────────────────┤
│              │                                                                      │
│ Concurrent   │ Parallel: all agents receive the same input simultaneously.          │
│              │ Results are aggregated via a configurable strategy (last message,    │
│              │ merge all, custom).                                                   │
│              │                                                                      │
│              │ MAF: AgentWorkflowBuilder.BuildConcurrent(agents, aggregator)        │
│              │                                                                      │
│              │ UI: Agent list (unordered) + aggregation strategy picker.             │
│              │                                                                      │
│              │ Example: Three reviewers analyze content independently → merge        │
│              │          their feedback into a consolidated report.                   │
│              │                                                                      │
├──────────────┼──────────────────────────────────────────────────────────────────────┤
│              │                                                                      │
│ Handoff      │ Mesh: agents dynamically transfer control to each other based on     │
│              │ conversation context. Each agent gets AI tool functions               │
│              │ (handoff_to_N) for transferring to other agents. The LLM             │
│              │ decides when to hand off.                                             │
│              │                                                                      │
│              │ MAF: HandoffsWorkflowBuilder with WithHandoff() relationships        │
│              │                                                                      │
│              │ UI: Agent list + handoff relationship matrix (who can hand off to     │
│              │      whom, with optional handoff reasons).                            │
│              │                                                                      │
│              │ Example: Triage agent routes to Content Specialist, SEO Expert,       │
│              │          or Translation Agent based on user request.                  │
│              │                                                                      │
├──────────────┼──────────────────────────────────────────────────────────────────────┤
│              │                                                                      │
│ Magentic     │ Manager + Specialists: a manager agent creates a task ledger and     │
│              │ dynamically dispatches subtasks to specialist agents. Uses a          │
│              │ two-loop architecture (task planning + progress tracking).            │
│              │                                                                      │
│              │ MAF: GroupChatWorkflowBuilder + StandardMagenticManager              │
│              │                                                                      │
│              │ UI: Pick manager agent/profile + specialist agent list + config       │
│              │      (max iterations, stall threshold).                               │
│              │                                                                      │
│              │ Example: Manager analyzes a content brief, dispatches Researcher,     │
│              │          Writer, and SEO Specialist as needed, re-plans on stalls.   │
│              │                                                                      │
└──────────────┴──────────────────────────────────────────────────────────────────────┘
```

### How Each Mode Compiles

#### Sequential

```csharp
// User defines: [Agent A] → [Agent B] → [Agent C]
// Compiles to:
var agents = steps.Select(s => ResolveAgent(s)).ToArray();
Workflow wf = AgentWorkflowBuilder.BuildSequential(agents);
// MAF chains: A.AddEdge(B).AddEdge(C).WithOutputFrom(C)
// Each agent sees previous output as user message (ReassignOtherAgentsAsUsers=true)
```

#### Concurrent

```csharp
// User defines: [Agent A, Agent B, Agent C] + aggregation strategy
// Compiles to:
var agents = steps.Select(s => ResolveAgent(s)).ToArray();
Func<IList<List<ChatMessage>>, List<ChatMessage>> aggregator = config.AggregationStrategy switch
{
    "lastMessage" => lists => lists.Select(l => l.Last()).ToList(),     // default
    "mergeAll"    => lists => lists.SelectMany(l => l).ToList(),        // combine everything
    "summarize"   => lists => CreateSummaryAggregator(lists, config),   // use an LLM to summarize
    _ => null  // use MAF default
};
Workflow wf = AgentWorkflowBuilder.BuildConcurrent(agents, aggregator);
// MAF creates: Start → FanOut → [A, B, C in parallel] → FanIn(aggregator) → Output
```

#### Handoff

```csharp
// User defines: agents + handoff matrix [{from: A, to: B, reason: "..."}, ...]
// Compiles to:
var builder = AgentWorkflowBuilder.CreateHandoffBuilderWith(initialAgent);
foreach (var handoff in config.Handoffs)
{
    builder.WithHandoff(
        from: ResolveAgent(handoff.FromAgentId),
        to: ResolveAgent(handoff.ToAgentId),
        handoffReason: handoff.Reason  // becomes the tool description the LLM sees
    );
}
Workflow wf = builder.Build();
// MAF creates: Start → InitialAgent ─[AddSwitch]→ {handoff_to_1→AgentB, handoff_to_2→AgentC, default→End}
// Each agent gets handoff_to_N tool functions injected; LLM decides when to transfer
```

#### Magentic

```csharp
// User defines: manager profile + specialist agents + config (maxIterations, stallThreshold)
// Compiles to:
var managerChatClient = await CreateChatClientForProfile(config.ManagerProfileId);
var manager = new StandardMagenticManager(managerChatClient, new()
{
    MaximumInvocationCount = config.MaxIterations ?? 5,
    // StallThreshold, etc.
});

var specialists = steps.Select(s => ResolveAgent(s)).ToArray();
var builder = AgentWorkflowBuilder.CreateGroupChatBuilderWith(manager, specialists);
Workflow wf = builder.Build();
// MAF creates: GroupChatHost executor that loops:
//   1. Manager evaluates progress (Task Ledger + Progress Ledger)
//   2. Manager picks next specialist via SelectNextAgentAsync()
//   3. Specialist executes with manager's instruction
//   4. Repeat until is_request_satisfied or max iterations
```

### Step Types

Steps available depend on the orchestration mode:

| Step Type | Graph | Sequential | Concurrent | Handoff | Magentic |
|---|---|---|---|---|---|
| **AgentStep** | Yes | Yes | Yes | Yes | Yes |
| **TransformStep** | Yes | - | - | - | - |
| **ConditionStep** | Yes | - | - | - | - |
| **ApprovalStep** | Yes | Yes* | - | - | - |
| **ContentRead** | Yes | Yes* | - | - | - |
| **ContentWrite** | Yes | Yes* | - | - | - |
| **SubWorkflowStep** | Yes | - | - | - | - |

\* In Sequential mode, approval/content steps are inserted between agent steps as non-agent executors.

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

### Composability: Workflows as Agents

MAF's `Workflow.AsAgent()` extension converts any workflow into an `AIAgent`. This means:

1. A **Sequential** workflow can be embedded as a single step inside a **Graph** workflow
2. A **Handoff** workflow can be one of the specialists inside a **Magentic** workflow
3. A **Concurrent** "brainstorm" workflow can be one step in a **Sequential** pipeline

```
Graph Workflow
├─ [ContentRead]
├─ [SubWorkflow: Sequential Translation Pipeline]  ← entire workflow as one step
│      ├─ Agent: Translate
│      ├─ Agent: Review
│      └─ Agent: Polish
├─ [Approval]
└─ [ContentWrite]
```

In the domain model, `SubWorkflowStep` references another `AIWorkflow` by ID. At compilation time, the referenced workflow is compiled to a MAF `Workflow`, converted to an `AIAgent` via `.AsAgent()`, and bound as an executor in the parent graph.

### Execution Model

All modes execute using MAF's **Pregel / Bulk Synchronous Parallel** model:

1. Input enters the starting executor
2. Executors in the same "superstep" run concurrently
3. Output is routed along edges to downstream executors
4. Approval steps **pause** execution until human resumes
5. The run completes when all terminal executors finish

**Graph mode example:**

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

**Handoff mode example:**

```
User Request ──▶ [Triage Agent]
                      │
                ┌─────┼──────────────┐
                │     │              │
        handoff_to_1  handoff_to_2   handoff_to_3
                │     │              │
                ▼     ▼              ▼
         [Content    [SEO         [Translation
          Specialist] Expert]      Agent]
                │     │              │
                └──┬──┘──────────────┘
                   │ (can hand back to Triage)
                   ▼
               [Complete]
```

**Magentic mode example:**

```
Task Input ──▶ [Manager Agent]
                    │
              ┌─────┼──────────────────┐
              │  Progress Ledger Loop   │
              │                         │
              │  1. Evaluate progress   │
              │  2. Pick next speaker   │
              │  3. Dispatch with       │
              │     instructions        │
              │                         │
              ├──▶ [Researcher] ────────┤
              ├──▶ [Writer] ────────────┤
              ├──▶ [SEO Specialist] ────┤
              │                         │
              │  4. Assess results      │
              │  5. Re-plan if stalled  │
              │  6. Repeat or finish    │
              └─────────────────────────┘
                    │
                    ▼
              [Summary Agent] ──▶ Output
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
    /// The orchestration mode determines the workflow's topology and compilation strategy.
    /// </summary>
    public required AIWorkflowOrchestrationMode Mode { get; set; }

    /// <summary>
    /// The step that receives initial input when the workflow starts.
    /// Only used in Graph mode — other modes derive the starting point automatically.
    /// </summary>
    public Guid? StartingStepId { get; set; }

    /// <summary>
    /// All steps in this workflow.
    /// In Sequential mode, order matters. In Concurrent/Handoff/Magentic, order is display-only.
    /// </summary>
    public IReadOnlyList<AIWorkflowStep> Steps { get; set; } = [];

    /// <summary>
    /// All edges connecting steps. Only used in Graph mode.
    /// Other modes generate edges automatically during compilation.
    /// </summary>
    public IReadOnlyList<AIWorkflowEdge> Edges { get; set; } = [];

    /// <summary>
    /// Mode-specific configuration. Null for Graph mode.
    /// See: SequentialConfig, ConcurrentConfig, HandoffConfig, MagenticConfig.
    /// </summary>
    public string? OrchestrationConfig { get; set; }

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

public enum AIWorkflowOrchestrationMode
{
    /// <summary>Full visual canvas — user defines steps and edges manually.</summary>
    Graph,

    /// <summary>Pipeline — agents process in order, output becomes next input.</summary>
    Sequential,

    /// <summary>Parallel — all agents process same input, results aggregated.</summary>
    Concurrent,

    /// <summary>Mesh — agents dynamically transfer control via handoff tool functions.</summary>
    Handoff,

    /// <summary>Manager + Specialists — manager dispatches subtasks dynamically.</summary>
    Magentic
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

### Orchestration Mode Configurations

Each orchestration mode (except Graph) stores its configuration in `AIWorkflow.OrchestrationConfig` as JSON:

```csharp
// Sequential — no extra config needed beyond step order.
// Steps are executed in list order. The step list IS the config.
public sealed class SequentialOrchestrationConfig
{
    // Reserved for future options (e.g., error handling strategy)
}

// Concurrent — configures how parallel results are combined.
public sealed class ConcurrentOrchestrationConfig
{
    /// <summary>
    /// How to combine results from all agents.
    /// "lastMessage" — take last message from each (default, MAF default)
    /// "mergeAll"    — concatenate all messages
    /// "summarize"   — use an LLM to synthesize a summary (requires SummaryProfileId)
    /// </summary>
    public string AggregationStrategy { get; set; } = "lastMessage";

    /// <summary>
    /// Profile for the summarization LLM when AggregationStrategy is "summarize".
    /// </summary>
    public Guid? SummaryProfileId { get; set; }

    /// <summary>
    /// Optional prompt template for the summary agent.
    /// Default: "Synthesize the following responses into a single coherent answer: {{responses}}"
    /// </summary>
    public string? SummaryPromptTemplate { get; set; }
}

// Handoff — defines the agent transfer relationships.
public sealed class HandoffOrchestrationConfig
{
    /// <summary>
    /// The step ID of the initial agent that receives the first message.
    /// </summary>
    public required Guid InitialAgentStepId { get; set; }

    /// <summary>
    /// Directed handoff relationships between agents.
    /// Each entry allows "from" to transfer to "to" with a reason.
    /// </summary>
    public IReadOnlyList<AIWorkflowHandoff> Handoffs { get; set; } = [];

    /// <summary>
    /// Whether the workflow supports interactive mode (user can send
    /// follow-up messages, like a multi-agent chat).
    /// </summary>
    public bool Interactive { get; set; } = false;
}

public sealed class AIWorkflowHandoff
{
    /// <summary>
    /// The step ID of the agent that can initiate this handoff.
    /// </summary>
    public required Guid FromStepId { get; set; }

    /// <summary>
    /// The step ID of the agent that receives the handoff.
    /// </summary>
    public required Guid ToStepId { get; set; }

    /// <summary>
    /// Why this handoff exists — becomes the tool function description
    /// that the LLM sees when deciding whether to transfer.
    /// Falls back to the target agent's description if not set.
    /// </summary>
    public string? Reason { get; set; }
}

// Magentic — configures the manager + specialist dynamic dispatch pattern.
public sealed class MagenticOrchestrationConfig
{
    /// <summary>
    /// Profile for the manager agent that creates task/progress ledgers
    /// and dispatches work. Should be a capable reasoning model.
    /// </summary>
    public required Guid ManagerProfileId { get; set; }

    /// <summary>
    /// Optional custom instructions for the manager agent.
    /// Merged with the default Magentic planning prompts.
    /// </summary>
    public string? ManagerInstructions { get; set; }

    /// <summary>
    /// Maximum number of agent invocations before the manager
    /// is forced to produce a final answer. Default: 10.
    /// </summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// Number of consecutive stalls (no progress) before
    /// the outer loop re-plans. Default: 2.
    /// </summary>
    public int StallThreshold { get; set; } = 2;

    /// <summary>
    /// Maximum number of outer-loop re-plans before terminating
    /// with failure. Default: 2.
    /// </summary>
    public int MaxReplans { get; set; } = 2;
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

The engine dispatches to the correct MAF builder based on orchestration mode:

```csharp
public class AIWorkflowEngine : IAIWorkflowEngine
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly IContentService _contentService;

    public async Task<StreamingRun> CompileAndStartAsync(
        AIWorkflow workflow,
        string input,
        AGUIEventEmitter emitter,
        CancellationToken ct)
    {
        Workflow mafWorkflow = workflow.Mode switch
        {
            AIWorkflowOrchestrationMode.Graph      => await CompileGraphAsync(workflow, emitter, ct),
            AIWorkflowOrchestrationMode.Sequential  => await CompileSequentialAsync(workflow, emitter, ct),
            AIWorkflowOrchestrationMode.Concurrent  => await CompileConcurrentAsync(workflow, emitter, ct),
            AIWorkflowOrchestrationMode.Handoff     => await CompileHandoffAsync(workflow, emitter, ct),
            AIWorkflowOrchestrationMode.Magentic    => await CompileMagenticAsync(workflow, emitter, ct),
            _ => throw new NotSupportedException($"Mode {workflow.Mode} is not supported")
        };

        return await InProcessExecution.StreamAsync(mafWorkflow, input);
    }
}
```

#### Graph Mode Compilation

Full manual control — user-defined steps and edges:

```csharp
private async Task<Workflow> CompileGraphAsync(
    AIWorkflow workflow, AGUIEventEmitter emitter, CancellationToken ct)
{
    // 1. Create executors for each step
    var executors = new Dictionary<Guid, IExecutor>();
    foreach (var step in workflow.Steps)
    {
        executors[step.Id] = step.Type switch
        {
            AIWorkflowStepType.Agent       => await CreateAgentExecutorAsync(step, emitter, ct),
            AIWorkflowStepType.Transform   => CreateTransformExecutor(step),
            AIWorkflowStepType.Condition   => CreateConditionExecutor(step),
            AIWorkflowStepType.Approval    => CreateApprovalExecutor(step, emitter),
            AIWorkflowStepType.ContentRead => CreateContentReadExecutor(step),
            AIWorkflowStepType.ContentWrite=> CreateContentWriteExecutor(step),
            AIWorkflowStepType.SubWorkflow => await CreateSubWorkflowExecutorAsync(step, emitter, ct),
            _ => throw new NotSupportedException($"Step type {step.Type}")
        };
    }

    // 2. Build the graph manually from user-defined edges
    var builder = new WorkflowBuilder(executors[workflow.StartingStepId!.Value]);

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
            // Fan-out/fan-in edges collected into groups and added in batch
        }
    }

    // 3. Mark terminal steps as outputs
    foreach (var id in FindTerminalStepIds(workflow))
        builder.WithOutputFrom(executors[id]);

    return builder.Build();
}
```

#### Sequential Mode Compilation

Steps executed in list order — each agent's output becomes the next agent's input:

```csharp
private async Task<Workflow> CompileSequentialAsync(
    AIWorkflow workflow, AGUIEventEmitter emitter, CancellationToken ct)
{
    // Resolve each step to a MAF AIAgent
    var agents = new List<AIAgent>();
    foreach (var step in workflow.Steps)
    {
        var agent = await ResolveStepToMAFAgentAsync(step, emitter, ct);
        agents.Add(agent);
    }

    // MAF handles the chaining automatically:
    // - AddEdge between each pair
    // - ReassignOtherAgentsAsUsers = true (prev output → next input)
    // - ForwardIncomingMessages = true
    return AgentWorkflowBuilder.BuildSequential(agents.ToArray());
}
```

#### Concurrent Mode Compilation

All agents process the same input in parallel, results aggregated:

```csharp
private async Task<Workflow> CompileConcurrentAsync(
    AIWorkflow workflow, AGUIEventEmitter emitter, CancellationToken ct)
{
    var config = Deserialize<ConcurrentOrchestrationConfig>(workflow.OrchestrationConfig);

    var agents = new List<AIAgent>();
    foreach (var step in workflow.Steps)
        agents.Add(await ResolveStepToMAFAgentAsync(step, emitter, ct));

    // Build the aggregation function
    Func<IList<List<ChatMessage>>, List<ChatMessage>>? aggregator = config.AggregationStrategy switch
    {
        "lastMessage" => null,  // MAF default: last message from each
        "mergeAll" => lists => lists.SelectMany(l => l).ToList(),
        "summarize" => await CreateSummaryAggregatorAsync(config, ct),
        _ => null
    };

    // MAF creates: Start → FanOut → [agents in parallel] → FanIn(aggregator) → Output
    return AgentWorkflowBuilder.BuildConcurrent(agents.ToArray(), aggregator);
}

// The "summarize" aggregator uses an LLM to merge parallel results
private async Task<Func<IList<List<ChatMessage>>, List<ChatMessage>>>
    CreateSummaryAggregatorAsync(ConcurrentOrchestrationConfig config, CancellationToken ct)
{
    var chatClient = await _chatClientFactory.CreateChatClientAsync(config.SummaryProfileId!.Value, ct);
    var template = config.SummaryPromptTemplate
        ?? "Synthesize these responses into a single coherent answer:\n\n{{responses}}";

    return lists =>
    {
        var responses = string.Join("\n\n---\n\n",
            lists.SelectMany(l => l).Select(m => m.Text));
        var prompt = template.Replace("{{responses}}", responses);

        // Run synchronously within the aggregator (MAF constraint)
        var result = chatClient.GetResponseAsync([new(ChatRole.User, prompt)]).GetAwaiter().GetResult();
        return [new ChatMessage(ChatRole.Assistant, result.Text)];
    };
}
```

#### Handoff Mode Compilation

Agents hand off control to each other dynamically via tool functions:

```csharp
private async Task<Workflow> CompileHandoffAsync(
    AIWorkflow workflow, AGUIEventEmitter emitter, CancellationToken ct)
{
    var config = Deserialize<HandoffOrchestrationConfig>(workflow.OrchestrationConfig);

    // Resolve all agents
    var agentMap = new Dictionary<Guid, AIAgent>();
    foreach (var step in workflow.Steps)
        agentMap[step.Id] = await ResolveStepToMAFAgentAsync(step, emitter, ct);

    // Build the handoff relationships
    var initialAgent = agentMap[config.InitialAgentStepId];
    var builder = AgentWorkflowBuilder.CreateHandoffBuilderWith(initialAgent);

    foreach (var handoff in config.Handoffs)
    {
        builder.WithHandoff(
            from: agentMap[handoff.FromStepId],
            to: agentMap[handoff.ToStepId],
            handoffReason: handoff.Reason  // LLM sees this as the handoff tool description
        );
    }

    // MAF creates for each agent:
    //   - handoff_to_N tool functions (one per valid target)
    //   - AddSwitch routing based on which tool the LLM calls
    //   - Full conversation history carried in HandoffState
    return builder.Build();
}
```

#### Magentic Mode Compilation

Manager + specialist agents with dynamic task dispatch:

```csharp
private async Task<Workflow> CompileMagenticAsync(
    AIWorkflow workflow, AGUIEventEmitter emitter, CancellationToken ct)
{
    var config = Deserialize<MagenticOrchestrationConfig>(workflow.OrchestrationConfig);

    // Create the manager (needs a capable reasoning model)
    var managerClient = await _chatClientFactory.CreateChatClientAsync(config.ManagerProfileId, ct);
    var manager = new StandardMagenticManager(managerClient, new()
    {
        MaximumInvocationCount = config.MaxIterations,
        // StallThreshold and MaxReplans configured via manager prompts
    });

    if (config.ManagerInstructions is not null)
        manager.Instructions = config.ManagerInstructions;

    // Resolve specialist agents
    var specialists = new List<AIAgent>();
    foreach (var step in workflow.Steps)
        specialists.Add(await ResolveStepToMAFAgentAsync(step, emitter, ct));

    // MAF creates a GroupChatHost that loops:
    //   1. Manager evaluates Task Ledger (facts, plan)
    //   2. Manager produces Progress Ledger (JSON):
    //      { is_request_satisfied, is_in_loop, is_progress_being_made,
    //        next_speaker, instruction_or_question }
    //   3. Selected specialist executes with instruction
    //   4. Results fed back to manager
    //   5. Re-plans on stall detection
    return AgentWorkflowBuilder.CreateGroupChatBuilderWith(manager, specialists.ToArray()).Build();
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

### Mode Selection — Create Workflow

When creating a new workflow, the user first picks the orchestration mode. This determines which editor they see:

```
┌─────────────────────────────────────────────────────────────┐
│  Create Workflow                                             │
│                                                              │
│  Choose an orchestration mode:                               │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ⊞ Graph                                              │  │
│  │  Full visual canvas. Define steps and connections      │  │
│  │  manually. Maximum flexibility for complex workflows.  │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  → Sequential                                         │  │
│  │  Pipeline: agents process in order. Output from one   │  │
│  │  becomes input to the next.                           │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ⇉ Concurrent                                         │  │
│  │  Parallel: all agents receive the same input. Results │  │
│  │  are combined using a strategy you choose.            │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ⇄ Handoff                                            │  │
│  │  Mesh: agents transfer control to each other based on │  │
│  │  context. LLM decides when to hand off.               │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ✦ Magentic                                           │  │
│  │  Manager + Specialists: a manager agent plans tasks   │  │
│  │  and dispatches work to specialist agents dynamically. │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Editor — Graph Mode (Visual Canvas)

The Graph mode editor is a node-based visual canvas built with Lit web components. Users drag-and-drop steps onto a canvas and connect them with edges.

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

### Editor — Sequential Mode

A simple ordered list of agents. Drag to reorder. Each agent's output automatically becomes the next agent's user message.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Workflow: Content Review Pipeline (Sequential)            [Save] [Run] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Pipeline Steps (drag to reorder)                                       │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  1.  ≡  Agent: "Content Reviewer"                        [Edit] │   │
│  │        Reviews content for quality, grammar, and tone           │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │         ↓  output becomes input                                  │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  2.  ≡  Agent: "SEO Optimizer"                           [Edit] │   │
│  │        Optimizes headings, meta description, keyword density    │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │         ↓  output becomes input                                  │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  3.  ≡  Agent: "Final Editor"                            [Edit] │   │
│  │        Polishes and produces the final version                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  [+ Add Agent Step]                                                     │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Editor — Concurrent Mode

Unordered list of agents that process in parallel, plus aggregation strategy configuration:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Workflow: Multi-Perspective Review (Concurrent)           [Save] [Run] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Parallel Agents (all receive the same input)                           │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Agent: "Technical Reviewer"                             [Edit] │   │
│  │  Reviews for technical accuracy                                 │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "Brand Voice Reviewer"                           [Edit] │   │
│  │  Checks tone and brand consistency                              │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "Accessibility Reviewer"                         [Edit] │   │
│  │  Checks readability and accessibility compliance                │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  [+ Add Agent]                                                          │
│                                                                          │
│  ── Aggregation Strategy ──────────────────────────────────────────     │
│                                                                          │
│  ○ Last message from each agent (default)                               │
│  ○ Merge all messages                                                   │
│  ● Summarize with LLM                                                   │
│    Profile: [AI Profile Picker: "GPT-4o Summary" ▾]                    │
│    Prompt:  [Synthesize these reviews into actionable feedback: ...]    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Editor — Handoff Mode

Configure which agents can transfer to which, with a visual relationship matrix:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Workflow: Customer Support Triage (Handoff)               [Save] [Run] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Agents                                           Initial Agent: ▾      │
│                                                   [Triage Agent]        │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Agent: "Triage Agent"                                   [Edit] │   │
│  │  Routes requests to the right specialist                        │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "Content Specialist"                             [Edit] │   │
│  │  Handles content editing and publishing questions                │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "SEO Expert"                                     [Edit] │   │
│  │  Handles SEO-related questions and optimization                 │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "Translation Agent"                              [Edit] │   │
│  │  Handles localization and translation requests                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  [+ Add Agent]                                                          │
│                                                                          │
│  ── Handoff Relationships ─────────────────────────────────────────     │
│  (Who can transfer to whom? LLM decides when based on the reason.)     │
│                                                                          │
│     From              → To                   Reason                     │
│  ┌──────────────────┬───────────────────┬─────────────────────────┐    │
│  │  Triage Agent     │ Content Spec.     │ "Content editing help"  │    │
│  │  Triage Agent     │ SEO Expert        │ "SEO questions"         │    │
│  │  Triage Agent     │ Translation Agent │ "Translation requests"  │    │
│  │  Content Spec.    │ Triage Agent      │ "Not content-related"   │    │
│  │  SEO Expert       │ Triage Agent      │ "Not SEO-related"       │    │
│  │  Translation Agent│ Triage Agent      │ "Not translation-rel."  │    │
│  └──────────────────┴───────────────────┴─────────────────────────┘    │
│  [+ Add Handoff Rule]                                                   │
│                                                                          │
│  ☐ Interactive mode (user can send follow-up messages)                  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Editor — Magentic Mode

Pick a manager profile and specialist agents. The manager dynamically plans and dispatches:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Workflow: Deep Content Research (Magentic)                [Save] [Run] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ── Manager Agent ─────────────────────────────────────────────────     │
│                                                                          │
│  Profile: [AI Profile Picker: "Claude Opus (Reasoning)" ▾]             │
│  Instructions (optional):                                               │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ You are managing a content research workflow for Umbraco CMS.   │   │
│  │ Prioritize accuracy and source citations. Re-plan if the       │   │
│  │ writer produces content without supporting research.            │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  ── Specialist Agents ─────────────────────────────────────────────     │
│  (Manager dispatches work to these agents based on its task plan)       │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Agent: "Researcher"                                     [Edit] │   │
│  │  Finds and summarizes relevant information                      │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "Writer"                                         [Edit] │   │
│  │  Drafts content sections based on research                      │   │
│  ├──────────────────────────────────────────────────────────────────┤   │
│  │  Agent: "SEO Specialist"                                 [Edit] │   │
│  │  Optimizes content for search engines                           │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  [+ Add Specialist]                                                     │
│                                                                          │
│  ── Execution Limits ──────────────────────────────────────────────     │
│                                                                          │
│  Max iterations:    [10  ▾]  (max agent invocations before forced end)  │
│  Stall threshold:   [2   ▾]  (stalls before re-planning)               │
│  Max re-plans:      [2   ▾]  (re-plans before terminating with error)  │
│                                                                          │
│  ── How It Works ──────────────────────────────────────────────────     │
│  The manager agent will:                                                │
│  1. Analyze the input and create a task plan (Task Ledger)             │
│  2. Assess progress after each specialist completes (Progress Ledger)  │
│  3. Pick the next specialist and provide specific instructions         │
│  4. Re-plan if progress stalls                                         │
│  5. Produce a final answer when the request is satisfied               │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Workflow Run Monitor

When a workflow is executing, the UI shows real-time progress. The monitor adapts to the orchestration mode:

**Graph & Sequential** — step-by-step progress with streaming output:

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

**Concurrent** — parallel progress columns showing each agent's output side-by-side:

```
┌─────────────────────────────────────────────────────────────────┐
│  Run: Multi-Perspective Review (Concurrent)           [Cancel]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌───────────────────┬───────────────────┬──────────────────┐   │
│  │ ⏳ Technical       │ ✅ Brand Voice     │ ⏳ Accessibility  │   │
│  │                    │                    │                   │   │
│  │ The code examples  │ The tone is        │ Heading levels   │   │
│  │ in section 3 have  │ consistent with    │ skip from H2 to  │   │
│  │ a syntax error...  │ brand guidelines.  │ H4 in section... │   │
│  │ ████████░░░░       │ Done (2.1s)        │ ██████░░░░░      │   │
│  └───────────────────┴───────────────────┴──────────────────┘   │
│                                                                  │
│  Aggregation: Summarize with LLM   Status: Waiting for agents   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Handoff** — chat-style view showing which agent is currently active:

```
┌─────────────────────────────────────────────────────────────────┐
│  Run: Customer Support Triage (Handoff)               [Cancel]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Active Agent: SEO Expert  (handoff from Triage Agent)          │
│                                                                  │
│  ┌─ Triage Agent ──────────────────────────────────────────┐    │
│  │ I'll route this to our SEO expert who can help with     │    │
│  │ your meta description optimization.                      │    │
│  │ → Handed off to SEO Expert                               │    │
│  └──────────────────────────────────────────────────────────┘    │
│  ┌─ SEO Expert ────────────────────────────────────────────┐    │
│  │ Looking at your page's meta description, here are my    │    │
│  │ recommendations: ...                                     │    │
│  │ ████████████████████░░░░░░ streaming...                  │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Transfer History: User → Triage → SEO Expert                   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Magentic** — shows manager's task plan and specialist dispatch log:

```
┌─────────────────────────────────────────────────────────────────┐
│  Run: Deep Content Research (Magentic)                [Cancel]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Manager: Planning... (iteration 3/10)                          │
│                                                                  │
│  ── Task Ledger ───────────────────────────────────────────     │
│  Facts: Found 3 authoritative sources on AI in CMS              │
│  Plan:  1. ✅ Research background  2. ⏳ Draft intro            │
│         3. ○ Draft body  4. ○ SEO optimize  5. ○ Final review   │
│                                                                  │
│  ── Progress Log ──────────────────────────────────────────     │
│  ┌───────┬──────────────┬──────────────────────────────────┐    │
│  │ Turn  │ Agent         │ Task                             │    │
│  │ 1     │ Researcher    │ ✅ Find sources on AI+CMS        │    │
│  │ 2     │ Researcher    │ ✅ Summarize top 3 articles       │    │
│  │ 3     │ Writer        │ ⏳ Draft introduction section     │    │
│  │       │               │ ████████░░░░ streaming...         │    │
│  └───────┴──────────────┴──────────────────────────────────┘    │
│                                                                  │
│  Progress: Making progress | No stalls | 3/10 iterations used   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

Step node status indicators (all modes):

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
    Mode            INT              NOT NULL DEFAULT 0,  -- enum AIWorkflowOrchestrationMode
    StartingStepId  UNIQUEIDENTIFIER NULL,   -- Required for Graph, optional for others
    OrchestrationConfig NVARCHAR(MAX) NULL,  -- Mode-specific JSON (null for Graph)
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

### Phase 1 — Foundation: Sequential & Concurrent

**Goal:** The two simplest orchestration modes with manual triggers. These have the simplest UIs (ordered list / unordered list) and require no edge editing.

- [ ] Domain models: `AIWorkflow`, `AIWorkflowStep`, `AIWorkflowEdge`, orchestration configs
- [ ] `IAIWorkflowService` with CRUD operations + version tracking
- [ ] `IAIWorkflowEngine` — mode-based compilation dispatcher
- [ ] **Sequential compilation** via `AgentWorkflowBuilder.BuildSequential()`
- [ ] **Concurrent compilation** via `AgentWorkflowBuilder.BuildConcurrent()` with aggregation strategies
- [ ] `IAIWorkflowRunService` — start/monitor/cancel runs
- [ ] `AIWorkflowRun` persistence with step execution log
- [ ] SSE streaming endpoint bridging MAF events → AG-UI
- [ ] Management API: workflow CRUD + run start/stream
- [ ] EF Core migrations for all tables
- [ ] Frontend: workflow list view with mode badges
- [ ] Frontend: mode selection on create
- [ ] Frontend: Sequential editor (ordered agent list, drag to reorder)
- [ ] Frontend: Concurrent editor (agent list + aggregation strategy picker)
- [ ] Frontend: run monitor — sequential step-by-step and concurrent parallel columns

**NuGet dependency:** `Microsoft.Agents.AI.Workflows`

### Phase 2 — Handoff & Magentic

**Goal:** The two dynamic orchestration modes where the LLM decides routing.

- [ ] **Handoff compilation** via `HandoffsWorkflowBuilder`
- [ ] Frontend: Handoff editor (agent list + handoff relationship matrix)
- [ ] Frontend: Handoff run monitor (chat-style view with transfer history)
- [ ] **Magentic compilation** via `GroupChatWorkflowBuilder` + `StandardMagenticManager`
- [ ] Frontend: Magentic editor (manager profile + specialist list + limits config)
- [ ] Frontend: Magentic run monitor (task ledger + progress log + dispatch history)
- [ ] Interactive mode for Handoff (user can send follow-up messages during run)

### Phase 3 — Graph Mode & Content Integration

**Goal:** Full visual canvas editor for custom topologies, plus deep Umbraco content integration.

- [ ] **Graph compilation** via manual `WorkflowBuilder` with user-defined edges
- [ ] `TransformStepExecutor` — deterministic data mapping between steps
- [ ] `ConditionStepExecutor` — evaluates predicates for conditional branching
- [ ] `ContentReadExecutor` — loads content node properties into workflow state
- [ ] `ContentWriteExecutor` — writes workflow state back to content properties
- [ ] `ApprovalStepExecutor` — pauses for human review via AG-UI interrupts
- [ ] Resume endpoint for approval responses
- [ ] Frontend: full canvas-based workflow editor (SVG edges, draggable nodes)
- [ ] Frontend: edge drawing via drag-from-port interaction
- [ ] Frontend: step configuration side panel with `{{variable}}` autocomplete
- [ ] Frontend: approval UI in run monitor (approve/reject/feedback)
- [ ] Content app showing available workflows and run history per content node
- [ ] Event-driven triggers via Umbraco notifications (publish, save, create)
- [ ] `WorkflowTrigger` configuration (content type + event → workflow)
- [ ] Workflow validation (cycle detection, unreachable steps, missing configs)

### Phase 4 — Composability & Production Features

**Goal:** Nested workflows, fault tolerance, and operational tooling.

- [ ] `SubWorkflowStepExecutor` — embed workflows within workflows (any mode as a step)
- [ ] Workflow version history and rollback (existing versioning pattern)
- [ ] Checkpointing via MAF `CheckpointManager` for fault tolerance
- [ ] Run retry/resume from last checkpoint
- [ ] Scheduled workflow triggers (cron-style)
- [ ] Workflow templates / presets (pre-built workflows users can import)
- [ ] Run analytics (duration, success rate, cost tracking per step/agent)
- [ ] Bulk operations (run workflow on multiple content nodes)
- [ ] Approval notification system (backoffice alerts, optional email)
- [ ] Declarative workflow import/export (MAF YAML/JSON format)

---

## Open Questions

1. **State size limits** — Workflow state accumulates output from every step. For content-heavy workflows (e.g., translating long articles), should we store large outputs in blob storage and keep only references in state?

2. **Concurrent run limits** — Should there be a configurable max concurrent runs per workflow to prevent resource exhaustion? Per-site? Per-user?

3. **MAF version pinning** — MAF is in preview targeting Q1 2026 GA. Should we wait for GA or build against preview packages with an abstraction layer for API changes?

4. **Declarative workflows** — MAF supports YAML/JSON declarative workflow definitions. Should we support importing/exporting workflows in this format for portability?

5. **Agent tool calls within workflows** — When an agent step calls tools (MCP, frontend), should those tool calls be visible in the workflow monitor? Or should each agent step be treated as a black box?

6. **Content locking** — When a workflow is writing to a content node, should we lock the node to prevent concurrent edits? Umbraco doesn't have native content locking, so this would need a custom implementation.

7. **Handoff interactive mode and AG-UI** — The Handoff pattern supports multi-turn conversations where the user sends follow-up messages. How should this integrate with the AG-UI run protocol? Should it be a long-lived SSE connection with bidirectional message flow, or separate run-per-turn?

8. **Magentic manager cost** — The Magentic pattern calls the manager LLM at every iteration for progress evaluation. For a 10-iteration workflow, that's 10+ manager calls plus specialist calls. Should we show estimated cost before starting? Allow budget limits?
