# Decision Log

- **2026-09-22** — Read-only shared link over live multi-user co-editing. Building on top of the
  Copilot Workspace conversation model (single-owner `UserKey`, no ACL), full collaborative editing
  (two people writing into one conversation) would need new concurrency/locking work; a read-only
  link reuses the existing owner-only write path unchanged. Killed: live shared chat editing.
- **2026-09-22** — Internal-only link (logged-in backoffice users), not a public/anonymous link.
  Smaller privacy surface, reuses backoffice auth as-is. Killed: unauthenticated external link.
- **2026-09-22** — Shared attachments must be redacted per the *viewer's* own permissions, not
  shown verbatim as the owner saw them. Chosen over the simpler "show everything" option despite
  the extra work, given the conversation may carry pages/files the recipient isn't cleared to see.
- **2026-09-22** — v1 scope is link **+** a "shared with me" list, not link-only. Killed:
  link-only MVP (deferring the list to a later version).
- **2026-09-22** — Sharing is revocable at any time by the owner; access is cut off immediately
  once turned off. This is also the privacy kill-switch for the "kill criteria" scenario in
  BRIEF.md.
- **2026-09-22** — v1 is strictly read-only for the recipient; no "save a copy and keep chatting"
  fork action. Deferred, not ruled out.
- **2026-09-22** — Manual refresh is sufficient for the recipient; no live/streaming updates
  required while they have the shared conversation open. Killed: real-time viewer sync.
- **2026-09-22** — Owner chooses, at share time, whether the share is frozen at the current message
  or continues to include future messages. Mechanism for enforcing the cutoff is an open TODO for
  `feature-design`, not decided here.
- **2026-09-22** — Wait for the Copilot RC to finalize and merge into `v18/dev`/`v17/dev` before
  starting build work, rather than branching off the still-moving RC release line now. Avoids
  building on a branch that could still change and extra RC-merge conflicts.
- **2026-09-22** — Ships for v18 and v17 at the same time, per the repo's keep-active-versions-in-sync
  policy. Killed: v18-only first with a deferred v17 port decision.

## feature-design

- **2026-09-22** — Recipients are named (owner picks specific colleagues), not a single
  "anyone with the link" toggle. Confirmed with the user; makes `SharedWithUserKey` and the
  "shared with me" list well-defined. Killed: blanket org-wide link.
- **2026-09-22** — Shared reads go through a brand-new `GetSharedConversationAsync` path that never
  calls `GetOwnedOrThrowAsync` or any write method, instead of widening the existing
  `UserKey == actingUser` equality check to also accept an active share. Killed: widening the
  single ownership check — too easy for a future write path to inherit the "or shared" branch by
  accident.
- **2026-09-22** — Attachment redaction (`CanViewAsync`) is a new default-implemented
  (default: allow) member on `IAIContextResourceType`, not a bespoke Conversations-only check.
  Additive, no existing resource type needs to change. Killed: a Conversations-local permission
  check that would only apply to conversation attachments, not resource types generally.
- **2026-09-22** — Shares live in a new child table (`AIConversationShareEntity`), one row per
  (conversation, recipient), mirroring how `Resources` already got its own child table instead of
  a JSON column like `ContextIds`. Killed: a JSON list on `AIConversationEntity` — can't answer
  "every conversation shared with user X" without a full scan, and can't revoke one recipient
  without rewriting the whole blob.
- **2026-09-22** — Snapshot scope is a stored cutoff reference on the share row, not a copied
  snapshot of the message set. Killed: physically copying messages at share time — duplicates
  storage and attachments, and gives Live mode nowhere to live.
- **2026-09-22 (correction)** — The cutoff reference is `CutoffMessageId` (`AIMessage.Id`),
  resolved to that message's existing `Sequence` at read time, not a raw `CutoffMessageCount` int.
  User pointed out messages already have a stable id; `AIMessage` also already has an
  authoritative, server-assigned `Sequence` built for exactly this kind of ordering. Reuses both
  instead of recomputing a count. Also defined the fallback if the anchor message is later deleted
  (regenerate/truncate): show everything that still exists, never error.
- **2026-09-22** — Revocation is soft (`DateRevoked`), not a row delete — preserves an audit trail
  of who had access and when.
- **2026-09-22** — Confirmed the existing backoffice section `[Authorize]` policy on
  `ConversationsManagementControllerBase` stays as a precondition unchanged; the share check is
  additional, not a replacement. Resolves the open question flagged in `BRIEF.md`.
- **2026-09-22 (correction)** — The read-only chat view isn't new work: archived conversations
  already have one (`UaiConversationWorkspaceContext.isReadonly$`, wired to `<uai-chat readonly>`).
  User caught that the first pass of `SPEC.md` described this as a new `readonly` input to build.
  Corrected to extend the existing mechanism (`isReadonly$` also true for `isShared`, a second
  `#reload()` path via `requestSharedView`) instead. Net effect: less new frontend work than
  originally scoped, not a different design.
