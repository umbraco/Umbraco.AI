# AI Workflows - Future Consideration

## Status: Under Consideration

This document explores **AI Workflows**, a system for automating field population and content transformation using AI. Inspired by [Drupal AI Automators](https://www.drupal.org/project/ai), adapted to fit Umbraco's architecture and Umbraco.Ai's design philosophy.

> **Note**: For human-initiated, inline property operations, see [AI Prompts](./ai-prompts.md). Workflows are automatic (event-driven) and can orchestrate multiple steps.

---

## The Idea

AI Workflows are document-scoped automations that populate fields using AI. They run automatically in response to content events (save, publish) or on a schedule, and can orchestrate one or more steps.

**Key Distinction**: Workflows are *automatic* (triggered by events), while AI Prompts are *human-initiated* (clicked in the UI).

**Core Concept**: Workflows can chain multiple steps, providing a unified execution model for both simple and complex automation.

```
AI Workflow (configured per document type)
├── Name, Alias
├── Trigger: Manual | OnSave | OnPublish | Scheduled
├── Steps[]
│   ├── Step Type (code-defined)
│   ├── Input Mappings (source properties + previous step outputs)
│   ├── Output Mapping (target property)
│   └── Profile Alias (AI configuration)
└── Document Type Alias
```

---

## Key Design Decisions

### 1. Chain-First Model

All workflows are chains of steps. This provides:
- **Unified execution model** - no special cases
- **Easy extension** - add steps to existing workflows
- **Consistent UI** - same interface regardless of complexity
- **Single execution engine** - one set of hooks, logging, monitoring

```csharp
// A workflow with multiple steps
var processArticle = new AiWorkflow
{
    Alias = "process-article",
    DocumentTypeAlias = "article",
    Trigger = WorkflowTrigger.OnPublish,
    Steps =
    [
        new AiWorkflowStep
        {
            Order = 1,
            StepTypeAlias = "llm-summarizer",
            InputMappings = new() { ["text"] = "bodyText" },
            OutputTarget = "summary",
            ProfileAlias = "content-summarizer"
        },
        new AiWorkflowStep
        {
            Order = 2,
            StepTypeAlias = "llm-tagger",
            InputMappings = new() { ["text"] = "bodyText", ["summary"] = "$previous" },
            OutputTarget = "tags",
            ProfileAlias = "content-tagger"
        },
        new AiWorkflowStep
        {
            Order = 3,
            StepTypeAlias = "llm-translator",
            InputMappings = new() { ["text"] = "summary", ["targetLanguage"] = "de" },
            OutputTarget = "summaryDe",
            ProfileAlias = "translator"
        }
    ]
};
```

### 2. Document Type Scoped

AI Workflows are configured per document type, ensuring:
- **Validation at configuration time** - UI shows only properties that exist
- **Type safety** - validate compatible inputs/outputs
- **No runtime surprises** - workflows won't fail due to missing properties

```
Document Type: Article
├── Properties: [title, bodyText, summary, tags, heroImage]
└── AI Workflows:
    └── "Process Article"
        ├── Step 1: Summarize (bodyText → summary)
        ├── Step 2: Tag (bodyText + summary → tags)
        └── Step 3: Translate (summary → summaryDe)
```

### 3. Standalone Workflows with Property Indicators

Workflows are configured separately (not attached to properties), but the UI displays indicators on properties that are workflow targets:

```
Content Editor View:

  title
  ┌──────────────────────────────────────────────┐
  │ How to Build Great Software                  │
  └──────────────────────────────────────────────┘

  summary                                      🤖 [Workflow]
  ┌──────────────────────────────────────────────┐
  │ (empty)                                      │
  └──────────────────────────────────────────────┘
  ℹ️ Populated by "Process Article" workflow (step 1 of 3)
```

### 4. Hybrid Configuration Model

Following Umbraco patterns (Property Editors + Data Types):

| Layer | Defined By | Purpose |
|-------|------------|---------|
| **Step Types** | Developers (code) | What a step *can* do |
| **Workflows** | Editors/Admins (UI) | How steps are configured and connected |

```csharp
// Developer creates Step Type
[AiWorkflowStepType("llm-summarizer", "Summarize Text")]
public class LlmSummarizerStep : AiWorkflowStepTypeBase
{
    public override string[] RequiredInputs => ["text"];
    public override string OutputType => "string";

    public override async Task<StepResult> ExecuteAsync(WorkflowStepContext context)
    {
        var text = context.GetInput<string>("text");
        var response = await context.ChatService.CompleteAsync(
            context.ProfileAlias,
            $"Summarize this text concisely:\n\n{text}");
        return StepResult.Success(response.Text);
    }
}
```

---

## Execution Model

### Step Context & Variable Passing

Steps can reference:
- Property values from the document
- Outputs from previous steps
- Workflow-level context

| Reference | Meaning |
|-----------|---------|
| `bodyText` | Property alias on the document |
| `$previous` | Output from immediately preceding step |
| `$step1` or `$stepAlias` | Named output from specific step |
| `$workflow.trigger` | How the workflow was invoked |

```csharp
public class WorkflowStepContext
{
    public IContent Content { get; }
    public IReadOnlyList<StepResult> PreviousResults { get; }

    public T GetInput<T>(string key)
    {
        // Resolve from property, previous step, or workflow context
    }
}
```

### Execution Flow

```csharp
public class AiWorkflowExecutor
{
    public async Task<WorkflowResult> ExecuteAsync(AiWorkflow workflow, IContent content)
    {
        var results = new List<StepResult>();

        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            var stepType = _stepTypeRegistry.Get(step.StepTypeAlias);
            var context = new WorkflowStepContext(content, results, step);

            var result = await stepType.ExecuteAsync(context);
            results.Add(result);

            // Optionally write to target property
            if (step.OutputTarget != null && result.Success)
            {
                content.SetValue(step.OutputTarget, result.Value);
            }
        }

        return new WorkflowResult(results);
    }
}
```

### Trigger Options

| Trigger | Behavior |
|---------|----------|
| **Manual** | Editor clicks "Run" in workspace view |
| **OnSave** | Executes when content is saved |
| **OnPublish** | Executes when content is published |
| **Scheduled** | Runs on a schedule (e.g., re-translate nightly) |

---

## UI Concepts

### Configuration UI (Document Type Settings)

```
┌─ Document Type: Article ─────────────────────────────────┐
│                                                          │
│  Properties    Structure    AI Workflows    Permissions  │
│                             ^^^^^^^^^^^^                 │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Configured AI Workflows:                                │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ ▶ Process Article            Trigger: On Publish   │  │
│  │   3 steps · Outputs: summary, tags, summaryDe      │  │
│  │   [Edit] [Delete]                                  │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ ▶ Daily Translation Sync     Trigger: Scheduled    │  │
│  │   2 steps · Outputs: titleDe, bodyDe               │  │
│  │   [Edit] [Delete]                                  │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  [+ Add AI Workflow]                                     │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

### Workspace View (Content Node)

A workspace view shows available workflows and execution history:

```
┌─ Content: "My Article" ──────────────────────────────────┐
│                                                          │
│  Content    Info    AI Workflows                         │
│                     ^^^^^^^^^^^^                         │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Available Workflows                                     │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Process Article                        [▶ Run]     │  │
│  │ 3 steps · Last run: 2 hours ago                    │  │
│  │ Status: Source changed since last run  ⚠️          │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  ─────────────────────────────────────────────────────   │
│  Execution History                                       │
│  ┌──────────────────────────────────────────────────┐   │
│  │ 10:32  Process Article   ✓ Completed  [View]     │   │
│  │ 10:30  Process Article   ✗ Step 2 failed         │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

### Property Indicators

Properties targeted by AI Workflows show visual indicators:

| State | Indicator | Meaning |
|-------|-----------|---------|
| Target | 🤖 | Property is populated by a workflow |
| Ready | 🤖 | Workflow can run, inputs available |
| Stale | 🤖⚠️ | Source fields changed since last run |
| Running | 🤖⏳ | Workflow currently executing |

---

## Built-in Step Types

Initial set of step types to ship with Umbraco.Ai:

| Step Type | Description | Capability |
|-----------|-------------|------------|
| `llm-text-generator` | Generate text from prompt + context | Chat |
| `llm-summarizer` | Summarize source field content | Chat |
| `llm-translator` | Translate to target language | Chat |
| `llm-tagger` | Suggest taxonomy terms | Chat |
| `llm-alt-text` | Generate image alt text | Chat + Media |
| `llm-seo-description` | Generate SEO meta descriptions | Chat |

Developers can create custom step types for specialized needs.

---

## Integration with Existing Architecture

### Profiles

AI Workflows reference existing `AiProfile` for model/prompt configuration:

```csharp
public class AiWorkflowStep
{
    public string ProfileAlias { get; set; }  // References existing profile
    // ...
}
```

### AI Context

Workflows automatically incorporate AI Context (brand voice, property hints) when executing steps. See [AI Context](./ai-context.md) for details.

### Capabilities

Step types declare which capability they require:

```csharp
[AiWorkflowStepType("llm-summarizer")]
public class LlmSummarizerStep : AiWorkflowStepTypeBase
{
    public override AiCapabilityType RequiredCapability => AiCapabilityType.Chat;
}
```

### Middleware

AI Workflow executions flow through the existing middleware pipeline, getting logging, telemetry, etc.

---

## Questions & Considerations

### 1. Reusability Across Document Types

What if the same workflow is needed on multiple document types?

**Options**:
- **A) Duplicate** - Configure separately on each (simple, v1 approach)
- **B) Templates** - Create reusable workflow templates
- **C) Composition** - Attach to compositions that doc types share

