# Decision log: Copilot as MCP client

Newest first. The current model lives in [FEASIBILITY.md](FEASIBILITY.md); this file records
when and why each call was made.

## 11-11-2026: Review of the feasibility study

Six single-lens reviewers (hidden assumptions, failure cases, phasing, over-building, security,
internal consistency) raised about 50 findings, grouped into 33 issues. Each was checked by a
skeptic against the code, the MCP C# SDK source (v2.4.0) and the MCP spec. 15 survived, plus a
batch of contradictions. All survivors were accepted and folded into FEASIBILITY.md.

### Decisions taken during triage

- **Run the OAuth login ourselves; the MCP library uses and refreshes the tokens.** The SDK keeps
  the PKCE verifier and state in local variables and does the code exchange privately
  (`ClientOAuthProvider` is internal), so a callback on another server or after a restart can't
  finish. Rejected: keeping the SDK flow and holding the request open while polling the database
  (breaks on restart or a slow login).
- **No write tools on shared-account servers, for now.** Every write would be logged in the
  outside system as the admin who signed in. Writes need personal sign-in. Consequence: Automate
  gets no MCP writes. Rejected: writes with approval; writes only with a confirmed service
  account.
- **Audit keeps call details only.** MCP results are left out of AI request snapshots, which every
  AI section user can read and which are kept 14 days (`AIAuditLogOptions` defaults). Rejected:
  full data for shared servers only; keeping everything as today.

### Accepted findings

- **Refresh race.** Many editors and web servers share one token; concurrent refreshes can make
  the service revoke it. The SDK only locks per client instance. Refresh runs under a database
  lock. Automate's `OAuthCredentialsService` has no lock.
- **Old token sent to a new host.** The SDK attaches a cached token on the first request with no
  check against the current URL. Token rows store URL and issuer; editing the URL forces
  reconnecting.
- **Secrets returned to the browser.** The Connections API returns decrypted settings and only
  masks them on screen. MCP secrets are write-only from day one. (Version snapshots already mask
  them; that part of the finding was wrong.)
- **Dependency floors.** `ModelContextProtocol` 2.4.0 needs `Microsoft.Extensions.AI.Abstractions`
  ≥ 10.11.0 and `Microsoft.Extensions.*` ≥ 10.0.14; part of the dependency sign-off.
- **Servers aren't tool scopes.** Scopes are a startup-time Core collection. Use an Agent-side
  server picker and resolve IDs in the Agent package. Resized S → M.
- **Auto pick can't filter with a selector step.** Selectors return one agent or nothing.
  Filtering moves to candidate selection; Workspace loads servers before picking. Resized S → M.
- **Tool source must cover every route.** Only AG-UI fills `additionalTools`; Automate and inline
  runs pass nothing. Approval policy and `AllowMultipleToolCalls` only cover registered tools.
- **Source labels don't reach the live chat.** The AG-UI tool result event carries only content.
  Needs a stream field. Resized S → M.
- **Read-only not enforced in phase 1.** Per-tool read/write flags move into phase 1; unflagged
  tools are treated as writes.
- **Agents deploy with dangling server references.** Agent Deploy copies scope IDs with no
  dependency. Agents refer to servers by alias; Agent Deploy flags missing servers, from phase 1.
- **Retries and cancel.** Only read tools are retried. Audit rows are written when a call starts.
- **Data leaking out through read tools.** Recorded as a risk; admins see a warning on read tools
  taking free text or URLs.
- **Contradictions.** Verdict, collaboration section, sizes, terms and phase list rewritten to
  match the 11-11 decisions. Open questions moved to one list at the top.

### Rejected findings (one line each)

- Phase 2 needs more migrations than the add-on list covers: they are new nullable columns, no
  data moves.
- Acting user is needed in phase 1: in phase 1 the sender is always the owner.
- Open questions should be phase gates: they are listed as questions already.
- Legal review is tied only to phase 2: it was already a standing open question (now question 4).
- Approvals resumed after settings change: each run rebuilds the tool list, so removed tools
  don't run.
- Renamed tools in old history: same as any tool leaving an agent today; covered by "old
  conversations".
- Admin permission is just AI section access: that is the existing admin boundary for the whole
  AI section.
- SDK doesn't persist client registration: `TokenContainer` stores the client ID and secret since
  2.3.0.
- Over-building (per-server groups duplicate agent overrides, user-added servers, seven layers,
  write design, single-option mode, project level, two connect places): each was an explicit
  decision with no unseen cost shown.

### Handled separately

One finding concerned existing shipped code rather than this plan. It is being handled through
private disclosure and is not described here.

## 11-11-2026: Model decisions

- **One kind of server:** remote MCP over Streamable HTTP. No per-service code.
- **OAuth in phase 1** (shared account); personal sign-in in phase 2.
- **MCP SDK, not Automate's OpenIddict package**, for discovery, registration and token use.
- **User group access per server**, checked on the server.
- **Servers are per agent, not global.** "Always use these servers" plus "Let users add their own
  connections" (default No). Users turn servers on per chat or project.
- **Auto pick respects user servers**, and the chat says when a server can't be used.
- **MCP writes blocked in the sidebar**, pointing to Workspace. Rejected for now: a per-server
  opt-in with approval.
