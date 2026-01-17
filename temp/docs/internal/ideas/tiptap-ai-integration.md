# TipTap AI Integration - Future Consideration

## Status: Under Consideration

This document explores integrating AI capabilities into Umbraco v17's TipTap-based Rich Text Editor through a standalone plugin package. This feature depends on and extends the [AI Prompts](./ai-prompts.md) system.

> **Prerequisites**: Requires AI Prompts feature to be implemented first.

---

## The Idea

A TipTap toolbar extension that provides AI-powered text operations directly within the Rich Text Editor. Editors can select text (or work with full content) and apply AI actions like improving writing, fixing grammar, translating, or running custom prompts.

**Key Concept**: One toolbar button opens a panel with all available AI prompts, filtered by applicability to the RTE context. Prompts execute with streaming responses, allowing editors to preview and apply results.

```
TipTap Editor Toolbar
┌──────────────────────────────────────────────────────────┐
│ [B] [I] [U] [Link] [List] ... [AI *]  ← New toolbar button│
└──────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                         ┌─────────────────────┐
                         │    AI Panel Modal   │
                         │  (prompt selection, │
                         │   streaming output, │
                         │   preview & apply)  │
                         └─────────────────────┘
```

**Example Use Cases**:
- Improve selected paragraph's clarity and readability
- Fix grammar and spelling in selected text
- Summarize a long section
- Translate content to another language
- Expand a brief outline into fuller content
- Apply custom AI prompts configured by administrators

---

## Key Design Decisions

### 1. Standalone Plugin Package

The TipTap integration lives in its own NuGet package for optional installation:

```
Umbraco.Ai.Tiptap (RCL with embedded client code)
    │
    ├── References: Umbraco.Ai.Core (for IAiChatService, AiPrompt)
    │
    └── Client calls: Umbraco.Ai.Web API endpoints
```

**Rationale**: Not all Umbraco.Ai users need RTE integration. Keeping it separate allows:
- Smaller core package size
- Optional installation
- Independent versioning
- Potential reuse in other TipTap-based editors

### 2. Reuses AI Prompts System

Rather than defining RTE-specific actions, this feature consumes the AI Prompts system:

```csharp
// Fetch prompts applicable to TipTap
var prompts = await _promptService.GetApplicablePromptsAsync(
    propertyEditorAlias: "Umbraco.RichText",
    contentTypeAlias: context.ContentType,
    propertyAlias: context.PropertyAlias);
```

The panel displays all prompts where `ApplicablePropertyEditors` includes `"Umbraco.RichText"` (or is null for "all editors").

**Benefits**:
- Single source of truth for prompt definitions
- Administrators configure prompts in one place
- RTE automatically gets new prompts when added
- Consistent behavior across property editors

### 3. Streaming Responses

AI responses stream in real-time via Server-Sent Events (SSE):

```
┌─────────────────────────────────────────────────────────┐
│  Improving Writing...                          [Cancel] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ The AI-generated text appears here character    │   │
│  │ by character as it streams from the service...  │   │
│  │ [|]                                             │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [=========>                 ] Generating...            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Rationale**:
- Better UX for longer operations (users see progress)
- Can cancel mid-generation
- Perceived performance improvement

### 4. Selection-Aware Context

The extension detects whether text is selected:

| Context | Behavior |
|---------|----------|
| Text selected | AI operates on selection only |
| No selection | AI operates on full editor content |

This is communicated clearly in the UI:
- "Working with: *Selected text...* (47 words)"
- "Working with: *Entire document* (523 words)"

### 5. Preview Before Apply

All prompt results show in a preview state before application:

```
┌─────────────────────────────────────────────────────────┐
│  Improve Writing - Complete                       [X]   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Result:                                                │
│  ┌─────────────────────────────────────────────────┐   │
│  │ The improved text that was generated by the     │   │
│  │ AI service. Ready to be applied to the editor.  │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│          [Regenerate]  [Cancel]  [Apply Changes]        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Actions**:
- **Apply Changes**: Replaces selection/content with AI result
- **Regenerate**: Runs the same prompt again for different output
- **Cancel**: Discards result and returns to editor

---

## UI Concepts

### Toolbar Button

A single button with the AI/magic wand icon:

```
[B] [I] [U] [Link] [List] [Image] [...] [AI ✨]
                                         ↑
                                    AI Assistant
```

