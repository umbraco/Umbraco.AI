# Research

> Source research behind this feature's decisions. Written up before `BRIEF.md`/`DECISION-LOG.md`
> existed, in response to the CTO asking how conversation sharing could work. The open questions at
> the bottom were resolved during the planning session that followed — see `BRIEF.md` for the
> answers (org-only, attachment redaction, "shared with me" list as v1 bar, snapshot-vs-live still
> TODO for design).

## What we have today

- `AIConversation` and `AIProject` both carry a single owning `UserKey`. Projects are
  explicitly documented in code as "private per user for MVP."
- Ownership is enforced as a repeated equality check (`UserKey == currentUser`) at the
  service and repository layers — there is no grants table, no roles, no public/link
  tokens, and no realtime/websocket infrastructure anywhere in the product.
- There is no "Artifact" concept (a distinct, shareable output separate from the raw chat)
  — only `AIMessage` (chat turns) and `AIAttachedResource` (input attachments) exist.
- **Implication:** any sharing model needs a new authorization layer. Read-only/shared-project
  models are additive (add a grants table, keep `UserKey` as owner). Live co-editing needs
  presence/sync infrastructure that doesn't exist. An artifacts model sidesteps the ownership
  problem but is new surface area built from nothing.

## What Claude's own products do (claude.ai / Desktop / Code)

- **Chat sharing is a read-only snapshot link**, not live. Messages sent after sharing don't
  appear until re-shared. No fork/continue option for the viewer. Revocable, no expiry.
  Free/Pro/Max can share publicly; Team/Enterprise can only share within the org.
- **Projects** (Team/Enterprise) can be set Public-within-org, Private, or (Enterprise beta)
  shared with a specific group — this is a separate, coarser mechanism from sharing one chat.
- **Artifacts are a distinct, separate sharing model** from the chat itself: publish an
  artifact to a public link (Pro/Max, single-shot — republishing after unpublish is blocked),
  or share org-internally with viewer/editor roles and commenting (Team/Enterprise).
  This is the cleanest existing precedent for treating "the output" as a separately
  shareable thing from "the conversation that produced it."
- No live multi-user co-editing of a chat exists anywhere in Anthropic's product line.
- Caveat: an unofficial report (Futurism, not Anthropic-sourced) found publicly-shared
  Claude chats being indexed/discoverable with sensitive content — a real risk to flag for
  any public-link design, not a confirmed Anthropic statement.

## What other AI chat products do

| Product | Read-only vs. continuable | Link vs. permission | Level | Live co-presence |
|---|---|---|---|---|
| ChatGPT (consumer) | Read-only snapshot | Public/unlisted link | Conversation | No |
| ChatGPT Projects (Business/Enterprise) | Teammates can branch into their own copy, not co-edit | In-app invite to Project | Shared workspace of files + chats | No |
| Perplexity Spaces | Collaborators add/continue threads; viewers read-only | In-app invite or link, viewer/collaborator roles | Shared workspace | No |
| Notion AI | Read-only link only | Public/unlisted link | Conversation | No |
| GitHub Copilot Chat | Read-only snapshot | Public link (preview) | Conversation | No |
| GitHub Copilot Spaces | N/A — shared context bundle, not a chat log | In-app team share | Shared context, not conversation | No |
| Google Gemini | Read-only link | Link; org-admin can gate sharing entirely | Conversation + separate Gem sharing | No |
| Microsoft Copilot Pages | **Live co-editing** — but of a derived canvas, not the raw chat | In-app link via Teams/Outlook | Output-level artifact | **Yes** |
| Replit Agent | Full live multiplayer (cursors, parallel agent tasks) | In-app invite, viewer/collaborator roles | Whole workspace, not one chat thread | **Yes** |
| Cursor | Requested, not shipped | — | — | No |

**Pattern:** almost nobody does live-multiplayer chat. Two models dominate:
1. **Read-only link export** of a conversation (ChatGPT, Notion, Gemini, GitHub Copilot Chat, Claude).
2. **Shared project/workspace with async branching** (ChatGPT Projects, Perplexity Spaces, GitHub Copilot Spaces) — a team folder holding multiple people's chats; others view or fork a thread, never co-edit the same live turn.

Real-time co-presence only ever shows up where the shared surface is a **derived artifact/canvas**
(Microsoft Copilot Pages) or the **whole workspace** (Replit) — never the raw chat transcript.

## What other CMS/SaaS backoffice tools do (as distinct from consumer chat apps)

These are closer analogues to Umbraco than the pure chat products above, since multiple
editors already share one CMS/SaaS instance.

- **Sitecore Stream** — review-stage "multiplayer collaboration," plus a separate app for
  sharing approved content externally. No raw-conversation sharing confirmed.
