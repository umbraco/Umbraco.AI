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
  `Umbraco.AI.IncludePersonalContext`. Rejected: Agent package only (prompts couldn't use it).
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
- **"Include Personal Context", not "Use personal context"** (user's call, 26-11-2026). Matches
  the prompt editor's existing "Include Entity Context" in verb and title casing. The property is
  `IncludePersonalContext` and the runtime key `Umbraco.AI.IncludePersonalContext`, mirroring
  `IncludeEntityContext`.
- **Memory tools keep their names and stay provider-supplied** (user's call, 26-11-2026). They
  don't follow the context lookup tools' `verb_noun` names (`get_context_resource`), since
  `remember` and `forget` read more naturally. They aren't system tools, which are offered on
  every agent and prompt run. Their arguments match the system tools' style: typed records with
  `[Description]`. Rejected: `save_memory` / `forget_memory`, and a new conditional system tool
  kind in core.
- **Personal context gets a site-wide kill switch** (user's call, 26-11-2026). Replaces spec
  guess 9 (per-prompt and per-agent toggles only). A built-in "Personal Context" toggle in AI
  Settings, on by default, stops every prompt and agent loading it and keeps stored data, like
  the memory switch. Gives admins a one-click stop for governance or cost. Rejected: off by
  default (hides the feature until an admin finds it).
- **Every open guess agreed** (user's call, 26-11-2026). The brief's 10, architecture's 8,
  spec's 9, stories' 2 and plan's 2 guesses stand as written. Each file keeps its list in a
  collapsed "agreed" section, so body references like "(guess 4)" still resolve.
- **No per-agent personal resources in v1** (user's call, 26-11-2026). Personal context reaches
  every agent, so the long-term home for "you + this agent" learning is an `On-Demand` resource
  named for the agent: other agents see only its name. Rejected for now: a "Use with" setting
  per resource (all agents or picked ones), which adds an agent picker per resource and another
  visibility rule nobody has asked for yet. Kept as the follow-up in the memory tiers idea.
- **Memory and personal context settings move with Deploy** (user's call, 26-11-2026). Replaces
  MEM-01's "out of scope: moving settings with Deploy" and fixes the architecture's wrong claim
  that core Deploy doesn't move settings (it has `AISettingsArtifact`, which already carries the
  disclosure notice mode). Core's settings artifact gains `PersonalContextEnabled` and an
  `Additional` map for every registered add-on setting, so add-ons need no connector of their
  own; import skips keys the target doesn't know or values of the wrong type, and a missing
  value keeps the target's, matching `DisclosureNoticeMode`. Agent config fields already travel;
  Agent.Deploy only gains specs. Rejected: a per-definition "deployable" flag (no setting needs
  to stay put yet) and an Agent.Deploy settings connector (duplicates core's).
- **Docs are part of the plan** (user's call, 26-11-2026). Three tasks after the smoke test:
  v18 screenshots (8 new, up to 3 re-taken), v18 pages (4 new, about a dozen updated) as a
  draft UmbracoDocs PR held until release, then the v17 copy with v17 screenshots after the
  port. Screenshots come after the smoke test so they show the finished screens, not mockups.
- **Personal details bound for a shared space are held by the Decision check** (user's call,
  27-11-2026). The brief promised the agent asks before saving something personal in a shared
  space, but only the memory rules text said so: no rule, story or test. The Decision question
  gains "a personal detail", held as `personal-in-shared` only for projects and shared chats;
  holding is the asking, since someone must click Keep. Without a Decision profile the rules
  text is the only guard. Rejected: a fixed-rules check for personal details (names and life
  events don't have reliable shapes). Layer choice and merging stay with the agent's model,
  which has the whole chat; a second model call per save isn't worth it there.

## Plan interrogation (27-11-2026)

Seven single-lens reviews (assumptions, edge cases, order, YAGNI, testability, security, blast
radius), about 80 raw findings merged into 31, each put to a skeptic against the code. 7
refuted, 24 survived (11 significant, 13 minor, none blocking). Decisions are the user's.

**Accepted, for `feature-design` (SPEC.md / ARCHITECTURE.md):**

- **F01 Frozen block goes stale** (significant). Store what the frozen list was built for
  (space type and key, agent id, effective mode, site switch, policy, personal switch) with it in
  session state; when any differs on a later turn, rebuild it, and drop deleted ids from it. One
  cache miss on change. Amends R16.
- **F02 Outside content beyond tool results** (significant). Corrected mechanism: the sidebar's
  open page arrives as a system message part (`SerializedEntityContributor`), Workspace resources
  as `AdditionalResources`, attachments in the user message; none are tool results. Treat all of
  them as content for the echo and after-content rules, and judge only this turn (messages after
  the last user message), since `CurrentContext.Messages` carries the whole history.
- **F03 Recipients see beyond their share** (significant). Reading a chat's memory needs an
  active (not revoked) share; Snapshot shares see no memory. The ever-shared test (T21) stays
  for choosing the space only. Amends R12.
- **F13 Inline agents and test runs** (significant). `AIInlineAgentBuilder` defaults
  `IncludePersonalContext` to false; agent and prompt test runs never load personal context
  (`PersonalContextResolver` honours `ContextIdsOverride` like the profile and agent resolvers).
- **F14 Automate has a user** (significant). Corrected mechanism: Automate sets a workspace
  service account as the backoffice user per action. Gate memory and personal context on an
  explicit "interactive run" marker (or an Automate run marker that turns both off), not on the
  presence of a user. Amends R2, R27.
- **F15 "Your AI" can't read site settings** (significant). `GET /mine` also returns the site
  memory switch, the personal memory policy and a per-agent memory-off flag, like
  `GetDisclosureSettingsController`.
- **F16 Users are rarely deleted** (significant). Umbraco refuses to delete a user with login
  history (`CannotDeleteUserWithLoginHistory`). Add an admin "Erase AI memory and personal
  context" user entity action in Users; keep the delete handlers for code-level deletes. GDPR
  erasure goes through the action.
- **F17 Workspace startup order** (significant). Workspace's composer adds
  `[ComposeAfter(typeof(UmbracoAIAgentComposer))]`, or `InsertBefore` throws.
- **F25 Ports** (significant). Ships to v18, v17 and v19, through the normal port workflow, with
  identical migration ids (`UmbracoAI_AddPersonalContext`, `UmbracoAIAgent_AddMemory`,
  `UmbracoAIPrompt_AddIncludePersonalContext`) on every line.
- **F09 Settings cache on load-balanced sites** (minor, user asked to address). `AISettingsService`
  caches settings with no cache refresher, so switches stay stale on other servers. Add a
  distributed cache refresher for AI settings in core, fired on save (and Deploy import).
- Minor, folded in: **F04** name the resolver that answers access for `user` spaces, and the
  Workspace spaces also require the Copilot Workspace section; **F05** `forget` gets an Undo on
  its note (soft delete for 7 days, like replaced rows); **F07** held rows don't count toward caps,
  Keep checks the cap; **F08** the "Your AI" box sends one save at a time; **F10** settings reads
  keep only registered add-on keys and saves leave unregistered rows alone instead of rejecting;
  **F11** new members on `IAISettingsService` and `IAIConversationShareService` ship with default
  interface bodies; **F12** fix the stated order (or have Agent/Prompt insert their resolvers
  before `AdditionalResourcesContextResolver` and `PersonalContextResolver`); **F24** move applies
  remember's layer rules, refuses held memories, and clears cross-layer replace links;
  **F28** `uai-context-picker` needs a compact mode (no such property today).

**Accepted, for `feature-plan` (PLAN.md / STORIES.md):**

- **F19** T27 also raises Agent.UI's `Umbraco.AI.Agent` floor.
- **F21** Name each screen check in its task's acceptance (T16, T17, T18, T19, T37, T40);
  repository specs on the SQLite `EFCoreTestFixture` (drop "SQL Server" from T4); a real-DB spec
  for the revoked-share query (T21); fix PersonalContextResolver AC5's setup (profile context or
  fake resolver); live delete checks for T24 and T32 handlers; a criterion and data source for
  "reopened later" chat notes (T20).
- **F18** (minor) T29's live check moves after T12; T22 defines the project-id runtime key T23
  sets; T33 depends on T43; client regen after T43 and T36; T20 depends on T18; split shared
  registration and `lang/en.ts` edits out of parallel groups; T9 adds bulk delete by space,
  user and agent for T13 and T24; T27 depends on T3, T13, T17, T19, T20, T32; T16 depends on T2.
- **F20** (minor) add `settings/`, `personal-context/` and Agent `memory/` exports to the root
  `exports.ts` chain in T2, T35, T18.
- Plus tasks for F01-F03, F09, F13-F17 once `feature-design` has folded them in.

**Rejected:**

- **F06 Delete restores replaced memories** (survived, minor). Kept: plan guess 1 was agreed
  with "the lists alike" in view. Add the cap re-check on restore only if `feature-design`
  touches R22 anyway.
- Refuted by the skeptics: F22 (settings PUT default: keep the `= true` initializer), F23
  (purge on read is the agreed guess 5), F26 (Saving notification is the house convention),
  F27 (no add-on setting holds ids or secrets yet), F29 (renderers keyed by tool name is the
  existing convention), F30 (internal IAITool classes outside the collection work), F31 (the
  Decision profile is opt-in and already sees chat content; a docs line in T47 is enough).
- Not put to the skeptics: the YAGNI review's points against the settings extension, the
  switch layering, `uaiUserProfileProperty`, the save-check collection, `LastConfirmedAt` and
  short ids. Each was an earlier user call in this log.

## Design folded in from the interrogation (27-11-2026)

- **Unattended runs get a marker, not a user check.** New `Constants.ContextKeys.UnattendedRun`,
  set by Automate's `RunAgentAction` and the agent and prompt test features. `ContextIdsOverride`
  was rejected as the marker because inline agents set it too. Amends R2, R27.
- **Frozen block keeps a fingerprint.** Space, agent, effective mode and the three switches are
  stored with the list; a mismatch rebuilds it, and dead ids drop out every turn (R16a).
- **Erase lives in core, each package erases on a notification.** Core can't know what other
  packages store; a notification plus a `uaiUserDataEraseItem` manifest keeps both the delete and
  the dialog wording in step with what's installed. Rejected: a core hard-coded list.
- **Resolver order:** Agent and Prompt insert their resolvers before `PersonalContextResolver`
  instead of appending, giving Profile, Content, KnowledgeSet, Agent/Prompt, Personal,
  AdditionalResources. One cache miss on upgrade.
- **`Forgotten` status** for `forget`, purged after 7 days like replaced rows, with a restore
  route. Caps count active rows only.
- **Recipients read memory only through an active Live share** (R12). The ever-shared test stays
  for picking the space.
- **Settings get a distributed cache refresher**; memories and personal context still don't.
- **`GET /{id}`** added so a reopened chat note can show the memory's current state.
- **F06 restore cap check:** R22 was touched anyway, so merge-undo now checks the cap too.

## Plan updated from the interrogation (27-11-2026)

- **Two new stories:** MEM-23 (moves follow the save rules) and MEM-24 (admin erase). 62 new
  criteria in all; amended in place, same ids: MEM-04 AC8, MEM-07 AC2, MEM-08 AC2, MEM-15 AC6,
  MEM-19 AC5.
- **Shared files come off parallel groups.** T30 now waits for T1 (core registration file); T16,
  T17, T18 run in order (Agent `lang/en.ts`); T26 waits for T25 (Workspace `lang/en.ts`). T6 and
  T7 keep group B by putting their builder extensions in separate partial files.
- **T29's live Jev check moves into T12**, the first task where memory runs at all.
- **T9 gains bulk deletes** by space, user and agent, because the repository is internal and
  T13, T24 and T56 live outside the service.
- **The admin erase is five tasks** (T53-T57): core endpoint, wire, core action, Agent handler and
  label, then a live erase. The Agent half waits for core's manifest type.
- **Unattended runs are three tasks**, one per product (Agent test feature, Prompt test feature,
  Automate), so each lands with its own product's build.
- **Ports are tasks now** (T58 v17, T59 v19) through `change-backport`, after the smoke test.
  The v17 docs (T48) wait for the v17 port.
- **ARCHITECTURE fix:** `AIInlineAgentBuilder` lives in the Agent package, not core; the file tree
  line moved.