- **2026-09-22 (clarification)** — User asked directly: does attachment redaction hide the
  resource's *existence*, or just block access to its *details*? Checked the frontend: the resource
  picker only ever shows identity (name/type/description) as a chip; there's no "open a resource"
  action anywhere in chat. So there's no separate "details" surface to distinguish — redaction
  means the chip doesn't render at all for an unauthorized viewer, full stop, confirming the
  full-omission (not a placeholder) design already in `SPEC.md`, now with the reasoning made
  explicit in `ARCHITECTURE.md`.
- **2026-09-22 (finding, same investigation)** — The context panel does **not** consume the
  store's `isReadonly$` — its resource/context pickers check `conversation.isArchived` directly in
  four places. Extending only `isReadonly$` (as originally scoped) would have left a shared
  conversation's context panel showing working-looking, but silently-failing, "add a
  resource/context" controls. Fixed by introducing one derived `readonly` field on
  `UaiConversationDetailModel` (`isArchived || isShared`) that both the chat view and the context
  panel read, instead of patching `isArchived` checks individually — found by tracing the actual
  render code, not assumed.
- **2026-09-22 (reversal, supersedes the "hide what the viewer can't see" decision from
  `feature-explore` and the `CanViewAsync` decision above)** — Dropped attachment redaction entirely.
  User pushed back: message content is already unredacted, so a reply that leans on an attachment
  can already describe or quote it — hiding just the attachment chip protects a label, not the
  substance, while making such a reply look broken (references something invisible). Since the
  system can't meaningfully protect this content, replaced silent filtering with a disclosure
  warning shown to the *owner* at share time instead: sharing shows the recipient everything
  referenced, permissions or not. This is a real product/privacy call, not a pure engineering one —
  made with the user, not assumed. Removes the new `IAIContextResourceType.CanViewAsync` Core
  interface member and its two TODOs (which permission check to reuse; whether `AIContext` needs
  the same treatment) — both moot now that nothing is filtered.

## feature-plan

- **2026-09-22** — Owner-side stories (US1 share, US2 manage/revoke) sequenced before
  recipient-side (US3 find, US4 read) in `PLAN.md`. Confirmed with the user. The owner side has no
  dependency on the recipient side and is independently demonstrable (shares exist, can be listed
  and revoked) before anything renders for a recipient.
- **2026-09-22** — Backend service layer split into two parallel tracks (SC-04 owner-side,
  SC-05 recipient-side) rather than one combined sharing service. Mirrors `ARCHITECTURE.md`
  decision 1 directly: shared reads were already required to be a separate code path from owned
  reads/writes, so the task split follows the same seam rather than cutting across it.
- **2026-09-22** — Migration generation for SqlServer + SQLite (SC-03) is one task, not two,
  even though `feature-plan`'s general guidance favors small single-seam tasks. Both providers are
  mechanically generated from the same entity change in the same commit — splitting them wouldn't
  produce two independently reviewable units, just two files reviewed together anyway.
- **2026-09-22** — Frontend feature work (SC-10 readonly plumbing, SC-11 Share modal, SC-13
  sidebar grouping) is one parallel-group, not sequenced — confirmed they touch disjoint files
  (workspace context + context panel vs. a new modal vs. sidebar grouping code) once
  `conversation.repository.ts` (SC-09) exists as their shared dependency.
- **2026-09-22** — v17 is one backport task (SC-15) at the end, not a parallel task list next to
  the v18 checklist — matches `BRIEF.md`'s decision to build via the normal Backport Workflow
  (build on one line, port to the other) rather than developing both simultaneously.
- **2026-09-22** — `bdd-specs`-generated pending specs are staged under this plan folder's own
  `specs/` subfolder, not written directly into `Umbraco.AI.Agent.Copilot.Workspace`'s real test
  projects. That product doesn't exist on this branch at all yet (not even an empty folder) — there
  is nowhere real to put them until SC-01 onward creates the product structure. Each staged file's
  header names its real destination and which `PLAN.md` task moves it there. This is a deviation
  from `bdd-specs`'s default assumption (the target project already exists); the feature's
  blocked-on-external-merge status is the reason, not a change to the discipline itself.

## Post-plan addition

- **2026-09-22** — Added a share-notification email, discovered as a gap after `PLAN.md` was
  already written (the user asked whether any notification was expected — nothing in `BRIEF.md`
  through `PLAN.md` had addressed it). Checked the codebase and Umbraco core first: no
  in-app/toast notification system exists to hook into, but core already has a generic, reusable
  `SendEmailNotification` pipeline (same one behind forgot-password and content-change alerts).
  Confirmed with the user: email, not silence, and not a new in-app notification system (bigger
  lift, nothing to reuse). Updated `BRIEF.md` (new confirmed requirement), `ARCHITECTURE.md`
  (decision 5), `SPEC.md` (POST /shares behavior), `STORIES.md` (US1 AC6–7, old AC6 renumbered to
  AC8), and `PLAN.md` (new task SC-05, full renumber SC-05→SC-16 through the rest of the list —
  see each file for the current state, not this entry, per the "snapshot not changelog" rule
  memory `plan-doc-architecture-spec-are-snapshots.md` — applied here to `STORIES.md`/`PLAN.md`
  too, not just `ARCHITECTURE.md`/`SPEC.md`).
