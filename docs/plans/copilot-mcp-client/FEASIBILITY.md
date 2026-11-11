# Feasibility: Copilot as MCP client

> **Status:** Feasibility study, 21-10-2026, checked against `v18/dev`. Source idea:
> "Copilot as MCP client: external context for Umbraco.AI" (Claude Doc by Filip Bruun Bech-Larsen,
> 20-10-2026). Not a design. Sections touching personal data are an AI-assisted draft, pending
> legal/compliance review.

## Verdict

**Feasible, but bigger than it looks.** The tool pipeline can take an external function today, and
Copilot Workspace already has the "attach to project, inherit in conversation" shape. What does
not exist is: a runtime tool source, a server registry, per-user OAuth storage, and per-call audit.
Those four are the real build.

**The collaboration half does not stack cleanly on share-conversations.** Share-conversations v1
makes recipients strictly read-only. The idea's "a colleague continues a shared conversation and
Copilot fetches as that colleague" needs multi-writer or fork, both explicit non-goals of that
feature. What *does* stack: source labels, the share-time warning, and a per-server sharing policy.

## What exists vs. what must be built

| Idea concept | Today | Gap | Size |
|---|---|---|---|
| MCP client | No `ModelContextProtocol` reference anywhere (Umbraco.AI or Automate) | Add `ModelContextProtocol` (2.3.0, net10, stable since 1.0.0 on 25-02-2026). New dependency, needs sign-off | S |
| External tools in a run | `AIAgentFactory.CreateStandardAgentAsync` already accepts `additionalTools` (`AIAgentFactory.cs:71,82,210-217`), but only AG-UI frontend tools fill it | Runtime tool source that resolves attached servers, lists tools, filters by allowlist, wraps them | M |
| Registry (admin-curated) | Tools are compile-time only (`AIToolCollectionBuilder`, `[AITool]`) | New entity + workspace editor + API + migrations (SQL Server + SQLite) | M |
| Attachment per project / conversation | `AIProject` / `AIConversation` carry `ContextIds` + `Resources`; conversation inherits project via `StreamConversationAGUIController.BuildRuntimeContextAsync` (`:300-354`) | Add server ids alongside, same inheritance pattern; context panel section | S–M |
| Shared connection | Connections encrypt `IsSensitive` fields (`AISensitiveFieldProtector`) | Reuse the editable-model + encryption path for registry credentials | S |
| Personal connection (per-user OAuth) | None in Umbraco.AI. CMS has no per-user secret store. Automate's OpenIddict OAuth (`IOAuthCredentialsService`) is per-connection, not per-user | Per-user token table, server-side OAuth callback endpoint, "connect" UI, refresh. MCP SDK helps (`ClientOAuthOptions.TokenCache`, dynamic client registration), but its redirect delegate assumes an interactive client, so the callback must be our own endpoint | L |
| Read-first, writes opt-in + confirm | HITL exists via `ApprovalRequiredAIFunction`, but auto-wrapping only applies to ids in `AIToolCollection` with `IsDestructive` (`AIAgentFactory.cs:161-204`) | Wrap MCP tools ourselves from registry flags. Do not trust the server's own `readOnlyHint` | S |
| Audit per external call | No per-tool-call audit row. Tool calls only appear as text inside LLM-call snapshots (`AIChatMessageFormatter`) | `AIToolExecutedNotification` handler (has `RuntimeContext`, `CallId`, args, result, duration) writing structured rows with user, server, tool, conversation | M |
| Source labels | Tool results persist as full M.E.AI messages in `ContentJson`; `FunctionResultContent.AdditionalProperties` survives the round trip (`ConversationChatHistoryProvider.cs:514-592`) | Stamp server, acting user, fetched-at on the result; render a chip in chat | S |
| Deployable registry | `Umbraco.AI.Deploy` connectors exist for connections | New connector. Credentials must never travel in Deploy files; the current sensitive-settings handling needs fixing first (tracked internally) | M |

Rough total for the non-sharing scope: about the size of Copilot Workspace projects. Sizes are
relative guesses, not estimates.

## What M.E.AI and MAF give us

We are on `Microsoft.Extensions.AI` 10.7 and `Microsoft.Agents.AI` (MAF) 1.18. Neither has its own
MCP *client*. Both offer the same two routes:

| Route | How | Who calls the MCP server | Fit |
|---|---|---|---|
| **Client-side tools** | Official MCP C# SDK: `McpClient.CreateAsync(new HttpClientTransport(...))`, `ListToolsAsync()`. Each `McpClientTool` *is* an M.E.AI `AIFunction`, so it drops into `ChatOptions.Tools` or a MAF agent's tool list unchanged | Our server | **Recommended.** Works with every provider. Runs through our function invoker, so `AIToolExecuting`/`Executed` notifications, `ApprovalRequiredAIFunction`, audit and provenance all apply. Per-user tokens never leave our server |
| **Provider-hosted** | M.E.AI `HostedMcpServerTool` (`ServerAddress`, `AllowedTools`, `ApprovalMode`, `Headers`); `AsOpenAIResponseTool()` maps it for OpenAI Responses; MAF also exposes Foundry's `MCPToolDefinition` | The AI provider | **Not for v1.** Only some providers support it. The provider calls the CRM with our token, so the token and the fetched data go to the provider directly. Private/intranet servers are unreachable. Calls bypass our invoker, so audit, approval and provenance need separate handling. Matches the reasons in the existing "defer provider-hosted tools" decision |

