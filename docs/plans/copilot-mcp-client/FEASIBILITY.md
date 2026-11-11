# Feasibility: Copilot as MCP client

> **Status:** Feasibility study, 21-10-2026, checked against `v18/dev`. Source idea:
> "Copilot as MCP client: external context for Umbraco.AI" (Claude Doc by Filip Bruun Bech-Larsen,
> 20-10-2026). Not a design. Sections touching personal data are an AI-assisted draft, pending
> legal/compliance review. **Updated 11-11-2026** with the connection and sign-in decisions (see
> [Connections and sign-in](#connections-and-sign-in-decided-11-11-2026)).

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
| Servers on an agent | Agents already pick tools via `AllowedToolIds` / `AllowedToolScopeIds`, with per-user-group overrides (`AIAgentUserGroupPermissions`) | Each server appears as a tool scope; add the "Let users add their own connections" switch. See [Which servers a run gets](#which-servers-a-run-gets-decided-11-11-2026) | S |
| User-chosen connections per chat / project | `AIProject` / `AIConversation` carry `ContextIds` + `Resources`; conversation inherits project via `StreamConversationAGUIController.BuildRuntimeContextAsync` (`:300-354`) | Add server ids alongside, same inheritance pattern; a connections toggle in the chat and context panel. Sidebar keeps it per chat only | S–M |
| Auto agent pick respects connections | Copilot auto pick is an `IAIAgentSelector` chain | New selector step that only picks agents allowing user connections when the user turned some on | S |
| Shared connection (admin signs in once) | Connections encrypt `IsSensitive` fields (`AISensitiveFieldProtector`). Automate has per-connection OAuth via OpenIddict, but with hand-written handlers per service (e.g. Slack) | Generic OAuth through the MCP SDK: discovery, dynamic client registration, our own callback endpoint, token table, refresh. Moved into phase 1 (see below) | M |
| Personal connection (per-user OAuth) | None in Umbraco.AI. CMS has no per-user secret store. Automate's OpenIddict OAuth (`IOAuthCredentialsService`) is per-connection, not per-user | Reuses the shared flow; adds the user column in use, a "connect" UI, "not connected" handling, cleanup on user delete. MCP SDK helps (`ClientOAuthOptions.TokenCache`, dynamic client registration), but its redirect delegate assumes an interactive client, so the callback must be our own endpoint | M (on top of shared) |
| User group access per server | None | Admin picks which user groups may use each server; enforced in the runtime tool source and the "connect" endpoint | S |
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

## Connections and sign-in (decided 11-11-2026)

**One connection type.** Every server is a remote MCP server over Streamable HTTP. There are no
per-service connection types like Automate's (Slack and so on), because MCP standardises sign-in:
the server publishes where to log in, and the client can often register itself.

**OAuth is phase 1.** Many hosted MCP servers (HubSpot, Notion and others) only accept OAuth, with
no API key option. A static API key or bearer token stays as an option for servers that offer one.

**Use the MCP SDK's OAuth, not Automate's OpenIddict package.** The SDK already handles server
discovery and dynamic client registration; OpenIddict would need custom code per service. Patterns
worth copying from Automate (`Umbraco.Automate.OpenIddict`): the callback controller, credentials
in their own store that the connection points to by id, refresh on use, and the cleanup job for
sign-ins that were started but never finished.

### Two sign-in modes per server

| | Shared account | Each user signs in |
|---|---|---|
| Who signs in | An admin, once, from the server's editor | Each editor, with their own account |
| Whose data Copilot sees | Whatever the admin's account can see, for every allowed editor | Only what that editor's own account can see |
| Token stored against | The server | The user + the server |
| Phase | 1 | 2 |

### The admin (registry)

- A new "MCP Servers" list in the AI section, next to Connections. Each server has a name, URL,
  sign-in mode, allowed tools and allowed user groups.
- "Connect" opens the service's own login page in a popup. Our callback page receives the result.
  The server then shows who it is connected as, with "Disconnect" and "Reconnect".
- Where a service does not allow dynamic client registration, the admin pastes a client ID and
  secret on the server's settings.
- If a refresh fails, the server shows "Needs reconnecting", and Copilot tells the editor that the
  source is unavailable.

### User group access

- The admin picks which user groups may use each server. Applies to both sign-in modes.
- In shared mode this is the main guard, since every allowed editor sees what the admin's account
  can see.
- In personal mode it controls who sees a "Connect" button at all.
- Enforced server-side: the runtime tool source leaves out servers the acting user's groups do not
  allow, and the connect endpoint refuses them. Never only hidden in the UI.

### Per-user tokens (phase 2)

- **Admin allowlist is the only way in.** Editors can only connect to servers the admin has added
  and set to "Each user signs in". They cannot enter a server address of their own. The admin still
  decides which tools are allowed; the editor's account only limits what data comes back.
- **Where editors connect.** Inline in chat when needed: Copilot leaves out that server's tools,
  says "Connect HubSpot to use this", and shows a "Connect" button. A "My connections" panel in the
  user profile is also possible, since the CMS lets packages add a user profile app
  (`umbUserProfileApp`, as Theme and History do). To confirm in design whether the popup sign-in
  flow works well from there.
- **Admin oversight.** The admin can see who has connected to a server and disconnect any user.
- **Storage.** One MCP client per user per server, never shared. Tokens refresh on use. Our own
  `ITokenCache` reads and writes the token table for the current user.
- **Cleanup.** A user's tokens go when the user is deleted or disabled. All tokens for a server go
  when the server is removed.
- **Acting user.** Calls run as the person who sent the message, not the conversation owner. Today
  the run only knows the owner (`UserKey`), so this needs adding.
- **Background runs.** Automate and scheduled agents have no editor present, so they can only use
  shared servers.
- **Personal data.** Stored tokens and fetched results are personal data under GDPR. Needs legal
  review before release.

### Build phase 1 so phase 2 is an add-on

Per-user can be added later without moving data, if phase 1 already has:

- A sign-in mode setting on each server, with only "Shared account" offered at first.
- Tokens in their own table with a user column (empty for shared), not inside the server's
  settings.
- A token lookup that is told who is asking, even though phase 1 ignores it.
- User group access on each server.

Skip these and phase 2 means a data migration and reworking the token code.

## Which servers a run gets (decided 11-11-2026)

Servers are **not global**. One CRM server can add 50+ tools, and every server is another way for
outside text to steer the model, so each one is opt-in. This replaces the source idea's "attach a
server to a project" as the main mechanism.

**What users expect.** Claude and ChatGPT both work the same way: an admin turns a service on (and
on their enterprise plans can limit it by role or group), each person connects their own account,
and they choose which connections to use in a chat. A purpose-built agent can be locked to a fixed
set (ChatGPT's agent builder picks which app actions the agent may use). Users will expect Copilot
to behave the same, so the model below follows that pattern.

### Three layers

1. **Server (admin).** Allowed tools and allowed user groups. This is the ceiling; nothing below
   can add tools or groups the server does not allow.
2. **Agent (admin).** Two settings, on the agent's Tools tab:
   - **Always use these servers.** Each server shows as a tool scope next to the agent's other tool
     groups. These are always on for the agent; the user does nothing. In shared mode they just
     work; in personal mode the user gets a "Connect" prompt the first time. The agent's existing
     per-user-group overrides apply (e.g. "Writers don't get HubSpot on this agent").
   - **Let users add their own connections.** Yes/no. Yes: the agent also uses the servers the user
     turned on for that chat or project. No: only the fixed list, whatever the user has connected.
     Proposed default: **No**, so a new agent never quietly accepts outside data. Not yet confirmed.
3. **User (editor).** Connects their own accounts (personal mode) and turns connections on per
   conversation, or per project in Workspace. Only servers their user groups allow are offered. A
   project does not narrow the agent; it says "this project uses HubSpot" for that user's chats.

| Always use | Let users add | What the agent gets |
|---|---|---|
| None | No | No outside servers (today's behaviour) |
| HubSpot | No | Only HubSpot, always |
| None | Yes | Only what the user turns on |
| HubSpot | Yes | HubSpot, plus whatever the user turns on |

Example agents: a **Campaign helper** with HubSpot and Analytics fixed, users can't add; a
**General Copilot** with nothing fixed, users can add; a **Content publisher** with nothing fixed,
users can't add, because it edits content and should stay sealed off from outside text.

A tool runs only when every layer agrees: the server allows the tool, the user's group is allowed
on the server, the agent allows it (fixed, or user-added with the switch on), and in personal mode
the user has connected.

### Auto agent pick

Copilot often picks the agent automatically, so a user could turn on HubSpot and get an agent with
"Let users add" off. Two fixes, together:

1. **The picker respects the user's connections.** A new step in the `IAIAgentSelector` chain: if
   the user turned on connections in this chat, only pick from agents that allow user connections.
2. **Never ignore a connection silently.** If the chosen agent cannot use a connection, say so in
   the chat ("HubSpot isn't available to the Content publisher agent"). This covers a hand-picked
   agent, or no allowed agent fitting the request.

Optional: once the agent is known, grey out connections it cannot use, with a short reason. With
auto pick that only happens after the message is sent, so fix 2 is still needed.

The fixed list has the same gap (the picker may never choose the Campaign helper), but that is how
auto pick already works for every agent setting today.

### Sidebar, Workspace and Automate

| | Sidebar | Workspace | Automate |
|---|---|---|---|
| Fixed servers on the agent | Yes | Yes | Yes |
| User-added connections | Per chat | Per conversation or project | No, no user present |
| Shared account | Yes | Yes | Yes |
| Each user signs in | Yes, inline "Connect" in chat | Yes, inline "Connect" in chat | No |
| Source labels on results | Yes, gone when the sidebar closes | Yes, saved with the conversation | In the run log |
| MCP read tools | Yes | Yes | Yes |
| MCP write tools (phase 2) | **No, always blocked** | Yes, with approval | Per the action's tool permissions |

### MCP writes are blocked in contextual Copilot (decided 11-11-2026)

Contextual Copilot (the sidebar, `CopilotAgentSurface`) declares
`RestrictsDestructiveBackendTools`. It can still edit the item on screen through its
context-bound frontend tools, but every destructive *backend* tool is dropped, because a backend
tool can change any item by id and so reach outside what the user has open
(`AIAgentFactory.cs:171-205`). MCP tools always run on the backend and always act outside the
page, so the same rule applies to them.

- **Rule.** Every MCP tool the admin has flagged as a write is dropped in contextual Copilot,
  whatever the agent or the user allows. MCP read tools are unaffected.
- **Why not "writes related to this page".** We cannot check that an external record belongs to the
  open page; only the model claims it does, and fetched text (e.g. a CRM note) can steer that
  claim. The sidebar is also where outside text sits closest to the open item.
- **What the user sees.** Asking to change an external system from the sidebar gets a clear
  answer that it can't do that from here, with a pointer to Copilot Workspace, where writes are
  allowed with approval. `ContextualEditingGuidanceContributor` already explains the Umbraco
  limit; it needs a line covering external systems.
- **Build note: the existing lock does not cover MCP tools.** It filters only tools registered in
  `AIToolCollection`. MCP tools arrive through `additionalTools`, so the MCP tool source must apply
  the surface's lock itself. Which MCP tools count as writes comes from the admin's flags in the
  registry, never from the server's own `readOnlyHint`.
- **Rejected for now: a per-server opt-in** ("Allow write tools in contextual Copilot", always with
  approval). Revisit only if users ask to edit external records from the page.

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
  `RestrictsDestructiveBackendTools` today. Needs a deliberate decision. (Contextual Copilot does
  set it, and MCP writes are blocked there; see above.)
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
  than which chats turn a server on.
- **Sidebar Copilot.** The idea is framed around Workspace (persisted conversations). Resolved by
  making the agent the main gate: fixed servers work in the sidebar unchanged; user-added
  connections there last only for that chat.
- **Both lines.** Ships on v17 and v18. The SDK targets net10, so no blocker, but it doubles the
  migration and backport work.

## Recommended phasing

1. **Shared-account, read-only.** Registry (HTTP servers, tool allowlist, user group access,
   sign-in mode), shared OAuth through the MCP SDK (plus static key/token where offered), token
   table and callback endpoint, "Always use these servers" on agents, runtime tool source, source
   labels, per-call audit. Proves the value with one CRM and one analytics server.
2. **Write tools + personal OAuth.** Opt-in write tools with per-action approval; "Each user signs
   in" mode, connect flow (inline in chat, plus user profile if it works), "not connected"
   handling, acting user per message, token cleanup on user delete. User-added connections per
   chat/project, the "Let users add their own connections" agent switch, the picker step, and the
   "not available to this agent" note. (User choice mostly matters once people have their own
   accounts, so phase 1 stays agent-only.) MCP writes blocked in contextual Copilot, with the
   hand-off message to Workspace.
3. **Sharing integration** (after share-conversations merges). Provenance-aware share warning,
   per-server sharing policy.
4. **Deploy** for the registry, once the credential-handling fix lands. Can move earlier if
   credentials stay out of Deploy files from day one.

## Open questions for the team

- [ ] Which two servers do we prove it with? Do they offer Streamable HTTP? Do they allow dynamic
      client registration, or will the admin need to paste a client ID and secret?
- [ ] Does the popup sign-in flow work from a user profile app, or is inline chat the only place
      editors can connect?
- [x] One connection type or several? One: remote MCP over HTTP (11-11-2026).
- [x] OAuth in phase 1? Yes, shared account; per-user in phase 2 (11-11-2026).
- [x] MCP SDK OAuth or Automate's OpenIddict? MCP SDK (11-11-2026).
- [x] Global servers, per agent, or per project? Per agent, plus user-added connections where the
      agent allows it (11-11-2026).
- [ ] Should "Let users add their own connections" default to No on a new agent? Proposed: No.
- [x] MCP writes in contextual Copilot? Blocked, with a hand-off to Workspace (11-11-2026).
- [ ] Does the registry live in `Umbraco.AI.Agent` (so Automate agents can use it too) or in
      Copilot Workspace? With the agent as the main gate, `Umbraco.AI.Agent` is the natural home.
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
- Automate OAuth: `Umbraco.Automate.OpenIddict` (`IOAuthCredentialsService`, `OAuthCallbackController`,
  `OAuthCredentialsCleanupJob`) in the Umbraco.Automate repo
- Claude connectors (owner enables, members connect):
  [Microsoft 365 connector](https://support.claude.com/en/articles/12542951-enabling-and-using-the-microsoft-365-connector),
  [Setting up integrations](https://support.claude.com/en/articles/10168395-setting-up-claude-integrations)
- ChatGPT apps (admin enables, role-based access):
  [Admin controls for apps](https://help.openai.com/en/articles/11509118-admin-controls-security-and-compliance-in-apps-connectors-enterprise-edu-and-business),
  [Feature controls and integrations](https://academy.openai.com/public/resources/feature-controls-and-integrations-with-your-tools)
- CMS user profile apps: `src/packages/user/current-user/user-profile-app.extension.ts` in
  Umbraco-CMS