### Action Selection Grid

Available prompts displayed as clickable cards:

```
┌─────────────────────────────────────────────────────────┐
│  AI Assistant                                     [X]   │
├─────────────────────────────────────────────────────────┤
│  Working with: "Selected text..." (47 words)            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │ Improve  │  │ Grammar  │  │ Summarize│              │
│  │   [✨]   │  │   [abc]  │  │   [≡]    │              │
│  └──────────┘  └──────────┘  └──────────┘              │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │  Expand  │  │ Simplify │  │Translate │              │
│  │   [+]    │  │   [-]    │  │   [🌐]   │              │
│  └──────────┘  └──────────┘  └──────────┘              │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Or describe what you want...            [Go →]   │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Configuration Form (for prompts with variables)

When a prompt has required variables (e.g., translate needs target language):

```
┌─────────────────────────────────────────────────────────┐
│  [←] Translate                                    [X]   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Target Language *                                      │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Spanish                                     [▼] │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│                              [Cancel]  [Translate →]    │
└─────────────────────────────────────────────────────────┘
```

### Custom Prompt Input

For ad-hoc requests not covered by defined prompts:

```
┌─────────────────────────────────────────────────────────┐
│  [←] Custom Prompt                                [X]   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  What would you like the AI to do?                      │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Make this text more engaging for a younger      │   │
│  │ audience while keeping the key points           │   │
│  │                                                 │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│                                 [Cancel]  [Generate →]  │
└─────────────────────────────────────────────────────────┘
```

---

## Execution Flow

```
User clicks AI toolbar button
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. Capture Editor Context                                        │
│    - Get selected text (or full content if no selection)         │
│    - Note selection range for later replacement                  │
│    - Detect property editor alias, content type, property alias  │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Fetch Applicable Prompts                                      │
│    - GET /umbraco/ai/management/api/v1/prompts/applicable        │
│    - Filter by propertyEditor="Umbraco.RichText"                 │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Display Action Selection                                      │
│    - Show prompt grid in modal                                   │
│    - User selects a prompt (or enters custom)                    │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Configure Variables (if needed)                               │
│    - Show configuration form for prompts with required variables │
│    - e.g., target language for translation                       │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Execute with Streaming                                        │
│    - POST /umbraco/ai/management/api/v1/chat/stream              │
│    - Display streaming response in real-time                     │
│    - Show progress indicator and cancel button                   │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Preview Result                                                │
│    - Show complete AI output                                     │
│    - Offer: Apply, Regenerate, Cancel                            │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Apply to Editor                                               │
│    - If selection: replace selected text                         │
│    - If no selection: replace full content                       │
│    - Close modal and return focus to editor                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Architecture

### Project Structure

```
src/Umbraco.Ai.Tiptap/
├── Umbraco.Ai.Tiptap.csproj          # RCL project
├── TiptapComposer.cs                  # Auto-registration
├── wwwroot/
│   └── App_Plugins/
│       └── UmbracoAiTiptap/
│           └── (compiled JS/CSS)
└── Client/                            # TypeScript source
    ├── src/
    │   ├── extension/
    │   │   ├── ai.tiptap-toolbar-api.ts
    │   │   └── ai.manifest.ts
    │   ├── ui/
    │   │   ├── elements/
    │   │   │   ├── ai-panel.element.ts
    │   │   │   ├── ai-action-list.element.ts
    │   │   │   ├── ai-streaming.element.ts
    │   │   │   └── ai-config-form.element.ts
    │   │   └── modals/
    │   │       ├── ai-panel-modal.element.ts
    │   │       └── ai-panel-modal.token.ts
    │   ├── services/
    │   │   ├── ai-api.service.ts
    │   │   └── ai-stream.service.ts
    │   └── bundle.manifests.ts
    ├── package.json
    ├── vite.config.ts
    └── tsconfig.json
```