- **2026-09-22** — Email sends only when a share row is newly *created*, not on an idempotent
  re-share that only changes scope. Avoids emailing a recipient every time the owner adjusts
  Snapshot/Live on an existing share.

## feature-explore (resumed 2026-10-20, after a ~month gap)

- **2026-10-20** — Resumed after Copilot Workspace shipped to `v18/dev`/`v17/dev` for real,
  resolving the build blocker `BRIEF.md`/`PLAN.md` had been carrying since 2026-09-22.
- **2026-10-20** — Cut v1 scope down to just the read-only-snapshot core, confirmed with the user:
  owner shares a conversation (always a frozen snapshot, no Live option in v1) with named
  colleagues, it appears in their "shared with me" list, they read it read-only. **Deferred, not
  killed:** the share-notification email (added 2026-09-22, above), revoke/manage-shares (`US2`),
  and the Live "keep updating" scope choice. Flagged once (not re-litigated): revoke is cheap
  relative to what's being deferred alongside it — a single delete endpoint and a button, cheaper
  than either the email or the live-scope mechanism — so there's room to add it back into v1 later
  without much cost if there's appetite.
- **2026-10-20** — Corrected two stale docs found while resuming: `BRIEF.md`'s "Riskiest unknowns"
  still described the reversed redaction requirement (redaction was dropped on 2026-09-22, above,
  but that reversal never made it back into `BRIEF.md`); `RESEARCH.md` (written later than
  `BRIEF.md`, for the CTO) had the same stale claim in its "resolved" open-questions section. Both
  now describe the current design: attachments shown as-is, with a share-time disclosure warning.
- **2026-10-20** — Created `README.md`, the plan folder's read-order index — this plan predates
  that convention. `ARCHITECTURE.md`/`SPEC.md`/`STORIES.md`/`PLAN.md` are left marked pending in it
  even though they already have substantial content from before, since `feature-explore` only owns
  its own line — the upcoming `feature-design`/`feature-plan` passes (re-cutting these for the v1
  scope above) will correct their own lines when they run.
- **2026-10-20 (reversal, minutes later)** — Widened v1 back out: Live scope and revoke are back
  in, only the notification email stays deferred. Also added a new requirement that wasn't in any
  earlier pass: the owner needs an ambient, persistent indicator (not just something inside the
  Share dialog) that a conversation is currently shared and with whom. The earlier "revoke is
  cheap, flag once" assumption from this same session turned out to be moot within minutes — the
  user brought revoke back before that flag needed to do any work.
- **2026-10-20 (reversal again, same session)** — Notification is back in v1 too. Net effect after
  three scope passes in one sitting: v1 is now the full original feature (share with Snapshot/Live
  choice, email on first share, revoke, "shared with me" list) plus the one genuinely new
  requirement from this round — owner-side visibility of share status outside the Share dialog.
  Nothing is deferred to a later phase at this point.

## feature-design (resumed 2026-10-20)

- **2026-10-20** — Re-verified decisions 1–5 against the real `v18/dev` code (Copilot Workspace had
  only existed on pre-merge RC branches when they were written). No material drift —
  `AIConversation`/`AIConversationEntity`/`AIMessage`/`AIConversationResourceEntity`/
  `AIConversationService` all match on the members this design depends on. One unrelated addition
  found (`AIMessage.AgentId`, `GetLastAssistantAgentIdAsync` — agent auto-selection tracking) that
  doesn't touch anything here.
- **2026-10-20** — Designed the new owner-side "this is shared" requirement from `BRIEF.md`: reuse
  the tree item's existing single-icon slot (pushpin > share icon > plain chat icon), computed via
  a server-side `hasActiveShares` field added to the existing paged list query — not a new badge
  element, not a per-row extra request. Rejected a separate badge: the row has an established
  single-icon convention (pin today), and pin-vs-shared don't both need to be visible at once often
  enough to justify a second UI element.
- **2026-10-20** — Added `## Connected systems` to `ARCHITECTURE.md` (a section this feature's
  original write-up predates): checked Deploy/uSync, the AI operation audit trail, and notification
  events — none apply, each for a specific reason now recorded there rather than left as a silent
  omission.
- **2026-10-20 (process gap, flagged not hidden)** — `feature-design`'s own process calls for a
  static mockup + screenshot per new/changed screen before handing off. None were produced this
  pass, for either the new tree-item indicator or the Share modal/read-only view designed in the
  original (pre-`feature-design`-skill) session. Recorded as a known gap rather than silently
  skipped; worth doing before `feature-build` if screens should be visually approved first.
