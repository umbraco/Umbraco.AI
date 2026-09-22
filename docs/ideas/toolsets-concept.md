# ToolSets - Future Consideration

## Status: Under Consideration

This document captures thinking around a potential "ToolSets" feature for organizing and governing AI tools. **Not currently planned for implementation.**

---

## The Idea

ToolSets would be named collections of tools that can be referenced by Agents, providing:

- **Organization**: Group related tools together (e.g., "Content Management", "Translation", "Read Only")
- **Governance**: Optionally attach user group permissions to the set

```csharp
// Hypothetical ToolSet model
public sealed class AIToolSet
{
    public required Guid Id { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Tool IDs included in this set.
    /// </summary>
    public IReadOnlyList<string> ToolIds { get; init; } = [];

    /// <summary>
    /// User groups allowed to use this ToolSet.
    /// Empty means no restriction.
    /// </summary>
    public IReadOnlyList<string> AllowedUserGroups { get; init; } = [];
}
```

Agents could then reference ToolSets instead of (or in addition to) individual tools:

```csharp
public sealed class AIAgent
{
    public IReadOnlyList<string> EnabledToolSetAliases { get; init; } = [];
    public IReadOnlyList<string> AdditionalToolIds { get; init; } = [];
}
```

---

## Potential Benefits

### 1. Convenience

Instead of listing 6 individual tool IDs on every agent that needs content editing, reference one ToolSet:

```
Agent: "Content Assistant"
  EnabledToolSets: ["content-management"]

vs.

Agent: "Content Assistant"
  EnabledTools: ["content.search", "content.get", "content.create",
                 "content.update", "content.publish", "content.delete"]
```

### 2. Consistency

Changes to a ToolSet automatically apply to all Agents using it. Add a new content tool? All content-editing agents get it.

### 3. Reusable Permission Bundles

If ToolSets have `AllowedUserGroups`, they become reusable permission templates:

```
ToolSet: "Destructive Content Operations"
  Tools: [content.create, content.update, content.publish, content.delete]
  AllowedUserGroups: ["Administrators", "Senior Editors"]
```

---

## Concerns & Questions

### 1. Added Complexity

We already have: Connections → Profiles → Agents → Tools

Adding ToolSets creates another layer: Connections → Profiles → ToolSets → Agents → Tools

**Question**: Is this too many abstractions for the personas who configure this (developers, IT admins, agency partners)?

### 2. Where Does Governance Live?

Current design: **Agents own governance**. Tools in Core have no permissions.

With ToolSets having permissions, we'd have **two governance layers**:

- ToolSet level: "Only Admins can use destructive tools"
- Agent level: "Only Editors can use this agent"

**Question**: Is this clearer or more confusing? What happens when they conflict?

### 3. Bypassing ToolSets

Developers using Core directly can always access tools via `IAIToolRegistry`:

```csharp
_toolRegistry.GetTool("content.update")  // Always works, no governance
```

ToolSet governance would only apply within the Agents layer.

**Question**: Is this acceptable? It matches our "Core has no enforcement" principle, but could lead to accidental bypasses.

### 4. Categories vs ToolSets

Tools already have a `Category` property for organization:

```csharp
[AITool("content.update", "Update Content", Category = "Content")]
```

And `IAIToolRegistry` provides:

```csharp
_toolRegistry.GetToolsByCategory("content")
```

**Question**: Is this sufficient for organization? Are ToolSets only valuable if they add governance?

### 5. No `GetToolsForAgent()` Method

We decided NOT to expose a method like `agentService.GetToolsForAgent("content-assistant")` because it would allow bypassing agent governance while "borrowing" its tool configuration.

ToolSets arose partly from wanting a way to group tools for reuse. But if we don't expose raw tools from agents, do we need another grouping mechanism?

---

## Alternative: Just Use Categories

A simpler approach: rely on built-in Categories and let Agents reference them:

```csharp
public sealed class AIAgent
{
    /// <summary>
    /// Enable all tools in these categories.
    /// </summary>
    public IReadOnlyList<string> EnabledCategories { get; init; } = [];

    /// <summary>
    /// Enable specific additional tools.
    /// </summary>
    public IReadOnlyList<string> EnabledToolIds { get; init; } = [];

    /// <summary>
    /// Explicitly exclude certain tools (override).
    /// </summary>
    public IReadOnlyList<string> ExcludedToolIds { get; init; } = [];
}
```

This gives grouping without a new entity. Governance stays purely at the Agent level.

---

## Recommendation

**Defer ToolSets** until we have real-world usage patterns that justify the added complexity.

Start with:

1. **Categories on tools** (built-in organization)
2. **Individual tool IDs on Agents** (explicit, simple)
3. **Agent-level user group permissions** (single governance layer)

Revisit ToolSets if:

- Users complain about repetitive tool configuration across agents
- A clear need emerges for permission bundles separate from agents
- The simpler model proves insufficient

---

## Related Decisions

| Decision               | Current Choice                                          |
| ---------------------- | ------------------------------------------------------- |
| Governance in Core     | No - Core has no enforcement                            |
| Tool access from Agent | No `GetToolsForAgent()` - prevents bypassing governance |
| Tool organization      | Categories (metadata on tools)                          |
| Permission layer       | Agent-level only                                        |
