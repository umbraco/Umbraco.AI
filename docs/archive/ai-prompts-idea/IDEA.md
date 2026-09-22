# AI Prompts - Future Consideration

## Status: Shipped

Shipped as the `Umbraco.AI.Prompt` product. Kept here for historical reference only.

## Original status: Under Consideration

This document explores **AI Prompts**, a system for executing pre-defined, single-step AI operations directly from property editors. Inspired by [Perplex AI ContentBuddy](https://marketplace.umbraco.com/package/perplex.ai.contentbuddy), adapted to fit Umbraco.AI's design philosophy.

> **Note**: For automatic, event-driven automation, see [AI Workflows](./ai-workflows.md). AI Prompts are human-initiated via inline UI buttons.

---

## The Idea

AI Prompts are pre-defined, single-step operations that editors can execute with one click directly from property editors. They provide a simple way to leverage AI for common content tasks.

**Key Distinction**: Prompts are _human-initiated_ (clicked in the UI), while AI Workflows are _automatic_ (triggered by events like save/publish).

**Core Concept**: One prompt, one result, one click.

```
AI Prompt
├── Name, Alias, Icon
├── Prompt Template (with variables)
├── Profile Alias (AI configuration)
├── Applicability (which property editors/content types)
└── Output Mode (Replace, Append, Preview)
```

**Example Use Cases**:

- Generate SEO meta description from page content
- Write alt text for an image
- Summarize body content
- Improve text readability
- Translate to another language
- Check grammar and spelling

---

## Key Design Decisions

### 1. Single-Step Simplicity

Unlike AI Workflows (which chain multiple steps), AI Prompts execute a single operation:

```csharp
public class AIPrompt
{
    public Guid Id { get; set; }
    public string Alias { get; set; }           // "generate-meta-description"
    public string Name { get; set; }            // "Generate SEO Meta Description"
    public string? Description { get; set; }    // Shown in UI tooltip
    public string Icon { get; set; }            // UUI icon name

    // The prompt itself
    public string PromptTemplate { get; set; }  // "Generate a meta description for: {content}"
    public string ProfileAlias { get; set; }    // Which AI profile to use

    // Output behavior
    public PromptOutputMode OutputMode { get; set; }
    public bool RequiresConfirmation { get; set; }
}

public enum PromptOutputMode
{
    Replace,    // Replace field content entirely
    Append,     // Add to existing content
    Preview     // Show result, user clicks to apply
}
```

### 2. Property Editor Integration

AI Prompts appear as inline buttons on property editors:

```
┌────────────────────────────────────────────────────────────────┐
│ Meta Description                                        [🤖 ▼] │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Current page description here...                               │
│                                                                │
└────────────────────────────────────────────────────────────────┘
                                                          │
                                                          ▼
                                              ┌─────────────────────┐
                                              │ ✨ Generate SEO Meta │
                                              │ ✨ Improve Clarity   │
                                              │ ✨ Translate...      │
                                              │ ──────────────────── │
                                              │ ⚙️ Manage Prompts    │
                                              └─────────────────────┘
```

### 3. Applicability Scoping

Prompts can be scoped to appear only on relevant properties:

```csharp
public class AIPrompt
{
    // Applicability filters (null = all)
    public IReadOnlyList<string>? ApplicablePropertyEditors { get; set; }  // ["Umbraco.TextBox", "Umbraco.TextArea"]
    public IReadOnlyList<string>? ApplicableContentTypes { get; set; }     // ["article", "blogPost"]
    public IReadOnlyList<string>? ApplicablePropertyAliases { get; set; }  // ["metaDescription", "seoTitle"]

    // Site scoping (for multi-site)
    public Guid? ContextId { get; set; }  // null = global, else site-specific
}
```

**Resolution Example**:

- "Generate SEO Meta" → appears on `metaDescription` properties
- "Improve Text" → appears on all TextArea/TinyMCE editors
- "Translate to French" → appears on all text editors (for French site)

### 4. Prompt Templates with Variables

Templates support variable substitution for context-aware generation:

```
Template: "Write an SEO-optimized meta description (max {maxLength} characters) for a page about: {content}.
Target audience: {context.audience}. Tone: {context.tone}."
```

**Available Variables**:

| Variable             | Description                            |
| -------------------- | -------------------------------------- |
| `{content}`          | Current field value                    |
| `{documentContent}`  | All text content from the document     |
| `{propertyName}`     | Display name of the current property   |
| `{propertyAlias}`    | Alias of the current property          |
| `{contentType}`      | Current content type alias             |
| `{context.tone}`     | From AI Context (brand voice)          |
| `{context.audience}` | From AI Context (target audience)      |
| `{context.hint}`     | Property-specific hint from AI Context |
| `{maxLength}`        | Configured max length (if applicable)  |

### 5. Editor-Configurable

Non-developers can create and edit prompts through the backoffice:

```
┌─ AI Prompts Management ─────────────────────────────────────────┐
│                                                                 │
│  [+ New Prompt]                              [🔍 Filter...]     │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ✨ Generate SEO Meta Description              [Edit] [×]  │  │
│  │    Profile: seo-writer · Editors: TextBox, TextArea       │  │
│  │    Properties: metaDescription, seoDescription            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ✨ Improve Readability                        [Edit] [×]  │  │
│  │    Profile: content-editor · Editors: TinyMCE, TextArea   │  │
│  │    All content types                                      │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ✨ Generate Alt Text                          [Edit] [×]  │  │
│  │    Profile: accessibility · Editors: MediaPicker          │  │
│  │    All content types                                      │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Execution Flow

```
User clicks "Generate SEO Meta" on metaDescription field
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. Resolve AI Context                                            │
│    - Find site context (via root content node)                   │
│    - Load brand voice, audience, property hints                  │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Build Prompt                                                  │
│    - Start with prompt.PromptTemplate                            │
│    - Substitute variables ({content}, {context.tone}, etc.)      │
│    - Include AI Context (brand voice, property hint)             │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Execute via Profile                                           │
│    - Look up prompt.ProfileAlias                                 │
│    - Call IAIChatService.CompleteAsync(profileAlias, builtPrompt)│
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Apply Result                                                  │
│    - If OutputMode.Preview: show modal for approval              │
│    - If OutputMode.Replace: update field value                   │
│    - If OutputMode.Append: append to field value                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## UI Concepts

### Inline Property Button

The AI button appears on applicable property editors:

```
┌────────────────────────────────────────────────────────────────┐
│ Body Content                                            [🤖 ▼] │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Lorem ipsum dolor sit amet, consectetur adipiscing elit.       │
│ Sed do eiusmod tempor incididunt ut labore...                 │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Prompt Selection Popover

Clicking the button shows available prompts:

```
┌─────────────────────────────┐
│ ✨ Improve Readability      │
│ ✨ Summarize                │
│ ✨ Expand Content           │
│ ✨ Check Grammar            │
│ ✨ Translate...        →    │ ← Opens language submenu
│ ─────────────────────────── │
│ ⚙️ Manage Prompts           │ ← Opens admin panel
└─────────────────────────────┘
```

### Preview Modal (when RequiresConfirmation = true)

```
┌─────────────────────────────────────────────────────────────────┐
│ Generate SEO Meta Description                              [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Current Value:                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Welcome to our website about software development.        │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Generated Result:                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Discover expert software development insights, best       │ │
│  │ practices, and tutorials. Learn modern coding techniques  │ │
│  │ from industry professionals.                              │ │
│  └───────────────────────────────────────────────────────────┘ │
│  📊 155 / 160 characters                                       │
│                                                                 │
│                        [Regenerate]  [Apply]  [Cancel]          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Prompt Editor (Admin UI)

```
┌─────────────────────────────────────────────────────────────────┐
│ Edit AI Prompt                                             [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Generate SEO Meta Description                             │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Alias *                                                        │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ generate-seo-meta                                         │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Prompt Template *                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Write an SEO-optimized meta description (max 160 chars)   │ │
│  │ for content about: {documentContent}                      │ │
│  │                                                           │ │
│  │ Tone: {context.tone}                                      │ │
│  │ Target audience: {context.audience}                       │ │
│  │ {context.hint}                                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│  Available: {content} {documentContent} {propertyName}         │
│             {context.tone} {context.audience} {context.hint}   │
│                                                                 │
│  AI Profile *                                                   │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ seo-writer                                            [▼] │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ─────────────────── Applicability ───────────────────────────  │
│                                                                 │
│  Property Editors                                               │
│  [×] Umbraco.TextBox  [×] Umbraco.TextArea  [+ Add]            │
│                                                                 │
│  Property Aliases (optional)                                    │
│  [×] metaDescription  [×] seoDescription  [+ Add]              │
│                                                                 │
│  Content Types (optional)                                       │
│  All content types                                         [▼] │
│                                                                 │
│  ─────────────────── Behavior ────────────────────────────────  │
│                                                                 │
│  Output Mode                                                    │
│  (•) Replace content  ( ) Append  ( ) Preview first            │
│                                                                 │
│  [ ] Require confirmation before applying                       │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Built-in Prompts

Initial set of prompts to ship with Umbraco.AI:

| Prompt              | Description                    | Applicable Editors         |
| ------------------- | ------------------------------ | -------------------------- |
| Generate SEO Meta   | SEO-optimized meta description | TextBox, TextArea          |
| Generate Alt Text   | Accessibility-focused alt text | MediaPicker                |
| Summarize Content   | Create a brief summary         | TextArea, TinyMCE          |
| Improve Readability | Enhance clarity and flow       | TextArea, TinyMCE          |
| Expand Content      | Add more detail                | TextArea, TinyMCE          |
| Check Grammar       | Fix grammar and spelling       | TextBox, TextArea, TinyMCE |
| Translate           | Translate to target language   | TextBox, TextArea, TinyMCE |

---

## Integration with AI Context

AI Prompts automatically incorporate AI Context when executing. See [AI Context](./ai-context.md) for details.

```csharp
public class AIPromptExecutor
{
    public async Task<PromptResult> ExecuteAsync(
        AIPrompt prompt,
        PropertyExecutionContext propertyContext)
    {
        // 1. Resolve AI Context for this site/property
        var aiContext = await _contextService.ResolveContextAsync(
            propertyContext.RootContentId,
            propertyContext.ContentTypeAlias,
            propertyContext.PropertyAlias,
            propertyContext.PropertyEditorAlias);

        // 2. Build the prompt with variable substitution
        var builtPrompt = _templateEngine.Build(prompt.PromptTemplate, new
        {
            content = propertyContext.CurrentValue,
            documentContent = propertyContext.DocumentContent,
            propertyName = propertyContext.PropertyName,
            propertyAlias = propertyContext.PropertyAlias,
            contentType = propertyContext.ContentTypeAlias,
            context = new
            {
                tone = aiContext?.ToneDescription,
                audience = aiContext?.TargetAudience,
                hint = aiContext?.GetPropertyHint(propertyContext.PropertyAlias)
            }
        });

        // 3. Execute via profile
        var response = await _chatService.CompleteAsync(prompt.ProfileAlias, builtPrompt);

        return new PromptResult(response.Text, prompt.OutputMode);
    }
}
```

---

## Relationship to Other Features

| Aspect            | AI Prompts                 | AI Workflows                 | Agents                          |
| ----------------- | -------------------------- | ---------------------------- | ------------------------------- |
| **Initiation**    | Human-initiated (UI click) | Automatic (event-driven)     | Human-initiated (conversation)  |
| **Steps**         | Single step only           | One or more (chainable)      | Dynamic (tool calls)            |
| **Trigger**       | Inline button click        | OnSave, OnPublish, Scheduled | User conversation               |
| **Output**        | Single property value      | Property values              | Chat responses + tool calls     |
| **Use case**      | Quick content assistance   | Automation pipelines         | Complex reasoning & exploration |
| **Configuration** | Per editor/property        | Per document type            | Per agent definition            |

**When to use which**:

- **AI Prompts**: "I want to generate a meta description for this field" (human clicks button)
- **AI Workflows**: "When I publish, auto-generate summary + tags + translation" (automatic)
- **Agents**: "Help me rewrite this entire page for a different audience" (conversation)

---

## API Design

### Backend Service

```csharp
public interface IAIPromptService
{
    // CRUD
    Task<AIPrompt> CreateAsync(AIPrompt prompt, CancellationToken ct = default);
    Task<AIPrompt?> GetByAliasAsync(string alias, CancellationToken ct = default);
    Task<IEnumerable<AIPrompt>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(AIPrompt prompt, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Resolution
    Task<IEnumerable<AIPrompt>> GetApplicablePromptsAsync(
        string propertyEditorAlias,
        string? contentTypeAlias = null,
        string? propertyAlias = null,
        Guid? contextId = null,
        CancellationToken ct = default);
}

public interface IAIPromptExecutor
{
    Task<PromptResult> ExecuteAsync(
        string promptAlias,
        PropertyExecutionContext context,
        CancellationToken ct = default);
}
```

### API Endpoints

```
GET    /umbraco/ai/management/api/v1/prompts                    # List all prompts
GET    /umbraco/ai/management/api/v1/prompts/{alias}            # Get prompt by alias
POST   /umbraco/ai/management/api/v1/prompts                    # Create prompt
PUT    /umbraco/ai/management/api/v1/prompts/{id}               # Update prompt
DELETE /umbraco/ai/management/api/v1/prompts/{id}               # Delete prompt

GET    /umbraco/ai/management/api/v1/prompts/applicable         # Get prompts for a property
       ?propertyEditor=Umbraco.TextBox
       &contentType=article
       &propertyAlias=metaDescription

POST   /umbraco/ai/api/v1/prompts/{alias}/execute               # Execute a prompt
       Body: { contentId, propertyAlias, currentValue }
```

---

## Questions & Considerations

### 1. Naming

"AI Prompts" is a working name. Alternatives considered:

- AI Quick Actions
- AI Assists
- AI Operations
- Smart Actions

**Decision**: Keep "AI Prompts" for now - it's descriptive and distinguishes from Workflows.

### 2. Streaming Support

Should prompt execution support streaming for longer outputs?

```
POST /umbraco/ai/api/v1/prompts/{alias}/execute/stream
```

**Recommendation**: Yes, especially for content generation on TinyMCE fields.

### 3. History/Undo

Should we track prompt execution history for undo capability?

**Recommendation**: V2 consideration. For V1, rely on content versioning.

### 4. Custom Prompts per Content Item

Should editors be able to create one-off prompts for specific content items?

**Recommendation**: No for V1. Keep prompts as reusable definitions. Use Agents for ad-hoc requests.

---

## Recommendation

**Consider for Phase 2**, alongside AI Context.

### Prerequisites

1. Stable Profile and Connection management
2. Chat capability working end-to-end
3. AI Context system (for brand voice injection)
4. Property editor extension points in Umbraco backoffice

### Implementation Order

1. AIPrompt model and repository
2. Prompt execution service
3. API endpoints
4. Property editor button integration (frontend)
5. Prompt selection popover
6. Preview modal
7. Prompt management UI
8. Built-in prompt library

---

## Related Documents

- [AI Workflows](./ai-workflows.md) - Multi-step automated pipelines
- [AI Context](./ai-context.md) - Brand voice and property hints
- [Umbraco.AI.Agents](../umbraco-ai-agents-design.md) - Conversational AI assistants

---

## Related Decisions

| Decision      | Current Choice                                          |
| ------------- | ------------------------------------------------------- |
| Naming        | "AI Prompts" (working name, may change)                 |
| Scope         | Single-step operations only                             |
| Configuration | Editor-configurable via backoffice UI                   |
| Applicability | Filter by property editor, content type, property alias |
| Output modes  | Replace, Append, Preview                                |
| Integration   | Inline property editor buttons                          |