- **2026-10-20 (gap closed)** — User asked why the mockup step was skipped and asked for it to be
  done properly, plus project starters for both Copilot surfaces. Created
  `docs/mockups/starters/copilot-workspace.html` and `copilot-sidebar.html` (read from the real
  section-shell/sidebar source at commit `c6643b80c76a1025009838ff1d993e7c52901b27`), then three
  feature mockups built from the workspace starter: `sidebar-shared-indicator.html`,
  `share-modal.html`, `read-only-shared-conversation.html`. Each served locally and screenshotted
  at 1440×900 via the browser extension; `SPEC.md` now embeds each screenshot with its mockup
  source link. One real build quirk found while shooting these: the shared `_mockup.js` loader's
  `data-mockup-ready` flag sometimes wasn't set on a page's first load (a `swapIcons` timing issue,
  not specific to any one mockup) — a reload reliably fixed it each time; not a mockup-content bug,
  just something to know if a future mockup session hits the same blank-page symptom.
- **2026-10-20 (mockups verified against the real site)** — User asked to check the mockups against
  the actual backoffice. The local demo site (`demos/v18/Umbraco.AI.DemoSite`) didn't reference
  Copilot Workspace at all — only the contextual Copilot. Added a temporary `ProjectReference` to
  `Umbraco.AI.Agent.Copilot.Workspace.Startup`/`.Web.StaticAssets` in the demo site's `.csproj`
  (commented as temporary; `demos/` is gitignored, so this can't leak into git regardless of
  whether it's reverted). The section tab and shell chrome rendered once rebuilt, but the
  package's own frontend bundle never actually registered (`customElements.get('uai-copilot-workspace-shell')`
  stayed `false`, no init log line, despite the JS being servable) — a manifest-discovery issue
  in this one demo DB, not chased further since it's a side issue from the actual task. Still got
  real, confirmed values from what did render: header `#1b264f` / 60px tall, section-sidebar
  300px, `--uui-color-current` `#ffe8e6`, border `#d8d7d9` — all already matched the mockups,
  since both use the same real UUI theme. Two real corrections found and applied to all affected
  mockups: the section tab reads **"Copilot"**, not "Copilot Workspace"; and the sidebar's "New
  chat" is a split button (primary action + a chevron for more options) with a plain, icon-less
  "New project" button beside it, not two same-style buttons with icons. Re-screenshotted all four
  affected files (the `copilot-workspace` starter + the three feature mockups) after the fix.
- **2026-10-20 (second pass — user said mockups were still off)** — User flagged the tree, header
  sizes, user-bubble color (pink instead of blue), and modals not using `uui-box`. Got Copilot
  Workspace's own frontend actually mounting this time (the earlier manifest issue was just a slow
  first paint — reproduced it again after a restart and it resolved on its own after enough wait
  time). Confirmed against the real, running section (not just source reading):
  - **User message bubble was wrong — fixed.** Real CSS (`Umbraco.AI.Agent.UI`'s `message.element.ts`)
    uses `background: var(--uui-color-selected)` (`#283a97`, deep blue) with white text, as an
    inline-block `<p>` (hugs its content, not a fixed-width bubble), small default
    `border-radius`. The mockups had `--uui-color-current` (pale pink) in a larger, more rounded,
    fixed-width box. Also removed the assistant bubble's border — the real one has none.
  - **Context panel header was wrong — fixed.** Real header is exactly 70px tall, `0 40px 0 12px`
    padding (right side clears the collapse toggle), an actual `<h3>` — not a plain bold div with
    20px padding and no fixed height.
  - **Context panel content structure was wrong — fixed.** Real panel is two separate collapsible
    `<details>` blocks, "Contexts" and "Resources" (12px/700/uppercase summary, chevron, dashed
    "+ Add" button), not one flat "Attached" list. Read-only view now shows both sections correctly
    (an empty "No contexts attached" state, same real wording pattern as the component's own
    `#renderEmpty`).
  - **Share modal restructured into `uui-box` groups** — "Share with" (recipient search + scope +
    disclosure) and "Currently shared with" are now each their own `uui-box`, matching this
    skill's own `editor.html` template's "group" convention, instead of flat custom divs.
  - **Tree item sizing — not changed, and here's why.** `tree-item.element.ts` has zero custom
    CSS beyond `:host { display: block }` — it's a bare `<uui-menu-item>`, so there's nothing a
    mockup could get wrong beyond what UUI already renders by default, which the mockups already
    use unmodified. Confirmed `uui-menu-item`'s real metrics are unmodified defaults by checking
    the Content tree (same component, real data) after Copilot Workspace's own tree couldn't be
    populated (no AI connection configured on this demo site, so no conversation could actually be
    created/saved to populate it). If the tree still looks off after this round, it's worth a
    closer side-by-side rather than assumed fixed.
  - Demo site crashed/was killed mid-session three separate times for reasons unrelated to this
    feature (background process lifecycle in the sandboxed dev environment, not a bug in the
    product) — re-verify independently if picking this up again rather than trusting it stays up.
  - Re-screenshotted all four affected mockups again after these fixes.
- **2026-10-20 (third pass — headings still off, boxes too tight)** — Root cause for the headings:
  every `<h3>` I'd added (the Share modal's title, the context panel's "Context" header) had no
  explicit `font-size` at all, so it fell back to the raw browser default (much larger than real),
  while `uui-box`'s own `headline` attribute (a real loaded component) was always correctly sized —
  the inconsistency between the two is what read as "off." Fixed by setting `font-size: 16px;
  font-weight: 700` explicitly on both, matching the 16.38px measured live against the real
  context-panel header. Also tightened up spacing inside the Share modal's `uui-box`es: bumped
  gaps between its three fields, between the "currently shared with" rows, and added a proper
  description-to-input gap — nothing should read as touching now. Re-screenshotted all four
  affected mockups a third time to confirm visually rather than trust the CSS alone.
- **2026-10-20 (fourth pass — "Pinned"/"Recent"/"Shared with me" still huge)** — These had been
  12px via `var(--uui-type-small-size)` since the very first draft, and that variable genuinely
  does resolve to `12px` in the real theme — but the computed size on the actual element was 21px/
  weight 400 regardless. Traced it to a rule inside the CDN-loaded `@umbraco-ui/uui` theme CSS
  itself winning the cascade; couldn't even identify which rule, since cross-origin stylesheets
  can't be introspected via `document.styleSheets` (`sheet.cssRules` throws). Fixed by forcing
  `font-size`/`font-weight` with `!important` on `.cw-group-label` — confirmed by computed style
  (21px/400 → 12px/700) before re-screenshotting, not assumed from the CSS alone this time.
- **2026-10-20 (owner-side indicator → real entity-sign/flag)** — Replaced the icon-swap design
  (pin → share → chat, pick one) with Umbraco's own entity-sign mechanism
  (`core/entity-sign`/`UmbEntityFlag`, the same system behind core's own "has scheduled changes"
  badge on the document tree), per the user's own suggestion that this was a better fit. Checked
  directly against `@umbraco-cms/backoffice` source rather than assumed: a new `entitySign`
  manifest (`forEntityTypes: ['uai-conversation']`, `forEntityFlags:
  ['Uai.Conversation.IsShared']`) renders a small corner badge via `<umb-entity-sign-bundle>`
  wrapping the row's existing icon. Pinned and shared now show simultaneously, which the icon-swap
  design couldn't do. `ARCHITECTURE.md` decision 6 rewritten in place (old icon-swap text replaced,
  not narrated over); `mockups/sidebar-shared-indicator.html` rebuilt to show a pinned-and-shared
  row and a shared-only row, both badged.
