# Decision log

## 26-11-2026
- **Outside projects, memory is per user and per agent.** Fits the keynote pitch ("a specialized
  agent that learns") without one user's notes reaching another. Per agent across all users was
  rejected for v1: leaks and poisoned saves would hit the whole team; admin-approved shared
  memory could come later.
- **Design for collaborative sharing.** Shared conversations will let participants reply, and
  projects will become shareable, so every shared memory scope must handle several writers.
- **Layered memory.** Each turn reads "about you" (personal preferences, across agents) plus
  the space the chat sits in (you + this agent, shared chat, or project). Replaces the earlier
  rule that shared spaces never read personal memory: preferences are harmless to apply on
  your own turns, and work facts still stay in their space.
- **The agent picks where a save goes,** so the user isn't asked every time. Asked for
  directly: "decide where the memory goes without asking the user all the time".
- **Company layer deferred, but planned for.** Site-wide learned knowledge would reach every
  user, so it needs admin approval of proposed memories. Not in v1; the layer model must leave
  room for it.
- **Memory lives in the Agent package** (user's call), so the Copilot sidebar gets it too.
  Workspace adds project and shared-chat spaces through a resolver hook. Rejected: Workspace
  only (drops the sidebar), a new Agent.Memory package (one more package, and the editor toggle
  still needs an Agent change).
- **Build on MAF's `AIContextProvider`** (user's call), but it must not hurt prompt caching:
  memory goes in the stable head and only changes when memory changes; anything that changes
  per turn goes in the volatile tail. Placement proven by spike before planning.
- **Save silently, hold only suspect saves** (user's call). Asking on every shared-space save
  was rejected as too many interruptions. Like Claude's auto-save: a cheap check flags a save
  that looks like it came from content rather than the person, and only that one waits for
  the user. This refines the brief's guess 6.
- **No ready-made memory provider reused.** MAF's `ChatHistoryMemoryProvider` stores whole
  messages in a vector store, `FoundryMemoryProvider` is Azure-only preview, `FileMemoryProvider`
  is a file model. See RESEARCH.md.
- **Memory block via `AdditionalResources`, changes via `PendingSystemMessage`.** Spike showed
  `AIContext.Instructions` merges into the agent instructions and breaks the duplicate check
  when it changes, and `AIContext.Messages` are saved into history mid-list. Both rejected.
- **Memory tools are plain functions from the provider,** not `[AITool]` classes. Spike showed
  provider tools run through our function-invoking middleware without being in the allowed
  list. Keeps them out of the tool picker.
- **Frozen block per Workspace chat.** MAF session state is saved with Workspace chats, so
  the injected list is kept and reused; the sidebar keeps no session state and rebuilds (one
  cache miss after a save).
- **Merge while saving in v1; tidy-up pass later** (user's call). `remember` can replace
  several memories with one, using what the agent already sees in its prompt; Undo restores
  them. A background consolidation pass (LangMem-style) was deferred: it changes memories
  silently and could blend a held or poisoned memory into clean ones. If built, it should be a
  "Tidy up" suggestion a person accepts.
- **Lessons in v1** (user's call). LangMem-style episodic memory, stored as `Kind = Lesson` in
  the space layer, saved when the user confirms a result worked. It's the "agent that learns"
  part of the keynote story.
- **Expiry dates in v1** (user's call), from Mem0's expiration idea.
- **Fading deferred, door kept open** (user's call). "Fade the least recently confirmed when
  full" was preferred over time-based fading, which would drop lasting preferences. v1 stores
  `LastConfirmedAt` so it can be added without a migration. Fading by "last used" isn't
  possible: every memory is loaded every turn.
- **No "expired" status** (user's call). Expiry is read from `ExpiresAt`; an expired memory is
  treated as deleted, doesn't count against the cap, and is cleared when its layer needs room
  or its list is read. A 30-day "expired but visible" grace period was dropped as not worth a
  state.
- **3-way agent memory mode** (user's call): Off, Work (no "about you"), Work and person.
  Keeps personal details out of agents that write content or take actions.
- **Site-wide switch on AI Settings, via an extensible settings screen** (user's call). Core
  gains add-on setting definitions and a `uaiSettingsPanel` manifest that adds a box to the
  existing Settings view; tabs stay possible with a `workspaceView`. A single switch doesn't
  earn a tab. Rejected: a separate Agent settings screen (splits settings), a core field like
  the disclosure notice (core would know about an Agent feature).
- **AI Settings box holds a kill switch and a personal-memory policy** (user's call, 26-11-2026,
  review comment c8). The policy (Allowed, Opt-in, Not allowed) was the main intent; the kill
  switch stays as a quick way to stop all memory regardless of agent settings. Rejected: the
  kill switch alone (no site control over personal data), the policy alone (no one-click stop).
  The personal switch is stored only once flipped, so untouched users follow the policy.
- **Build order: core settings, then Agent, then Workspace** (feature-plan, 26-11-2026). The
  Agent settings panel needs the core extension, and Workspace plugs into the Agent space hook,
  so neither can start first. Core settings, the memory table, `MemoryMode` and the share
  service have no ties to each other and run in parallel.
- **Two wire tasks for the provider** (T12 sidebar, T23 Workspace). The sidebar path proves the
  provider alone; Workspace needs the resolver and runtime context ids, a separate failure.
- **Undo and Discard reuse `DELETE`** (plan guess 1 and 2). Fewer routes; Undo's restore of
  replaced memories (R22) lives in the service's delete.
- **Suspect checks are pluggable, with a Decision check** (user's call, 26-11-2026). Replaces
  "rules only, no extra AI call". The check uses the default Decision profile the same way auto
  agent selection does, so a site can point it at TypeSafe's Jev without the Agent package
  knowing about TypeSafe. Rejected: a TypeSafe-only check (ties the hook to one provider) and a
  separate memory profile setting (not needed yet; the default profile is enough to start).
- **Hooks use Umbraco collection builders** (user's call, 26-11-2026). Space resolvers and save
  checks are ordered (`OrderedCollectionBuilderBase`, like `AIAgentSelectors()`), so packages
  place theirs with `InsertBefore`/`Append`. Settings providers are unordered
  (`LazyCollectionBuilderBase`, like `AIGuardrailEvaluators()`).
- **Short ids are just long enough, and ambiguous ids are refused** (user's call, 26-11-2026).
  Replaces architecture guess 7 (a fixed 8-character prefix), which left a small chance of the
  agent forgetting or merging the wrong memory. Rejected: per-run numbers like m1 (shift between
  sidebar runs, so older turns point at the wrong memory) and a stored short-code column (only
  works for tables that add it). Recorded as a project decision so other tools adopt it later.
- **Personal context joins phase 1 as the long-term tier** (user's call, 26-11-2026). Users pick
  their own AI Contexts and resources in a "Your AI" profile box, each loaded Always or
  On-Demand, the same parts a Workspace project uses. Memories stay the short-term tier. This
  answers how memories move to long-term storage: the user decides, instead of a usage measure
  nobody can take (the model reads every loaded memory, so nothing shows which one it used).
  Rejected: a `recall` tool with promotion and demotion by use, and the search-before-each-turn
  approach (kept in `docs/ideas/agent-memory-tiers.md` for later).
- **Personal context lives in core, behind a caller flag** (user's call, 26-11-2026). Prompts
  use it as well as agents, so it's a core context resolver, loaded only when the run sets
  `Umbraco.AI.UsePersonalContext`. Rejected: Agent package only (prompts couldn't use it).
- **Builders choose per prompt and per agent; agents default on, prompts off** (user's call,
  26-11-2026). Prompts often have a fixed output format that one user's preferences would break;
  agents are conversations where it helps, but builders of content-writing agents can switch it
  off. Rejected: on for both (existing prompts change output after upgrade), prompts only.
- **The memory switch moves into the "Your AI" box** (user's call, 26-11-2026). One box holds
  contexts, resources and the switch; the memory table sits below it. Core owns the box and
  adds a `uaiUserProfileProperty` slot so the Agent package can add its switch.
- **"Turn into resource" left out of v1** (user's call, 26-11-2026). Users write the resource
  and delete the memories themselves. A bulk action can come later.
- **Company memory not planned** (user's call, 26-11-2026). AI Contexts already carry
  company-wide facts; what's missing is a way for users to propose additions. Wait until someone
  asks.