**Recommendation**: Start with duplication, add templates if pain emerges.

### 2. Preview Mode

Should workflows support "dry run" that shows proposed output without saving?

```
[▶ Run]  [👁️ Preview]

Preview Result:
┌────────────────────────────────────────────────────┐
│ Step 1 (Summarize): ✓                              │
│   "This article explores building great software   │
│    through iterative development and testing..."   │
│                                                    │
│ Step 2 (Tag): ✓                                    │
│   ["development", "testing", "software"]           │
│                                                    │
│ Step 3 (Translate): ✓                              │
│   "Dieser Artikel untersucht..."                   │
│                                                    │
│                    [Apply All]  [Discard]          │
└────────────────────────────────────────────────────┘
```

### 3. Error Handling

What happens when step 3 of 5 fails?
- Stop and rollback all changes?
- Keep completed steps, skip failed?
- Configurable per-workflow?

### 4. Cost Control

How to prevent runaway API costs from bulk operations?
- Rate limiting?
- Confirmation for batch operations?
- Usage tracking/quotas?

### 5. Approval Workflow Integration

Should AI Workflows integrate with the Agents approval workflow?

```csharp
public class AiWorkflowStep
{
    public bool RequiresApproval { get; set; }  // Pause for human review
}
```

This could align with the agents design doc's "human-in-the-loop" concept.