- **2026-10-20 (Share UI split into a manage view + a picker modal)** — The single combined Share
  modal (recipient search + scope + disclosure + current-recipients list, all in one form) is
  replaced by two modals at the user's request: `UAI_MANAGE_SHARES_MODAL` (opened by the entity
  action — shows who it's shared with now, plus a "Share" button) and `UAI_SHARE_PICKER_MODAL`
  (opened from that button — the actual add-a-recipient form, mirroring the stacked-modal pattern
  `project-picker-modal.element.ts` already uses). Checked what core offers for the recipient
  picker itself: `UMB_USER_PICKER_MODAL` exists but is a full collection-picker (too heavy for one
  field in an already-open modal); the lighter `UmbUserPickerInputContext`/`user-input` component
  is the better fit, with no existing consumer in this repo yet. `ARCHITECTURE.md` decision 7 added
  (new, not a rewrite of an existing one); `SPEC.md`'s Share-action section restructured to match;
  old combined `mockups/share-modal.html`/`.png` deleted, replaced by `manage-shares-modal.html`
  (+ an `-empty` empty-state variant) and `share-picker-modal.html`.
- **2026-10-20 (new in-chat "Shared" section in the context panel)** — Added a way to see who a
  conversation is shared with from inside the chat itself, not only via the action menu, per the
  user's direct question. A new "Shared" collapsible section in
  `conversation-context-panel.element.ts`, parallel to the existing Contexts/Resources sections,
  shown first since who can see the conversation outranks what's attached to it. Owner's own
  conversations only — a recipient already sees who shared with *them* via the read-only banner,
  and the underlying `GET .../shares` data is owner-only regardless. `ARCHITECTURE.md` decision 8
  added; `SPEC.md` gains a matching section; shown in the same screenshot as the sidebar indicator
  (`mockups/sidebar-shared-indicator.html`) rather than a separate near-duplicate mockup file.