- **Adobe AEM / Firefly Assistant** — no conversation sharing found; personal/ephemeral.
- **Optimizely Opal** — ships "Team Messages": real-time group chats/channels where humans
  *and* AI agents share the same live conversation, with @mentions. A genuine live shared
  human+AI chat, shipped.
- **Contentful** — collaboration exists at the content-entry level; nothing AI-chat-specific.
- **HubSpot Breeze** — ships "Artifacts": AI-drafted outputs shared with a person, a team, or
  the whole company; prompts are also shareable. The raw chat itself is never shared, only
  outputs and reusable prompts.
- **WordPress/Jetpack AI Assistant**, **Webflow AI Assistant** — strictly per-editor, no
  sharing of the AI conversation. Any collaboration (comments, review links) is a separate,
  content-level feature.
- **Salesforce Agentforce/Einstein Copilot**, **ServiceNow Now Assist**, **Zendesk AI** —
  never share the raw AI chat. Handoff happens by writing a summary into the ticket/case
  record for the next human agent — the record is shared, not the chat.
- **Microsoft Dynamics 365 / M365 Copilot** — strongest counter-example: "Teams Mode"
  converts a private Copilot conversation into a live group chat inside Microsoft Teams,
  a shipped multi-user + AI live conversation. Separately, Microsoft also has plain
  read-only "Share chat" links, and Copilot Pages for live co-editing of a derived canvas.

**Synthesis:** CMS/SaaS backoffice tools split into two camps. Support/CRM tools (Salesforce,
ServiceNow, Zendesk) treat the AI chat as a personal scratchpad and hand off *the ticket*,
never the transcript. Content-editor tools (AEM, Webflow, WordPress) keep AI chat strictly
per-user and layer any real collaboration onto the content itself. But two products —
**Optimizely Opal and Microsoft Teams Mode** — do ship genuine live group human+AI
conversations, which revises the earlier "nobody does live co-chat" finding: it does exist,
but always as an *opt-in addition* to normal private chat, never a replacement for it, and
it's newer/less proven than the read-only or output-sharing patterns.

## Mapping to the four options considered

1. **Realtime shared chat (co-comment/co-edit live)** — rare but not unheard of: Optimizely
   Opal and Microsoft Teams Mode both ship it, always as an opt-in extra alongside normal
   private chat, never as the default. Claude has no version of this. Would require building
   presence/sync infrastructure we don't have. Highest cost, weakest/newest precedent.
2. **Share a conversation read-only** — the industry-standard baseline (ChatGPT, Claude,
   Notion, Gemini, GitHub all do this). Cheapest to build on our current model: add a
   grants/share-token concept, keep the rest of the architecture as-is.
3. **Shared project (continue/view others' convos)** — the second most common pattern
   (ChatGPT Projects, Perplexity Spaces, GitHub Copilot Spaces). Fits naturally on top of
   our existing `AIProject` grouping concept, but "private per user" on `AIProject` would
   need to become a real membership model, which is more work than option 2.
4. **No shared convos, share artifacts instead** — mirrors Claude's own Artifacts feature,
   the cleanest conceptual precedent for separating "the output" from "the conversation."
   But we have no Artifact concept at all today — this is new surface area, not an
   extension of anything that exists.

## Recommendation (as given, before design)

Start with **option 2 (read-only conversation share)** — it matches what nearly every
competitor ships as their baseline, requires the smallest change to our current ownership
model (additive grants/token, no new realtime infrastructure), and gives the CTO something
concrete this quarter. Treat **option 3 (shared projects)** as the natural next step once
teams ask to collaborate on an ongoing body of work rather than one conversation — it's a
bigger data-model change (projects need real membership, not just an owner) but builds on
the same grants concept. Hold off on **option 1 (live co-chat)** — it exists (Optimizely,
Microsoft) but only as a newer, opt-in extra bolted onto private chat, nowhere near a proven
default, and it needs infrastructure we don't have. **Option 4 (artifacts)** is worth tracking
as a longer-term idea inspired by Claude's own product, but it's a separate feature from
"sharing conversations" and shouldn't be conflated with this ask.

This matches what `BRIEF.md` landed on: read-only link share, org-only, plus a
"shared with me" list.

## Open questions raised by this research (now resolved — see `BRIEF.md`)

- Public link (like Claude/ChatGPT) or org-only (like Claude Team/Enterprise)? →
  **Decided: org-only**, no public/anonymous link.
- Does "read-only" need to include attachments/context resources, or just the message
  transcript? → **Decided: attachments included, but redacted per-viewer permission** —
  the harder problem this research didn't anticipate.
- Is there appetite for a later "shared project" phase, or is one-off conversation sharing
  the actual need? → **Deferred**, not ruled out; v1 is conversation-level only.