What the libraries do **not** give us, so we still build it: the server registry, per-user token
storage (the SDK only offers an `ITokenCache` hook), the OAuth callback endpoint, tool allowlists
enforced on our side, and provenance labels. MAF adds nothing MCP-specific on .NET beyond what the
SDK and M.E.AI already provide. Its docs say to review and log all data shared with third-party MCP
servers, which matches the audit requirement.

Not checked yet: whether the Anthropic and Google M.E.AI adapters map `HostedMcpServerTool`. It
only matters if we ever take the hosted route.

## Collaboration and share-conversations

Share-conversations (`v18/feature/share-conversations`, SC-01 to SC-16 built, SC-17 to SC-19 open,
not merged) gives a read-only view to named recipients, Snapshot or Live. Mapped against the idea:

| Idea behaviour | Fits share-conversations v1? |
|---|---|
| History shared in full | Yes. Same "show as-is, warn at share time" decision as attachments |
| Warning at share time listing systems + who fetched | Partly. Today the banner is a boolean (`hasAttachments`) with fixed copy. Needs a provenance field derived from message `AdditionalProperties` and new copy |
| Per-server policy, "not shareable" blocks sharing | New. A server-side check in the share create endpoint, plus a disabled state in the picker |
| Source labels | Independent of sharing; useful for the owner too |
| Colleague continues the conversation, fetching as themselves | **No.** Recipients cannot send. Needs multi-writer or fork first |
| "No borrowed access" | Trivially true in v1, because nobody but the owner can trigger a fetch |

So: build MCP phases 1–2 without waiting. **Update 21-10-2026:** the plan is to extend
share-conversations to collaborative (multi-writer) conversations; how is out of scope here. For
MCP that means access is per message: calls run as the sender, never through another
participant's token, and the run needs an acting user per message (today only the conversation's
`UserKey`). Detail lives in the design doc "Copilot MCP client in Umbraco.AI".

## Risks

- **Prompt injection through fetched data.** External text lands in the model context next to
  Umbraco write tools (create/update content). A CRM note can tell the model to publish or delete.
  Existing HITL on destructive Umbraco tools is the main guard; the Workspace surface does not set
  `RestrictsDestructiveBackendTools` today. Needs a deliberate decision.
- **Tool poisoning.** Tool names and descriptions come from the server and go straight to the
  model. The registry should pin an allowlist by tool name and show the description the admin
  approved, and flag when the server's list changes.
- **Personal data to the AI provider.** "Aggregates over individuals" is only as strong as the
  external server's tools. We can restrict *which* tools, not *what* they return. Redaction
  patterns in audit logs do not stop data reaching the provider. Needs a legal/DPA review before
  any wording promises this.
- **Hosting.** Remote HTTP (Streamable HTTP) only. Stdio servers mean launching processes (often
  Node) on the web server, which will not work on Umbraco Cloud. Recommend HTTP-only for v1.
- **Load balancing.** Per-user tokens and MCP sessions must be shared across servers (database,
  not memory). OAuth callback must work behind the Cloud front door.
- **Tool overload.** One CRM server can expose 50+ tools. The per-server allowlist matters more
  than per-conversation attaching.
- **Sidebar Copilot.** The idea is framed around Workspace (persisted conversations). The sidebar
  has no persisted conversation to attach to; if wanted there, attach at agent level instead.
- **Both lines.** Ships on v17 and v18. The SDK targets net10, so no blocker, but it doubles the
  migration and backport work.

## Recommended phasing

1. **Shared-account, read-only.** Registry (HTTP servers, tool allowlist, shared credential),
   attach to project/conversation, runtime tool source, source labels, per-call audit. Proves the
   value with one CRM and one analytics server.
2. **Write tools + personal OAuth.** Opt-in write tools with per-action approval; per-user token
   store, connect flow, "not connected" handling.
3. **Sharing integration** (after share-conversations merges). Provenance-aware share warning,
   per-server sharing policy.
4. **Deploy** for the registry, once the credential-handling fix lands. Can move earlier if
   credentials stay out of Deploy files from day one.

## Open questions for the team

- [ ] Which two servers do we prove it with? Do they offer Streamable HTTP and OAuth?
- [ ] Does the registry live in `Umbraco.AI.Agent` (so Automate agents can use it too) or in
      Copilot Workspace?
- [ ] Should "continue a shared conversation" become a fork feature, a multi-writer feature, or be
      dropped from this idea?
- [ ] Approve adding `ModelContextProtocol` as a dependency?
- [ ] Legal/DPA review of sending external personal data to AI providers.

## Sources

- [ModelContextProtocol on NuGet](https://www.nuget.org/packages/ModelContextProtocol) (version history, net10 deps)
- [MCP C# SDK transports](https://csharp.sdk.modelcontextprotocol.io/v1/concepts/transports/transports.html)
- [ClientOAuthOptions API](https://csharp.sdk.modelcontextprotocol.io/v1/api/ModelContextProtocol.Authentication.ClientOAuthOptions.html)
- [HostedMcpServerTool API (M.E.AI)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.hostedmcpservertool)
- [MAF: Using MCP tools](https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools)
- [MAF: Hosted MCP tools](https://learn.microsoft.com/en-us/agent-framework/agents/tools/hosted-mcp-tools)
- Earlier repo thinking: `docs/ideas/mcp-server-integration.md`, `.claude/memory/provider-hosted-tools-deferred.md`
  (revisit trigger 2 now fires)
- Share-conversations plan: `docs/plans/share-conversations/`