- **2026-10-20 (dropped the entity action entirely; "Shared with" is the only entry point)** — One
  round later, the user asked to go further: drop `UaiConversationShareAction` and its manage-view
  modal altogether, and make the context panel's new section work exactly like Contexts/Resources
  already do (a visible list with an inline add/remove, not a menu item plus modal). Checked the
  real `<uai-resource-list>` component before redesigning rather than guessing the pattern again
  (per [[feedback_design_verify_against_real_code]]): rows are `uui-ref-node`s with an inline
  `uui-action-bar` trash-icon remove button, "+ Add" opens a picker modal directly, no second modal
  layer. "Shared with" now mirrors this exactly — renamed from "Shared" per the user's request, the
  "Choose" button opens `UAI_SHARE_PICKER_MODAL` directly, and each row's own remove button calls
  `DELETE .../shares/{userKey}` immediately.
  Also replaced the multi-recipient chip picker with a single-recipient form: one person + one mode
  per invocation (repeatable), since that's what "the actual user" (singular) asked for and matches
  how adding one resource is already one picker invocation. Checked what's reusable for the person
  picker: `UMB_USER_PICKER_MODAL` from CMS core is the real single-user-pick primitive — no lighter
  wrapper or existing precedent anywhere in this codebase, so it's used directly with a max-of-one
  constraint rather than inventing a new component. Mode switched from a radio group to a
  `uui-select` dropdown, mirroring this codebase's own idiom for a small fixed-option choice
  (`agent-details-workspace-view.element.ts`'s output-format dropdown) instead of a different
  control shape for the same kind of thing.
  `ARCHITECTURE.md` decisions 7 and 8 merged into one rewritten decision 7 (the entity action and
  the manage-view modal no longer exist, so there's nothing left for a separate "entry point #2"
  decision to describe); `SPEC.md`'s Share-related sections rewritten to match. Old
  `mockups/manage-shares-modal.html`/`.png` and `-empty` variant deleted (no manage view exists
  anymore); `mockups/share-picker-modal.html` rebuilt as a single-recipient form with a dropdown;
  `mockups/sidebar-shared-indicator.html`'s context-panel section relabeled "Shared with" and
  rebuilt with removable `uui-ref-node` rows + a "Choose" button, matching the real Resources
  pattern instead of a plain `uui-ref-list`.
- **2026-10-20 (share-picker modal: padding + disclosure banner contrast)** — Two visual fixes,
  checked live rather than assumed. The box's fields sat flush against its top/bottom edges
  because `.group`'s default padding (`0 18px`, from `_mockup.css`) is built for stacked
  `.property` rows that supply their own vertical padding — this box has none, so it needed an
  explicit override. The disclosure banner was black text on `--uui-color-warning-standalone`,
  which computed live to `#a17700` (a dark mustard meant for solid buttons/tags), not a light
  banner background — hard to read. Switched to `--uui-color-warning-emphasis` (`#fbe065`, the
  lighter tint token), icon recolored to the darker standalone shade so it still reads against it.
- **2026-10-20 ("Shared with me" rows made to match "shared" rows — icon+badge, one line)** — The
  "Shared with me" group previously used a different row shape than the owner's own shared rows: a
  swapped `icon-share` main icon plus a permanent "Shared by \<owner\>" subtitle line. At the user's
  request, unified both to the identical shape — plain icon with the same entity-sign badge
  overlay, one line, no subtitle. The owner's name isn't lost, just relocated: it's now the badge's
  hover tooltip text ("Shared by \<owner\>" instead of plain "Shared"), and still shown in the
  read-only banner once the conversation is open. `SPEC.md`'s sidebar section rewritten to describe
  one shared row shape used by both groupings; `mockups/sidebar-shared-indicator.html` and
  `mockups/read-only-shared-conversation.html` both updated (the latter gained the entity-sign
  badge CSS it didn't have yet, since its own "Shared with me" row needed the same treatment).
- **2026-10-20 (share-notification email mockup, built from the real Umbraco template)** — Added a
  mockup for the first-share email (`ARCHITECTURE.md` decision 5), per the user's request to review
  what Umbraco's own emails actually look like first rather than invent a style. Checked
  `Umbraco.Cms`'s source directly: there's no Razor `.cshtml` email template and no shared email
  layout file — real HTML notification emails (`mailBodyHtml` in
  `src/Umbraco.Core/EmbeddedResources/Lang/en.xml`) are fully inline-styled literal-hex-color HTML
  strings, token-replaced and handed straight to `IEmailSender`, since mail clients strip
  `<style>`/CSS custom properties. Mirrored that exact shell: `#1d1333` dark header strip with the
  small logo mark, white 560px card with `#392F54` text, `#35C786` teal CTA button (Outlook-safe
  nested-table markup), plain sign-off footer, no unsubscribe/legal block (none exists in
  Umbraco's own templates either). New `mockups/share-notification-email.html`/`.png` — the first
  mockup in this plan folder that isn't a backoffice UUI screen, so it intentionally does not use
  the usual `_mockup.css`/`_mockup.js` harness.
- **2026-10-20 (`feature-plan`: full re-cut of STORIES.md and PLAN.md)** — Both files were still
  shaped like the first design pass (pre-entitySign, pre-single-recipient-picker,
  pre-entity-action-removal) and were never updated through four rounds of design revision in this
  same session, so this was a full re-cut against current ARCHITECTURE.md/SPEC.md, not an
  incremental edit. Restructured to 4 stories / 29 ACs: US1 (share) now describes one recipient per
  picker invocation instead of "multiple recipients in one submit"; US2 absorbed both "manage"
  (revoke) and the two new visibility mechanisms (the context panel's "Shared with" section and the
  sidebar entity-sign badge) under one "see and manage" story, since BRIEF.md's "owner must see
  it's shared" requirement and the revoke capability now live on the same surface. US3/US4 mostly
  unchanged in shape, with ACs added for the badge's hover-tooltip wording difference ("Shared by
  the owner" vs. plain "Shared").
  PLAN.md grew from 16 to 19 tasks: split the old single "Share modal" task into three
  independent-file tasks (SC-13 entitySign manifest + badge, SC-14 context-panel section, SC-15
  picker modal) since they're genuinely separate frontend surfaces now, added SC-05 for the
  hasActiveShares backend field (previously undocumented as its own task), and added SC-16 as the
  wire task tying SC-14/SC-15 together. Dropped the stale "blocked until the Copilot RC merges"
  header — that blocker resolved earlier in this session (Copilot Workspace is live on both dev
  lines).
  Regenerated all 9 staged spec files under specs/ to match the new AC numbering and task ids, and
  added two new ones that didn't exist before: specs/backend/ConversationSharesIndicatorTests.cs
  (the hasActiveShares computation) and specs/frontend/shared-indicator.spec.ts (the entitySign
  badge's simultaneous-with-pin and hover-tooltip behavior) — neither had a spec file in the
  original pass since the mechanism they cover didn't exist yet at that point.
- **2026-10-20 (`feature-build` SC-01 review → filtered unique index + title-only search)** — The
  reviewer caught a real gap in `ARCHITECTURE.md` itself, not a builder mistake: the data model
  never said whether the `(ConversationId, SharedWithUserKey)` uniqueness applied to all rows or
  just active ones. Taken literally (a plain unique index), it would combine badly with soft-revoke
  — a revoked recipient's slot would stay occupied forever, permanently blocking re-sharing with
  them. Resolved: the index is filtered to `WHERE DateRevoked IS NULL`, so uniqueness holds only
  among active shares; sharing again after a revoke inserts a fresh row (new `DateShared`, fires the
  notification email same as any first share) rather than resurrecting the revoked one, preserving
  the full share/revoke history per recipient. `ARCHITECTURE.md`'s data model section rewritten with
  this as an explicit decision, not left implicit for `SC-02` to guess at.
  Also fixed from the same review: `SPEC.md`'s `shared-with-me` `search` param now explicitly
  matches title only, not message content — matching on content could leak whether a word exists
  past a Snapshot share's cutoff, the exact channel the cutoff exists to close. `SC-01`'s
  `AISharedConversationSummary` type is being corrected from `OwnerDisplayName` to `OwnerUserKey`
  (display-name resolution belongs in the service/Web layer, which already has to resolve it for
  `GET .../shares`' `userName` field anyway) — sent back to a fresh builder along with the other two
  findings and the reviewer's non-blocking naming/one-type-per-file suggestions.
- **2026-10-20 (`feature-build` SC-02: filtered-index cross-provider verification)** — The filtered
  unique index decided during SC-01's review (`(ConversationId, SharedWithUserKey)` unique where
  `DateRevoked IS NULL`) is the first of its kind in this codebase, so whether the raw filter SQL
  would need provider-specific branching (SQL Server vs. SQLite) was an open question rather than
  assumed. Verified empirically: generated scratch migrations against both
  `Persistence.SqlServer` and `Persistence.Sqlite` projects; both rendered the identical
  `filter: "DateRevoked IS NULL"` with no errors. No provider-conditional code needed — a single
  plain filter string works on both. Scratch migrations were deleted (not committed) once this was
  confirmed; the real, permanently-named migration is SC-03's job.
- **2026-10-20 (`feature-build` SC-04: Snapshot cutoff must fail, never silently become Live)** —
  Found during review, not anticipated at design time: resolving a Snapshot share's cutoff message
  (the conversation's current last message) can come back empty two ways — the conversation has no
  messages yet, or a regenerate race empties the tail between counting messages and fetching the
  last one. The first implementation stored `CutoffMessageId = null` in both cases, which is exactly
  what a `Live` share's cutoff looks like — an owner's explicit "frozen at today's messages" choice
  would have silently become "no ceiling, see everything forever." Resolved: the service now throws
  (`AIConversationShareCutoffUnresolvableException`) and rejects the share outright in both cases,
  applying the same "never reveal more than intended" posture the data model's read-time graceful
  degradation already uses, just at share-creation time instead. Written up as `ARCHITECTURE.md`
  decision 8 and `STORIES.md` US1 AC11. Whether "Snapshot an empty conversation" should ever be a
  supported case (it would need its own sentinel) is left as an open product question, not patched
  around.
- **2026-10-20 (`feature-build` SC-04: added a notification type, correcting an earlier assumption)**
  — `ARCHITECTURE.md`'s "Connected systems" section originally said "No new notification type is
  needed for v1." That turned out to be wrong once the email seam (decision 5) needed a way to
  decouple "a share was created" from "send an email about it" — `AIConversationShareCreatedNotification`
  now exists for exactly that, published only on a genuinely new share. Updated that paragraph to
  describe the real, current design rather than leave the stale claim in place.
- **2026-10-20 (`feature-build` pre-SC-06 research: corrected a design-doc inaccuracy about how
  Umbraco actually sends email)** — Before starting SC-06, checked the real `Umbraco.Cms` source
  for `SendEmailNotification`/`NotificationEmailModel`/`IEmailSender` rather than trusting
  ARCHITECTURE.md decision 5's original wording ("the sharing service publishes
  `SendEmailNotification`"). That's backwards: `SendEmailNotification` is an internal interception
  point `EmailSender` (the concrete `IEmailSender`) publishes itself when asked to, so *other*
  handlers can intercept a send — a feature author sends mail by injecting `IEmailSender` and
  calling `SendAsync(EmailMessage, emailType)`, the same method core's own password-reset and
  content-alert emails use. Corrected decision 5, the Extension points list, `PLAN.md` SC-06/SC-10,
  and `SPEC.md`'s POST-shares section to say this accurately before any code was written against
  the wrong mental model.
- **2026-10-20 (share-picker modal size: `small`, matching Automate's own convention)** — At the
  user's request, checked the sibling `Umbraco.Automate` product's own modals rather than picking a
  size arbitrarily. Every comparably-scoped picker/settings modal there (`node-picker`,
  `connection-type-picker`, `condition`, `notification-channel`, `trigger-settings`) uses `size:
  "small"`; only the heavier whole-node-configuration modal (`node-settings`) uses `"medium"`.
  `UAI_SHARE_PICKER_MODAL` (one person + one mode dropdown + a disclosure banner) is closer in
  weight to Automate's "settings" modals than to its node-configuration one, so `small` is the
  cross-product-consistent choice — already what the mockup used, now made explicit in `SPEC.md`
  (landed in the previous commit alongside an unrelated correction, since both were staged
  together) rather than left implicit in the mockup's CSS class alone.
- **2026-10-20 (pre-SC-09 fix: `SharedConversationResponseModel` was missing an owner-name field)**
  — Found before dispatching SC-09: `SPEC.md`'s single-conversation shared-view response never
  defined a field for the owner's display name, even though the frontend's read-only banner
  ("shared by \<owner\>") and the sidebar badge's hover tooltip both need one. Added
  `ownerDisplayName` to the response model's spec, resolved the same way `GET .../shares`'
  `userName` already is (via `IUserService` at the Web layer, not stored on the domain model) —
  caught before it became a frontend-task surprise two tasks later.
- **2026-10-21 (SC-11: `share()`/`revokeShare()` dispatch an entity-action event; `scopeMode`
  stays a plain `string` at the generated-type layer)** — Neither was spelled out in `SPEC.md`'s
  repository-additions section. (1) Sharing/revoking flips `hasActiveShares` on the owner's own
  conversation row, but nothing told the sidebar's conversation list to refetch after either call —
  caught in SC-11's review and fixed by mirroring `update()`'s existing `UaiEntityActionEvent.updated(...)`
  dispatch pattern exactly (including the same awaited-dispatch timing). `getShares()` stays a plain
  read with no dispatch. (2) `ShareConversationRequestModel["scopeMode"]` is left as the generated
  plain `string` rather than a hand-written `"Snapshot" | "Live"` union, matching this file's existing
  convention for other enum-like wire fields (e.g. `MessageResponseModel.role`); SC-15 (the mode
  dropdown) can add a stricter frontend-only type if it turns out to need one.
- **2026-10-21 (SC-12: `ownerDisplayName` lives on `UaiConversationDetailModel` itself; shared-view
  loading is a separate workspace-context method, not `#reload()`'s "second path")** — Neither was
  spelled out precisely in `SPEC.md`. (1) `ownerDisplayName?: string` was added directly to the one
  store model, set only by `toSharedConversationDetailModel()`, so the chat view's read-only notice
  and the later sidebar badge tooltip (SC-13/17) both read it off the same place rather than a
  second observable. (2) `SPEC.md` describes `#reload()` needing "a second path" for a shared
  conversation, but since no route to a shared conversation exists yet (that's SC-17/SC-18), SC-12
  added a separate, currently-uncalled `setSharedConversationId()` alongside `#reload()` instead of
  merging the branching logic into it now. Reviewer flagged this as a real but not-yet-reachable gap
  (today only owner-side writes fire the update event `#reload()` responds to) and recommended SC-18
  unify the two load paths into `#reload()` itself (keyed on `model.isShared`) rather than leaving
  two separate routines — noted here so it isn't lost before SC-18 starts.
- **2026-10-21 (SC-13: `iconColorAlias: 'green'`, not `'positive'`)** — `ARCHITECTURE.md`/`PLAN.md`
  both named `'positive'` as a placeholder, flagged as needing confirmation against the real
  icon/color sets. `'positive'` isn't a real `iconColorAlias` token in this CMS version (checked the
  actual fixed color list in `extractUmbColorVariable.function.js`); `'green'` is what Umbraco core's
  own entity signs use for this same positive/success styling (e.g. the has-schedule sign), so that's
  what shipped.
