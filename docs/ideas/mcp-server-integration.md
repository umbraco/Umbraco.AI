# MCP Server Integration - Future Consideration

## Status: Under Consideration

This document explores integrating external MCP (Model Context Protocol) servers with Umbraco.Ai, particularly the existing [Umbraco Developer MCP Server](https://docs.umbraco.com/umbraco-cms/reference/developer-mcp).

---

## The Opportunity

Umbraco already has an MCP Server ([umbraco/Umbraco-CMS-MCP-Dev](https://github.com/umbraco/Umbraco-CMS-MCP-Dev)) that provides **195 tools** for backoffice operations:

- Content and media management
- Document Type and Data Type creation
- Batch operations (moving, cleaning up)
- Content structure synchronization
- Log interpretation and debugging

**Instead of re-implementing these as native Umbraco.Ai tools, we could consume them via MCP.**

---

## How MCP + MEAI Works

The [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) integrates directly with Microsoft.Extensions.AI:

```csharp
// Connect to an MCP server
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Umbraco",
    Command = "npx",
    Arguments = ["-y", "@umbraco-cms/mcp-dev@16"],
});

var mcpClient = await McpClient.CreateAsync(clientTransport);

// List available tools - they ARE AIFunctions!
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

// Use directly with IChatClient
var response = await chatClient.GetResponseAsync(
    "Create a new blog post",
    new ChatOptions { Tools = [.. tools] });
```

**Key insight**: `McpClientTool` inherits from `AIFunction`, so MCP tools integrate seamlessly with MEAI's function calling.

---

## Potential Integration Approaches

### Option A: MCP as Tool Source (Alongside Native Tools)

Agents could pull tools from multiple sources:

```csharp
public interface IAiToolSource
{
    Task<IEnumerable<AITool>> GetToolsAsync(CancellationToken ct = default);
}

// Native tools from IAiToolRegistry
public class NativeToolSource : IAiToolSource { ... }

// Tools from MCP servers
public class McpToolSource : IAiToolSource
{
    private readonly McpClient _mcpClient;

    public async Task<IEnumerable<AITool>> GetToolsAsync(CancellationToken ct)
    {
        return await _mcpClient.ListToolsAsync(ct);
    }
}
```

Agent configuration could specify which sources to use:

```csharp
public sealed class AiAgent
{
    // Native tools
    public IReadOnlyList<string> EnabledToolIds { get; init; } = [];

    // MCP server connections
    public IReadOnlyList<string> EnabledMcpServerAliases { get; init; } = [];
}
```

### Option B: MCP Server as Connection Type

Treat MCP servers like AI provider connections:

```csharp
public sealed class McpServerConnection
{
    public required Guid Id { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Command to start the MCP server (e.g., "npx")
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Arguments for the command
    /// </summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>
    /// Environment variables to pass
    /// </summary>
    public IDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();
}
```

### Option C: Wrap MCP Tools as IAiTool

Create adapters that expose MCP tools through our `IAiTool` interface:

```csharp
internal class McpToolAdapter : IAiTool
{
    private readonly McpClientTool _mcpTool;

    public string Id => $"mcp:{_mcpTool.Name}";
    public string Name => _mcpTool.Name;
    public string Description => _mcpTool.Description;
    public string Category => "MCP"; // Or parse from tool name
    public bool IsDestructive => InferFromToolName(_mcpTool.Name);

    public AIFunction CreateFunction(IServiceProvider sp) => _mcpTool;
}
```

This would allow MCP tools to participate in our governance model (approval workflow, etc.).

---

## Questions & Concerns

### 1. Process Management

The Umbraco MCP Server is a **Node.js process**. Questions:
- Who starts/stops it? The Umbraco application? External process?
- How do we handle process crashes/restarts?
- Can it run in-process or must it be external?

### 2. Authentication

The MCP Server uses an **API user** to authenticate with Umbraco's Management API:
- How do we pass credentials securely?
- Can we use the current user's context instead?
- Does each Agent need its own API user for permission isolation?

### 3. Governance & Approval

MCP tools are opaque - we don't know which are destructive:
- Can we infer from tool names? (e.g., "delete_*", "update_*")
- Should we require explicit marking in configuration?
- Do we trust the MCP server's own permission model?

### 4. Performance

Each tool call goes through:
1. Agent → MCP Client → MCP Server → Management API → Umbraco

vs. native tools:
1. Agent → IAiTool → Umbraco Services

**Question**: Is the overhead acceptable? Does it matter for AI-driven operations?

### 5. Tool Discovery & Filtering

The Umbraco MCP Server has 195 tools. Questions:
- Do we expose all of them to Agents?
- How do we filter/curate which tools are available?
- Can we group them into categories for easier management?

### 6. Versioning & Compatibility

The MCP Server is versioned separately from Umbraco:
- `@umbraco-cms/mcp-dev@16` for Umbraco 16.x
- How do we ensure compatibility?
- What happens when they get out of sync?

### 7. Duplication vs. Reuse

Should we:
- **A) Use MCP exclusively** - No native tools, everything via MCP
- **B) Native + MCP** - Build critical tools natively, use MCP for extras
- **C) Native only** - Don't integrate MCP, build what we need

---

## Benefits of MCP Integration

### 1. Immediate Access to 195 Tools
No need to implement content/media/schema operations - they exist!

### 2. Maintained by Umbraco
The MCP Server is an official Umbraco product, kept up to date.

### 3. Consistency
Same operations available via Claude Desktop, Cursor, Copilot AND Umbraco.Ai Agents.

### 4. Extensibility
Other MCP servers (e.g., [Umbraco Commerce MCP](https://github.com/umbraco/Umbraco.Commerce.Mcp)) could also be integrated.

---

## Concerns with MCP Integration

### 1. External Process Dependency
Requires Node.js and a running MCP server process.

### 2. Complexity
Another moving part to configure, monitor, and troubleshoot.

### 3. Governance Gap
MCP tools bypass our native governance model unless we wrap them.

### 4. Performance Overhead
Additional network hop and process communication.

### 5. Different Permission Model
MCP uses API user permissions; Agents use user group restrictions. Reconciling these could be confusing.

---

## Recommendation

**Explore as a Phase 2 feature**, after core Agents functionality is working.

### Phase 1 (Current Plan)
- Build core tool infrastructure in Umbraco.Ai.Core
- Build Agents with native tools (content only for MVP)
- Prove the model works with hand-built tools

### Phase 2 (MCP Integration)
- Add MCP client support to Umbraco.Ai.Agents
- Allow Agents to connect to MCP servers as tool sources
- Wrap MCP tools for governance (approval workflow)
- Consider Option C (McpToolAdapter) to unify the model

### Questions to Answer First
1. Does the Umbraco MCP Server support HTTP transport? (Would simplify integration vs. stdio)
2. Can we run MCP in-process? (Avoid external Node.js dependency)
3. What's the overlap between MCP tools and what we'd build natively?

---

## Related Links

- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Umbraco Developer MCP Docs](https://docs.umbraco.com/umbraco-cms/reference/developer-mcp)
- [Umbraco CMS MCP Dev](https://github.com/umbraco/Umbraco-CMS-MCP-Dev)
- [Umbraco Commerce MCP](https://github.com/umbraco/Umbraco.Commerce.Mcp)
- [MEAI MCP Overview](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)

---

## Related Decisions

| Decision | Current Choice |
|----------|----------------|
| Phase 1 tools | Native IAiTool implementations only |
| MCP integration | Deferred to Phase 2 |
| Tool source model | Single source (IAiToolRegistry) for now |
