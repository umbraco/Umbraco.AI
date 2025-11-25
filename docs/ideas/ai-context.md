# AI Context - Future Consideration

## Status: Under Consideration

This document explores **AI Context**, a system for defining brand voice, tone, audience, and property-specific hints that automatically enrich all AI operations. Inspired by [Perplex AI ContentBuddy](https://marketplace.umbraco.com/package/perplex.ai.contentbuddy)'s centralized tone of voice feature.

---

## The Idea

AI Context provides a centralized place to define *how* AI should behave when generating content. Instead of repeating brand guidelines in every prompt, you define them once and they're automatically injected into all AI operations.

**Core Concept**: Define once, apply everywhere.

```
AI Context (scoped to site via root content node)
├── Name
├── Root Content Node (for multi-site support)
├── Brand Voice
│   ├── Tone Description ("Professional but approachable")
│   ├── Target Audience ("B2B decision makers")
│   ├── Style Guidelines ("Avoid jargon, use active voice")
│   └── Avoid Patterns ("Exclamation marks, superlatives")
└── Property Hints[]
    ├── Property/Editor Scope
    └── Hint Text ("Max 160 chars, include primary keyword")
```

---

## Why AI Context?

Without AI Context, every AI operation needs to include brand guidelines:

```
❌ Without Context:
"Write a meta description. Be professional but approachable.
Target B2B decision makers. Avoid jargon. Max 160 characters.
Include the primary keyword naturally..."

Every. Single. Time.
```

With AI Context, the guidelines are injected automatically:

```
✅ With Context:
"Write a meta description for: {content}"

→ System automatically adds brand voice, audience, and property hints
```

---

## Key Design Decisions

### 1. Site-Scoped via Root Content Node

Umbraco supports multi-site setups where different sites may have different brand guidelines. AI Context is scoped to root content nodes:

```
Content Tree:
├── Corporate Site (root)          → Corporate AI Context
│   ├── About
│   ├── Products
│   └── Blog
├── Consumer Brand (root)          → Consumer AI Context
│   ├── Home
│   ├── Shop
│   └── Support
└── Partner Portal (root)          → (inherits Global Context)
    └── Resources
```

**Resolution**: When executing an AI operation on content, the system finds the root content node and loads its associated AI Context. If none exists, falls back to the global default.

### 2. Hierarchical Property Hints

Property hints can be defined at multiple levels of specificity:

```
Hint Resolution (most specific wins):
1. Specific property on specific content type: article.metaDescription
2. Specific property (any content type): *.metaDescription
3. Property editor type: Umbraco.TextBox
4. Global default: (no hint)
```

### 3. Automatic Injection

AI Context is automatically injected into all AI operations - Prompts, Workflows, and Agents - without developers needing to handle it explicitly.

---

## Data Model

```csharp
public class AiContext
{
    public Guid Id { get; set; }
    public string Name { get; set; }                    // "Corporate Site Context"

    // Scope
    public Guid? RootContentId { get; set; }            // null = global default

    // Brand Voice
    public string? ToneDescription { get; set; }        // "Professional but approachable"
    public string? TargetAudience { get; set; }         // "B2B tech decision makers"
    public string? StyleGuidelines { get; set; }        // "Use active voice, be concise"
    public string? AvoidPatterns { get; set; }          // "Jargon, exclamation marks"

    // Property Hints
    public IList<AiPropertyHint> PropertyHints { get; set; } = [];
}

public class AiPropertyHint
{
    public Guid Id { get; set; }

    // Scope (all optional - more specific = higher priority)
    public string? ContentTypeAlias { get; set; }       // "article"
    public string? PropertyAlias { get; set; }          // "metaDescription"
    public string? PropertyEditorAlias { get; set; }    // "Umbraco.TextBox"

    // The hint itself
    public string Hint { get; set; }                    // "Max 160 chars, include keyword"
    public string? ExampleOutput { get; set; }          // "Discover how to..." (few-shot)
}
```

---

## Context Resolution

When an AI operation executes, the system resolves the applicable context:

```csharp
public class AiContextResolver
{
    public async Task<ResolvedAiContext> ResolveAsync(
        Guid contentId,
        string contentTypeAlias,
        string propertyAlias,
        string propertyEditorAlias)
    {
        // 1. Find root content node
        var rootId = await _contentService.GetRootIdAsync(contentId);

        // 2. Get site-specific context (or global fallback)
        var context = await _contextRepository.GetByRootIdAsync(rootId)
                   ?? await _contextRepository.GetGlobalAsync();

        if (context == null)
            return ResolvedAiContext.Empty;

        // 3. Find most specific property hint
        var hint = ResolvePropertyHint(
            context.PropertyHints,
            contentTypeAlias,
            propertyAlias,
            propertyEditorAlias);

        return new ResolvedAiContext
        {
            ToneDescription = context.ToneDescription,
            TargetAudience = context.TargetAudience,
            StyleGuidelines = context.StyleGuidelines,
            AvoidPatterns = context.AvoidPatterns,
            PropertyHint = hint?.Hint,
            ExampleOutput = hint?.ExampleOutput
        };
    }

    private AiPropertyHint? ResolvePropertyHint(
        IList<AiPropertyHint> hints,
        string contentTypeAlias,
        string propertyAlias,
        string propertyEditorAlias)
    {
        // Priority: most specific first
        return hints
            // 1. Exact match: content type + property
            .FirstOrDefault(h =>
                h.ContentTypeAlias == contentTypeAlias &&
                h.PropertyAlias == propertyAlias)
            // 2. Property alias only (any content type)
            ?? hints.FirstOrDefault(h =>
                h.ContentTypeAlias == null &&
                h.PropertyAlias == propertyAlias)
            // 3. Property editor type
            ?? hints.FirstOrDefault(h =>
                h.PropertyAlias == null &&
                h.PropertyEditorAlias == propertyEditorAlias)
            // 4. No hint
            ?? null;
    }
}
```

---

## How Context is Injected

### Into AI Prompts

```csharp
public class AiPromptExecutor
{
    public async Task<PromptResult> ExecuteAsync(AiPrompt prompt, PropertyContext propertyCtx)
    {
        // Resolve AI Context
        var aiContext = await _contextResolver.ResolveAsync(
            propertyCtx.ContentId,
            propertyCtx.ContentTypeAlias,
            propertyCtx.PropertyAlias,
            propertyCtx.PropertyEditorAlias);

        // Build prompt with context variables
        var builtPrompt = _templateEngine.Build(prompt.PromptTemplate, new
        {
            content = propertyCtx.CurrentValue,
            context = new
            {
                tone = aiContext.ToneDescription,
                audience = aiContext.TargetAudience,
                style = aiContext.StyleGuidelines,
                avoid = aiContext.AvoidPatterns,
                hint = aiContext.PropertyHint
            }
        });

        // Execute
        return await _chatService.CompleteAsync(prompt.ProfileAlias, builtPrompt);
    }
}
```

### Into AI Workflows

```csharp
public class AiWorkflowExecutor
{
    public async Task<WorkflowResult> ExecuteAsync(AiWorkflow workflow, IContent content)
    {
        // Resolve context once for the workflow
        var aiContext = await _contextResolver.ResolveAsync(content.Id, ...);

        foreach (var step in workflow.Steps)
        {
            // Context available to each step
            var stepContext = new WorkflowStepContext(content, aiContext, ...);
            await step.ExecuteAsync(stepContext);
        }
    }
}
```

### Into Agents

```csharp
public class AgentChatService
{
    public async Task<ChatResponse> ChatAsync(AgentChatRequest request)
    {
        // Resolve context based on current workspace
        var aiContext = await _contextResolver.ResolveAsync(
            request.CurrentContentId, ...);

        // Enrich agent system prompt with context
        var enrichedSystemPrompt = $"""
            {agent.SystemPrompt}

            --- Brand Context ---
            Tone: {aiContext.ToneDescription}
            Audience: {aiContext.TargetAudience}
            Style: {aiContext.StyleGuidelines}
            Avoid: {aiContext.AvoidPatterns}
            """;

        return await _chatService.ChatAsync(enrichedSystemPrompt, request.Messages);
    }
}
```

---

## UI Concepts

### Context Management (Settings Section)

```
┌─ AI Context Management ─────────────────────────────────────────┐
│                                                                 │
│  [+ New Context]                                                │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🌐 Global Default                              [Edit]     │ │
│  │    Applies to all sites without specific context          │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🏢 Corporate Site                              [Edit]     │ │
│  │    Root: /corporate-site                                  │ │
│  │    Tone: Professional, authoritative                      │ │
│  │    5 property hints configured                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🛍️ Consumer Brand                              [Edit]     │ │
│  │    Root: /consumer-brand                                  │ │
│  │    Tone: Friendly, conversational                         │ │
│  │    3 property hints configured                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Context Editor

```
┌─ Edit AI Context: Corporate Site ──────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Corporate Site                                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Root Content Node                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 📁 Corporate Site                               [Change]  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ═══════════════════ Brand Voice ═══════════════════════════   │
│                                                                 │
│  Tone Description                                               │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Professional and authoritative, but approachable. We      │ │
│  │ speak as trusted advisors to our clients.                 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Target Audience                                                │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ B2B technology decision makers: CTOs, IT Directors,       │ │
│  │ and senior developers at enterprise companies.            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Style Guidelines                                               │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ - Use active voice                                        │ │
│  │ - Be concise and direct                                   │ │
│  │ - Support claims with data where possible                 │ │
│  │ - Use industry terminology appropriately                  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Patterns to Avoid                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ - Exclamation marks                                       │ │
│  │ - Superlatives (best, greatest, revolutionary)            │ │
│  │ - Buzzwords without substance                             │ │
│  │ - Overly casual language                                  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ═══════════════════ Property Hints ════════════════════════   │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ metaDescription (any content type)            [Edit] [×]  │ │
│  │ "SEO-optimized, max 160 chars, include primary keyword"   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ article.summary                               [Edit] [×]  │ │
│  │ "2-3 sentences, highlight key takeaways for busy readers" │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Umbraco.MediaPicker (alt text)                [Edit] [×]  │ │
│  │ "Descriptive, accessibility-focused, no 'image of'"       │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  [+ Add Property Hint]                                          │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Property Hint Editor

```
┌─ Add Property Hint ────────────────────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Scope                                                          │
│  ( ) Specific property on specific content type                 │
│      Content Type: [article         ▼]                          │
│      Property:     [metaDescription ▼]                          │
│                                                                 │
│  ( ) Specific property (any content type)                       │
│      Property:     [metaDescription ▼]                          │
│                                                                 │
│  (•) Property editor type                                       │
│      Editor:       [Umbraco.TextBox ▼]                          │
│                                                                 │
│  Hint *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Keep under 60 characters for optimal display in search    │ │
│  │ results. Include the primary keyword naturally.           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Example Output (optional, for few-shot learning)               │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ "Discover enterprise AI solutions that scale with your    │ │
│  │ business. Learn how leading companies leverage AI."       │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## API Design

### Service Interface

```csharp
public interface IAiContextService
{
    // CRUD
    Task<AiContext> CreateAsync(AiContext context, CancellationToken ct = default);
    Task<AiContext?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AiContext?> GetByRootIdAsync(Guid rootContentId, CancellationToken ct = default);
    Task<AiContext?> GetGlobalAsync(CancellationToken ct = default);
    Task<IEnumerable<AiContext>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(AiContext context, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Resolution
    Task<ResolvedAiContext> ResolveAsync(
        Guid contentId,
        string contentTypeAlias,
        string propertyAlias,
        string propertyEditorAlias,
        CancellationToken ct = default);
}
```

### API Endpoints

```
GET    /umbraco/ai/management/api/v1/contexts                     # List all contexts
GET    /umbraco/ai/management/api/v1/contexts/{id}                # Get by ID
GET    /umbraco/ai/management/api/v1/contexts/global              # Get global default
GET    /umbraco/ai/management/api/v1/contexts/by-root/{rootId}    # Get by root content
POST   /umbraco/ai/management/api/v1/contexts                     # Create
PUT    /umbraco/ai/management/api/v1/contexts/{id}                # Update
DELETE /umbraco/ai/management/api/v1/contexts/{id}                # Delete

# Resolution endpoint (for testing/debugging)
GET    /umbraco/ai/management/api/v1/contexts/resolve
       ?contentId={id}&contentType={alias}&property={alias}&editor={alias}
```

---

## Integration Points

### With AI Prompts

Prompts can reference context variables in their templates:

```
Template: "Generate a meta description for: {content}

Tone: {context.tone}
Audience: {context.audience}
Guidelines: {context.style}
Avoid: {context.avoid}
Property hint: {context.hint}"
```

### With AI Workflows

Workflow steps automatically receive resolved context:

```csharp
public class LlmSummarizerStep : AiWorkflowStepTypeBase
{
    public override async Task<StepResult> ExecuteAsync(WorkflowStepContext ctx)
    {
        var prompt = $"""
            Summarize this content:
            {ctx.GetInput<string>("text")}

            Context:
            - Tone: {ctx.AiContext.ToneDescription}
            - Audience: {ctx.AiContext.TargetAudience}
            - Hint: {ctx.AiContext.PropertyHint}
            """;

        return await ExecutePromptAsync(prompt);
    }
}
```

### With Agents

Agent system prompts are enriched with context:

```csharp
// Agent sees this automatically in their system prompt:
"You are a content assistant for the Corporate Site.

Brand Voice:
- Tone: Professional and authoritative, but approachable
- Audience: B2B technology decision makers
- Style: Use active voice, be concise
- Avoid: Exclamation marks, superlatives"
```

---

## Questions & Considerations

### 1. Context Inheritance

Should child sites inherit from parent context with overrides?

```
Global Context
└── Corporate Site Context (inherits + overrides tone)
    └── Corporate Blog Context (inherits + overrides audience)
```

**Recommendation**: Start simple with flat contexts. Add inheritance if needed.

### 2. Context Versioning

Should we track context changes over time?

**Recommendation**: V2 consideration. For V1, rely on audit logs.

### 3. Context per Language/Culture

Should context vary by language variant?

**Recommendation**: Consider for V2. Multi-site covers most cases initially.

### 4. Context Import/Export

Should contexts be exportable for sharing across environments?

**Recommendation**: Yes, as JSON. Useful for dev → staging → prod workflows.

---

## Recommendation

**Implement as foundation for AI Prompts and AI Workflows**.

AI Context should be built first or alongside AI Prompts, as it provides the brand consistency that makes AI-generated content actually useful.

### Implementation Order
1. AiContext and AiPropertyHint models
2. Context repository and service
3. Context resolution logic
4. API endpoints
5. Management UI
6. Integration with AI Prompts
7. Integration with AI Workflows
8. Integration with Agents

---

## Related Documents

- [AI Prompts](./ai-prompts.md) - Human-initiated single-step operations
- [AI Workflows](./ai-workflows.md) - Automatic multi-step automation
- [Umbraco.Ai.Agents](../umbraco-ai-agents-design.md) - Conversational AI assistants

---

## Related Decisions

| Decision | Current Choice |
|----------|----------------|
| Naming | "AI Context" |
| Scope | Per-site via root content node |
| Hint resolution | Most specific wins (content type + property > property > editor) |
| Multi-site | Supported via root content node association |
| Inheritance | Flat (no inheritance) for V1 |
