---
name: provider-hosted-tools-deferred
description: Decision to defer MEAI provider-hosted tools (hosted web search, code interpreter, remote MCP) until concrete demand exists
type: decision
---

**Decision:** Defer provider-hosted tools (MEAI 10.6's `HostedWebSearchTool`,
`HostedCodeInterpreterTool`, `HostedMcpServerTool`). Do not implement now. Decided 2026-06-16,
scope: `Umbraco.AI.Agent` tool system.

**Why:**
- No driver — raised only during a MEAI adoption review for completeness, not from any user
  requirement. Building it now would be speculative (YAGNI).
- Provider fragmentation — hosted tools are provider-specific (OpenAI/Anthropic support different
  subsets; Google, Mistral, Bedrock, and the smaller providers support none), so it can't be a
  uniform agent capability without per-provider capability detection and graceful degradation.
- Sits outside our governance model — Umbraco's value-add is the scope/permission system
  (`IAITool.ScopeId`/`IsDestructive`, `AllowedToolScopeIds`, user-group overrides, and per-operation
  HITL approval, see [[hitl-approval-architecture]]). Provider-hosted tools execute inside the
  provider, invisible to that gating — adopting them naively would be a governance regression.
- Adds interaction cost — `AGUIStreamingService` and `AIToolReorderingChatClient` would need to
  reason about another content/turn shape (provider-side results interleaved with our function
  calls), which isn't justified without demand.
- Not a workaround gap: `AIToolReorderingChatClient` solves frontend-vs-backend tool ordering and
  stays regardless of this decision — it would not be simplified or removed by adopting hosted
  tools.

**How to apply:**
- Don't add `HostedWebSearchTool`/`HostedCodeInterpreterTool`/`HostedMcpServerTool` support
  speculatively. Point anyone proposing it at the revisit triggers below.
- **Revisit triggers:** (1) agents demonstrably need to answer with external/current information
  the site's own content can't provide — note this is *web discovery by query*, which
  `semantic_search`/`search_umbraco` (internal content) and `fetch_webpage` (known-URL retrieval
  only) don't cover; (2) a need to expose third-party remote MCP servers to agents without
  wrapping each as an `IAITool` — `HostedMcpServerTool`'s approval modes would dovetail with the
  HITL approval work; (3) a data-analysis use case makes provider code interpreter worthwhile.
- If a trigger fires: treat hosted tools as opt-in, per-agent, per-provider, with explicit
  capability detection and graceful degradation on unsupported providers; gate *enabling* the
  hosted tool at config time since per-operation scope gating doesn't apply; reuse the HITL
  approval flow for `HostedMcpServerTool` rather than inventing a parallel path; adopt exactly one
  hosted tool tied to the triggering demand, not the whole family at once.
