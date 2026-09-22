# ADR: Provider-hosted tools (web search, code interpreter, remote MCP)

- **Status:** Deferred (no action now)
- **Date:** 2026-06-16
- **Deciders:** Umbraco.AI maintainers
- **Scope:** `Umbraco.AI.Agent` tool system
- **Related:** [[project_meai_adoption_opportunities]], [[project_hitl_approval_architecture]]

## Context

MEAI 10.6 exposes **provider-hosted tools** — tools the model *provider* executes server-side, not us:
- `HostedWebSearchTool` — provider-run web search.
- `HostedCodeInterpreterTool` — provider-run sandboxed code execution.
- `HostedMcpServerTool` + approval modes (`HostedMcpServerToolAlwaysRequireApprovalMode`, `…NeverRequireApprovalMode`, `…RequireSpecificApprovalMode`) — provider-connected remote MCP servers.

These are a third tool category alongside the two we already have:
1. **Backend tools** (`IAITool` → `AIToolFunction`) — *we* execute, server-side, with scope/permission gating (and, per the HITL plan, optional approval).
2. **Frontend tools** (`AIFrontendToolFunction`) — the *browser* executes, via the AG-UI interrupt/resume flow.
3. **Hosted tools** (new) — the *provider* executes; we just declare them in `ChatOptions.Tools` and receive results.

### What we already have (web/search tooling)

- `semantic_search` (`Umbraco.AI.Search`, `SearchScope`) — vector search over indexed site content. *Internal discovery.*
- `search_umbraco` (`Umbraco.AI.Core`, `SearchScope`) — keyword search over Umbraco content. *Internal discovery.*
- `fetch_webpage` (`Umbraco.AI.Core`, `WebScope`, **disabled by default** via `AIWebFetchOptions.Enabled`) — given a **known URL**, fetches and extracts its text, with SSRF protection (`UrlValidator`) and size limits (`LimitedStream`). *External **retrieval**, not search.*

The capability we do **not** have is **external discovery by query** — "find current information about X on the web." An agent can *read* a page when it (or the user) supplies the URL, but it cannot *find* one. This is precisely the gap `HostedWebSearchTool` fills, and it complements `fetch_webpage` (search → fetch) rather than replacing any existing tool. The expensive part to build ourselves — a web search API integration (Bing/Brave/Google) plus the API key the user must configure and we must operate — is exactly what a provider's hosted web search bundles.

This ADR was raised during the MEAI adoption review. When asked what's driving it, the answer was **"just completeness — no specific demand, flagged only because MEAI supports it."**

## Decision

**Defer.** Do not implement provider-hosted tools now. Revisit only on concrete demand (see triggers).

## Rationale

1. **No driver (YAGNI).** There is no user requirement for hosted web search, code interpreter, or remote MCP today. Building speculative capability is exactly the unexamined-assumption trap.
2. **Provider fragmentation.** Hosted tools are provider-specific — OpenAI and Anthropic support different subsets, and most of our other providers (Google, Mistral, Bedrock, the smaller ones) support none. A hosted-tool feature is inherently non-portable across our provider matrix, so it can't be a uniform agent capability; it would need per-provider capability detection and graceful degradation. That's real complexity for a feature nobody has asked for.
3. **Sits outside our governance model.** Our value-add is the scope/permission system (`IAITool.ScopeId`/`IsDestructive`, agent `AllowedToolScopeIds`, user-group overrides) and — per the HITL plan — runtime approval for destructive tools. Provider-hosted tools execute inside the provider; we don't see or gate their individual operations the same way. Web search and code interpreter would bypass our permission model entirely, which is a governance regression for a backoffice product unless carefully bounded.
4. **Interaction cost with existing machinery.** Hosted tools add another content/turn shape that `AGUIStreamingService` and `AIToolReorderingChatClient` would need to reason about (results arrive provider-side, interleaved with our function calls). Non-trivial, and unjustified without demand.

## What we are NOT claiming

- The `AIToolReorderingChatClient` is **not** a workaround for missing hosted tools and would not be simplified or removed by adopting them. It solves frontend-vs-backend tool *ordering* and stays regardless. (Correcting an earlier framing in the adoption review.)

## Revisit triggers

Re-open this ADR when any of:
1. Agents demonstrably need to answer with **external/current** information that the site's own content can't provide. Note this is *web discovery by query*, which none of our tools do — `semantic_search`/`search_umbraco` cover internal content, and `fetch_webpage` only retrieves a *known* URL. When that need is real, adopt the provider's hosted web search (it bundles the search-API integration we'd otherwise build/operate) and let it feed `fetch_webpage` for deep reads.
2. We want to expose third-party **remote MCP** servers to agents without wrapping each as an `IAITool` — `HostedMcpServerTool` becomes the natural mechanism, and its approval modes dovetail with the [[project_hitl_approval_architecture]] work.
3. A data-analysis use case makes provider code interpreter worthwhile in a CMS context.

## Approach sketch (for when a trigger fires — not now)

- Treat hosted tools as an **opt-in, per-agent, per-provider** capability with explicit capability detection; degrade gracefully (omit the hosted tool) on providers that don't support it, and surface that in the agent config UI.
- Decide governance up front: which hosted tools are allowed, and how their use is represented to the user (they bypass our per-operation scope gating, so at minimum gate *enabling* the hosted tool at config time).
- For `HostedMcpServerTool`, reuse the HITL approval flow via the MCP approval modes rather than inventing a parallel path.
- Start with exactly one hosted tool tied to the triggering demand — do not adopt the whole family at once.
