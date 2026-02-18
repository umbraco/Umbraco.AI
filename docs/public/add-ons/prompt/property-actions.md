---
description: >-
    Use prompts directly from property editors in the backoffice.
---

# Property Actions

Prompts are automatically registered as property actions in the Umbraco backoffice. This allows editors to execute prompts directly from text-based property editors.

## How Property Actions Work

When you create an active prompt:

1. The prompt is registered as a property action
2. The action appears on compatible property editors
3. Editors can click to execute the prompt
4. Results can be inserted or applied to properties

```
┌─────────────────────────────────────────────────────┐
│ Page Title                                    [AI ▼]│
│ ┌─────────────────────────────────────────────────┐ │
│ │ Welcome to Our Website                          │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ AI Actions:                                         │
│ ├── Improve SEO                                     │
│ ├── Translate to French                             │
│ └── Generate Alternatives                           │
└─────────────────────────────────────────────────────┘
```

## Compatible Property Editors

Property actions appear on text-based editors:

| Editor           | Support                                |
| ---------------- | -------------------------------------- |
| Textstring       | Yes                                    |
| Textarea         | Yes                                    |
| Rich Text Editor | Yes                                    |
| Markdown         | Yes                                    |
| Block Editors    | Yes (on text properties within blocks) |

## Scoping Property Actions

Control which content types can use a prompt through scoping rules:

### Allow All (Default)

The prompt appears on all compatible property editors:

```csharp
var prompt = new AIPrompt
{
    Alias = "improve-seo",
    Name = "Improve SEO",
    Instructions = "Improve this text for SEO...",
    Scope = new AIPromptScope
    {
        Mode = AIPromptScopeMode.AllowAll
    }
};
```

### Allow List

Only show on specific content types:

```csharp
var prompt = new AIPrompt
{
    Alias = "blog-enhancer",
    Name = "Enhance Blog Post",
    Instructions = "...",
    Scope = new AIPromptScope
    {
        Mode = AIPromptScopeMode.AllowList,
        ContentTypes = ["blogPost", "article"]
    }
};
```

### Deny List

Show on all content types except specified ones:

```csharp
var prompt = new AIPrompt
{
    Alias = "general-assistant",
    Name = "Writing Assistant",
    Instructions = "...",
    Scope = new AIPromptScope
    {
        Mode = AIPromptScopeMode.DenyList,
        ContentTypes = ["settings", "folder"]
    }
};
```

## Display Modes

Property actions can display results in two modes:

### Modal (Default)

Opens a centered dialog with the prompt result:

- Preview the generated content
- Copy to clipboard
- Insert into the property
- Regenerate if needed

### Panel

Opens a slide-in sidebar panel:

- Useful for longer interactions
- Maintains visibility of the content being edited
- Supports iterative refinement

## Context Extraction

When a property action executes, it automatically extracts context:

| Context         | Description                     |
| --------------- | ------------------------------- |
| `entityId`      | The content/media item ID       |
| `entityType`    | "document" or "media"           |
| `propertyAlias` | The property being edited       |
| `culture`       | Current culture variant         |
| `segment`       | Current segment (if applicable) |
| `currentValue`  | Current property value          |

This context is available in your prompt template:

```
The current value is:
{{currentValue}}

Improve this text for better readability.
```

## Applying Results

Property actions can apply results in multiple ways:

### Single Property

Replace or append to the current property:

```csharp
// Result replaces the property value
await ApplyToPropertyAsync(propertyAlias, result);
```

### Multiple Properties

Update multiple properties atomically:

```csharp
var changes = new Dictionary<string, object>
{
    ["pageTitle"] = generatedTitle,
    ["metaDescription"] = generatedMeta,
    ["summary"] = generatedSummary
};

await ApplyChangesAsync(changes);
```

{% hint style="info" %}
Multi-property updates are applied as a single operation, allowing editors to undo all changes at once.
{% endhint %}

## Creating Prompts for Property Actions

When designing prompts for property actions:

### 1. Be Context-Aware

Reference the current content context:

```
You are editing "{{name}}" ({{contentType}}).
The current {{propertyAlias}} value is:

{{currentValue}}

Improve this text while maintaining the same tone.
```

### 2. Provide Clear Output

Structure prompts to produce directly usable output:

```
Generate an improved version of this text.
Return ONLY the improved text, no explanations.

Original:
{{currentValue}}
```

### 3. Consider the Editor Experience

- Keep prompts focused on single tasks
- Use clear, action-oriented names
- Group related prompts with tags

## Managing Property Actions

### Via Backoffice

1. Navigate to the **AI** section > **Prompts**
2. Create or edit a prompt
3. Configure the scope settings
4. Set `IsActive` to true

### Visibility

Only active prompts appear as property actions. Deactivate a prompt to remove it from property editors without deleting it.

## Troubleshooting

### Prompt Not Appearing

- Verify the prompt is active (`IsActive = true`)
- Check the scope allows the content type
- Ensure the property editor is compatible
- Confirm a default chat profile is configured

### Wrong Context

- Verify variable names match your template
- Check that entity context is being passed
- Review the prompt's `IncludeEntityContext` setting

## Related

- [Concepts](concepts.md) - Prompt fundamentals
- [Template Syntax](template-syntax.md) - Variable interpolation
- [Scoping](scoping.md) - Content type rules