---

## Relationship to Other Features

AI Workflows, AI Prompts, and Agents serve different use cases:

| Aspect | AI Workflows | AI Prompts | Agents |
|--------|--------------|------------|--------|
| **Initiation** | Automatic (event-driven) | Human-initiated (UI click) | Human-initiated (conversation) |
| **Steps** | One or more (chainable) | Single step only | Dynamic (tool calls) |
| **Trigger** | OnSave, OnPublish, Scheduled | Inline button click | User conversation |
| **Output** | Property values | Single property value | Chat responses + tool calls |
| **Use case** | Automation pipelines | Quick content assistance | Complex reasoning & exploration |
| **Configuration** | Per document type | Per property editor/type | Per agent definition |

They share infrastructure (Profiles, Capabilities, Middleware, AI Context) but serve distinct purposes.

---

## Recommendation

**Consider for Phase 2**, after core chat/embedding functionality is stable.

### Prerequisites
1. Stable Profile and Connection management
2. Chat capability working end-to-end
3. Basic backoffice UI for AI configuration
4. AI Context system (for brand voice injection)

### Implementation Order
1. Step Type plugin system and registry
2. Workflow model and execution engine
3. Document type configuration UI
4. Workspace view for manual execution
5. Property indicators
6. Automatic triggers (OnSave, OnPublish)
7. Staleness detection
8. Execution history

---

## Related Links

- [Drupal AI Automators](https://www.drupal.org/project/ai)
- [Drupal AI Automators Plugin Development](https://project.pages.drupalcode.org/ai/1.1.x/developers/writing_an_ai_automators_plugin/)

---

## Related Documents

- [AI Prompts](./ai-prompts.md) - Single-step, inline property operations
- [AI Context](./ai-context.md) - Brand voice and property hints
- [Umbraco.Ai.Agents](../umbraco-ai-agents-design.md) - Conversational AI assistants

---

## Related Decisions

| Decision | Current Choice |
|----------|----------------|
| Naming | "AI Workflows" (multi-step automation) |
| Configuration model | Hybrid: code-defined step types, UI-configured workflows |
| Scope | Per document type |
| Execution model | Chain-first (all workflows are step chains) |
| UI integration | Workspace view + property indicators |
| Chaining support | First-class, via steps with variable passing |