### Extension Manifest

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'tiptapToolbarExtension',
    kind: 'button',
    alias: 'UmbracoAi.Tiptap.Toolbar.AiAssistant',
    name: 'AI Assistant TipTap Toolbar Extension',
    api: () => import('./ai.tiptap-toolbar-api.js'),
    meta: {
      alias: 'aiAssistant',
      icon: 'icon-wand',
      label: 'AI Assistant',
    },
  },
  {
    type: 'modal',
    alias: 'UmbracoAi.Modal.TiptapAiPanel',
    name: 'AI Panel Modal',
    element: () => import('../ui/modals/ai-panel-modal.element.js'),
  },
];
```

### Toolbar API

```typescript
export default class AiTiptapToolbarApi extends UmbTiptapToolbarElementApiBase {
  override async execute(editor?: Editor): Promise<void> {
    if (!editor) return;

    const { from, to, empty } = editor.state.selection;
    const selectedText = empty
      ? editor.getText()
      : editor.state.doc.textBetween(from, to, ' ');

    const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
    const modal = modalManager.open(this, AI_PANEL_MODAL, {
      data: {
        selectedText,
        hasSelection: !empty,
        propertyEditorAlias: 'Umbraco.RichText'
      }
    });

    const result = await modal.onSubmit().catch(() => undefined);
    if (!result?.text) return;

    // Apply based on output mode and selection state
    if (empty || result.outputMode === 'replace') {
      editor.chain().focus().setContent(result.text).run();
    } else {
      editor.chain().focus().deleteSelection().insertContent(result.text).run();
    }
  }
}
```

### Streaming Chat API

New endpoint in `Umbraco.Ai.Web`:

```
POST /umbraco/ai/management/api/v1/chat/stream
Content-Type: application/json

{
  "profileId": "guid-or-null-for-default",
  "promptAlias": "improve-writing",
  "text": "The selected text content",
  "variables": { "tone": "professional" }
}

Response: text/event-stream
data: {"text": "The ", "done": false}
data: {"text": "improved ", "done": false}
data: {"text": "text...", "done": false}
data: {"done": true}
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+A` | Open AI panel |
| `Escape` | Close panel / Cancel operation |
| `Enter` | Submit / Apply (when button focused) |
| `Arrow keys` | Navigate action grid |

---

## Relationship to Other Features

| Feature | Relationship |
|---------|-------------|
| **AI Prompts** | This feature *consumes* AI Prompts. Prompts applicable to RTE appear in the panel. |
| **AI Context** | When AI Context is built, it will be injected into prompt execution automatically. |
| **AI Agents** | Agents are for complex, multi-turn conversations. This is for quick, single-shot operations. |

**When to use which**:
- **TipTap AI**: "Improve this paragraph" (quick, in-context)
- **AI Prompts (on other editors)**: "Generate SEO meta description" (field-level)
- **AI Agents**: "Help me rewrite this entire article for a different audience" (conversation)

---

## Questions & Considerations

### 1. Should there be a bubble menu in addition to toolbar?

A floating bubble menu could appear when text is selected, offering quick AI actions.

**Recommendation**: Start with toolbar-only for V1. Bubble menu can be added later if there's demand, but adds complexity.

### 2. HTML vs Plain Text

TipTap content is HTML. Should AI responses include HTML formatting?

**Recommendation**:
- For "improve/grammar/simplify" type prompts: Return plain text, preserve original formatting
- For "expand/generate" prompts: Return HTML if the prompt specifies it
- Make this configurable per prompt via a new `OutputFormat` property

### 3. Undo Integration

Should AI changes integrate with TipTap's undo stack?

**Recommendation**: Yes, automatically handled by TipTap's transaction system. The `insertContent` and `setContent` commands create undo points.

### 4. Rate Limiting / Token Limits

Should there be safeguards for large selections?

**Recommendation**:
- Show word count in UI
- Warn if selection > 5000 words
- Backend should enforce profile token limits

---

## Recommendation

**Consider for Phase 2**, after AI Prompts is implemented.

### Prerequisites
1. AI Prompts feature complete and stable
2. Streaming chat endpoint available
3. Profile system supporting token limits

### Implementation Order
1. Create `Umbraco.Ai.Tiptap` RCL project
2. Implement toolbar extension and manifest
3. Build modal with action selection grid
4. Add streaming display component
5. Integrate with AI Prompts API
6. Add configuration forms for variable prompts
7. Polish UX (keyboard shortcuts, error handling)

---

## Related Documents

- [AI Prompts](./ai-prompts.md) - Configurable prompt system (dependency)
- [AI Context](./ai-context.md) - Brand voice and property hints
- [Umbraco.Ai.Agents](../umbraco-ai-agents-design.md) - Conversational AI assistants
