# Build Log

- **SC-19** — Backport to v17. Done on `v17/feature/share-conversations` (branched off `v17/dev`),
  3 commits: plan docs copied unchanged, backend ported, frontend ported. Full detail lives in that
  branch's own `BUILD-LOG.md`/`DECISION-LOG.md` (plan-folder docs are branch-specific per this
  repo's `CLAUDE.md`, so this entry is a summary, not the record of truth). Headlines: normal
  cross-branch git operations were blocked by this session's safety tooling, so the port was done by
  reading each file from this branch via `git show` and writing it into the v17 worktree; two
  genuine v17-vs-v18 API differences were found and fixed (EF Core's `IEfCoreScope<T>` casing,
  `IEmailSender.SendAsync`'s obsolete 2-arg overload); migrations were regenerated fresh against
  v17's own schema rather than copied, then renamed to this branch's exact migration IDs to avoid a
  previously-documented migration-ID-drift bug class; both backend and frontend passed review
  (one fix round on the backend) with test counts matching this branch exactly (165+3 skipped unit,
  2 integration, 84 frontend). Owner-side live-verified fully working on the real v17 demo site.
  Recipient-side live verification could not be completed due to a browser-automation environment
  issue (Chrome's own autofill repeatedly overriding the login form, unrelated to the shipped code)
  — accepted via the reviewer's byte-for-byte diff confirming the recipient-rendering code is
  unchanged from this branch, which was already live-verified working as a real recipient login
  here (see SC-18's entry above).
- **Whole-feature review** (per `feature-build`'s "Finish" step, all 18 tasks SC-01–SC-18 already
  committed and individually reviewed). Commit pending. One round: **FAIL** → fixed → **PASS**.
  Full suite re-run clean before the review: `dotnet test` on
  `Umbraco.AI.Agent.Copilot.Workspace.slnx` (164 passed + 3 pre-existing skipped, 2 integration
  passed), `npm run test` (84/84), `tsc --noEmit` clean.
  **1 Critical finding, fixed:** a Snapshot share (the owner's explicit choice to freeze a
  conversation "at today's messages") could silently become an unbounded Live share. If the owner
  later regenerated a message, the regenerate's pre-existing truncate could delete the exact message
  a Snapshot share's `CutoffMessageId` pointed at; the old read-time fallback for "cutoff not found"
  responded by returning *every message that currently exists*, which — once the owner wrote
  anything new post-regenerate — meant the recipient permanently saw content the owner never meant
  to disclose past the freeze point. No single task's own tests caught this, since it only manifests
  across three things that are each correct in isolation (SC-04's cutoff-at-share-time, the
  pre-existing unrelated regenerate/truncate feature, and SC-07's cutoff-not-found fallback). Fixed:
  `AIConversationShareReadService.ApplyCutoff`'s fallback now bounds by the share's own `DateShared`
  timestamp (`messages.Where(m => m.DateCreated <= share.DateShared)`) instead of returning
  everything current — a time bound can't be fooled by a post-truncation message reusing a
  `Sequence` number that looks earlier than it is. `SharedConversationResponseModelFactory`'s frozen
  `dateModified`/`lastMessageAt` now derive from the actual bounded message list
  (`messages.Max(m => m.DateCreated)`), not an assumption that the last array element is always the
  correct tail. New regression test
  `GetSharedConversationAsync_CutoffMessageDeletedThenRegenerateAddsNewMessage_ExcludesMessageWrittenAfterShare`
  reproduces the exact bug (deleted cutoff, reused Sequence, dated after `DateShared`) and fails
  against the old code. `dotnet test` after the fix: 165 passed (was 164 — one old test, whose name
  asserted the buggy behavior, was renamed and a second, real regression test added alongside it) +
  3 pre-existing skipped, 2 integration passed.
  **1 Important finding, fixed directly by the orchestrator (doc-only, not code):** several
  `DECISION-LOG.md` outcomes from mid-build task deviations had never been written back into
  `ARCHITECTURE.md`/`SPEC.md`, which are meant to be current-state snapshots — the second
  `entitySign` manifest for "Shared with me"'s dynamic badge (SC-17), `iconColorAlias: 'green'` not
  `'positive'` (SC-13), the real entity-type string, the try-then-fallback routing description
  (SC-18), `scopeMode`'s PascalCase casing, and a TODOs list that still listed several
  already-resolved items. All corrected in place to describe what actually shipped.
  **Non-blocking suggestions logged for later, not actioned:** duplicated `--umb-sign-bundle-bg` CSS
  between the two tree-item elements; duplicated unknown-user-display-name and acting-user-key
  helpers across the share services/factories; three empty `[Fact(Skip=...)]` DI-resolution test
  stubs; no server-side validation that a recipient key is a real user; a concurrent-share race
  against the filtered unique index would 500 instead of upserting; `HasActiveShares` always reports
  `false` from non-list endpoints; N+1 user lookups on the shares/shared-with-me list endpoints;
  this product's `CLAUDE.md` Management API table doesn't yet list the 5 new endpoints; archived
  conversations remain visible to recipients (undocumented as a decision, probably fine).
- **SC-18** — Wire: recipient reads a shared conversation end-to-end. Commit pending. Two builder
  passes, one review round (PASS):
  (1) First pass fixed the routing gap: `conversation-workspace.context.ts`'s `#reload()` now falls
  back to `setSharedConversationId(id)` when the owner lookup 404s, so `setConversationId` (the one
  method the `conversation/:id` route calls) transparently resolves either an owned or a shared
  conversation — there's only one route/URL shape for both (SC-17's sidebar rows deliberately reuse
  the same `copilotWorkspaceConversationPath()` helper an owned row uses). New
  `conversation-workspace.context.test.ts` (10 tests).
  (2) That pass's own builder found, but correctly left unfixed (flagged for a decision instead of
  guessing), a second gap in the same feature: `chat/server-persisted-conversation.strategy.ts`'s
  `loadInitial()` always called the owner-only `requestMessages(id)`, which also 404s for a
  non-owner, so a shared conversation's chat history silently failed to load even once pass 1 let
  the metadata/read-only state resolve. A second builder pass fixed it with the identical
  try-then-fallback pattern: falls back to `requestSharedView(id).messages` (confirmed the same
  `MessageResponseModel` type as `requestMessages`'s `items`). Extended
  `server-persisted-conversation.strategy.test.ts` (+2 tests). Reviewer independently re-verified the
  404-as-no-throw contract for both calls, walked the request-token races in `#reload` by hand (safe
  in every ordering checked), and confirmed the deliberate decision to make two separate
  `requestSharedView` calls (store + strategy) rather than thread data between the two decoupled
  contexts. `npm run build:copilot-workspace` clean and `npm run test:copilot-workspace` 84/84
  passing throughout, no regressions.
  **Live verification — fully completed.** Avoided this plan's well-documented picker/modal
  automation flakiness (see SC-16, SC-17 above) entirely by seeding the share directly in the demo
  site's SQLite database (a real row in `umbracoAIConversationsShare`, Snapshot mode, targeting the
  conversation's real last message as the cutoff) rather than driving the share-picker UI, then
  resetting a real interactive demo user's ("Recipient Tester") password via the Users section and
  logging in as them — sidesteps the flaky picker without weakening what's being proven, since the
  share row itself is real data the backend will enforce identically regardless of how it was
  created. Confirmed, as that real recipient user, in the real running backoffice:
  - The "Shared with me" sidebar entry appears with the correct title, and hovering its badge shows
    "Shared by Administrator" (closes out SC-17's own live-verification gap — see its entry above).
  - Clicking it loads the real conversation: the actual 3 persisted messages render (not empty), the
    composer is replaced with "This conversation is read-only — shared by Administrator.", and the
    context panel shows only Contexts/Resources — no "Shared with" section (owner-only, correctly
    gated).
  - No entity-actions dropdown renders on the sidebar row (confirmed via the real DOM — the element
    genuinely omits it, not just hides it).
  - Network log confirmed the real fallback chain firing exactly as designed: owner `GET
    /conversations/{id}` 404 → `GET /conversations/{id}/shared-view` 200 (store); owner `GET
    /conversations/{id}/messages` 404 → `GET /conversations/{id}/shared-view` 200 again (strategy,
    the accepted duplicate call).
- **SC-17** — "Shared with me" sidebar grouping. Commits `e3f9dec1` (feature) + `4f4f04e3`
  (checkbox/log). Three builder/review rounds:
  (1) First builder pass built the new sidebar slice, menu, and tree-item correctly, but hand-rolled
  the per-row "Shared by `<owner>`" badge (plain `<umb-icon>` + copied ring CSS + a native `title`
  attribute) instead of using the real `entitySign`/`<umb-entity-sign-bundle>` extension system
  SC-13's owner-side badge uses, on the stated belief that the real mechanism can't show per-row
  dynamic hover text (its label looked fixed per manifest). Reviewer FAILed this: independently
  verified in the actual `@umbraco-cms/backoffice` source that `UmbEntitySignApi.label` can be an
  `Observable<string>`, and `entity-sign-bundle.element.js` creates a fresh api instance *per row*
  (per `<umb-entity-sign-bundle>`), whose element ends up a real DOM descendant of the row — so a
  custom api can `consumeContext` an ambient, per-row context the same way
  `UaiConversationEntityContext` already does in this codebase. (2) A fresh builder implemented the
  prescribed fix: new `UaiSharedConversationRowContext` (per-row context carrying
  `ownerDisplayName`), a new `UaiSharedWithMeSignApi` (consumes that context, exposes a reactive
  `label` built from `localize.term("uaiCopilotWorkspace_sharedByOwner", owner)`), a second
  `entitySign`/`kind: "icon"` manifest entry (`Uai.Conversation.IsSharedWithMe`), and the tree-item
  rewritten to use the real `<umb-entity-sign-bundle>` instead of the hand-rolled badge. (3) PASS —
  the orchestrator independently re-verified the api-loader contract and the context-bubbling
  mechanism against the real CMS source before accepting the re-review.
  **Everything else passed review in round 1** and was left unchanged across rounds: the
  `sidebar.context.ts` `#sharedWithMe` slice (loads once per section mount, no live cross-user
  push — a share created/revoked by the owner happens in a different user's session, and this
  codebase has no mechanism for that), the `group-not-empty.condition.ts` `"sharedWithMe"` match,
  the `sidebar/manifests.ts` registration at weight 150 (between Recent=200 and the Archived
  footer=100), and the standalone `shared-with-me-menu.element.ts` (not forced through the
  `ConversationResponseModel`-typed `UaiConversationListMenuElementBase`, since the data shape
  genuinely differs). `npm run build:copilot-workspace` clean and `npm run test:copilot-workspace`
  72/72 passing throughout all three rounds, no regressions.
  **Live verification at commit time — not completed, code-trace only.** This is the first
  dynamic-label `entitySign` in this codebase, so a real hover check was worth attempting. The
  orchestrator made two careful, isolated attempts directly against the running demo site (including
  a fresh admin login after a session timeout) to create a real share, open "Shared with me", and
  hover the badge. Both attempts hit the same modal-instability pattern already documented in
  SC-16's entry above: the share-picker/user-picker modal chain closed itself partway through, even
  when avoiding the specific zoom/scroll/JS-query triggers SC-16 identified. Given SC-16 already
  established (and the team accepted) that this category of hover/picker interaction is not reliably
  drivable via browser automation in this environment, and given the mechanism here was
  independently verified by two separate reviewers plus the orchestrator directly against the real
  CMS source (down to the exact function: `entity-sign-bundle.element.js`'s
  `#gotProperties`/`#handleHoverTimer`, `create-extension-element-with-api.function.js`'s constructor
  call, `UmbEntitySignApi`'s `label?: Observable<string>` contract), this was accepted as done on
  code-level verification, consistent with SC-16's precedent.
  **Update, during SC-18's live verification below: live-confirmed.** Seeding a real share directly
  in the demo database (sidestepping the flaky picker UI entirely) and logging in as the real
  recipient user let the badge actually be hovered for real. It shows exactly "Shared by
  Administrator" — the dynamic per-row label works as designed. (SC-13's own owner-side badge was
  not re-hovered in this pass — its code path is untouched by this feature and was already confirmed
  separately in SC-13's own entry.) No code change was needed; this just closes out the
  live-verification gap left open above.
- **SC-16** — Wire: "Shared with" section + picker modal end-to-end. No commit (verification-only
  task, no code changes — nothing found broken). Done directly by the orchestrator, live against the
  running demo site, not delegated.
  **Fully verified, live, in the real backoffice:**
  - Revoking an existing share removes the row immediately, with no page reload, and the UI only
    drops the row after the real `DELETE .../shares/{userKey}` call actually succeeds (exercised
    SC-14's fixed `#onRevokeShare`).
  - "Choose" opens the real `UAI_SHARE_PICKER_MODAL` (confirmed by its real custom-element tag and
    action buttons), matching the mockup's layout (headline naming the conversation, Person field,
    Mode dropdown, Share disabled with nothing picked).
  - "Choose a person" opens the real, unmodified Umbraco core `UMB_USER_PICKER_MODAL`
    (`umb-collection-item-picker-modal`), which lists real users from a real
    `GET /umbraco/management/api/v1/filter/user` call (confirmed via the network log) — not a stub
    or mock.
  **Not completed live:** actually picking a specific user card inside that vendor picker and
  watching the add-flow finish (new row appears without reload) could not be driven through browser
  automation in this session. Traced the exact mechanism to confirm it's a real, reachable feature
  (not a defect in this plan's own code): `uui-card-user`'s clickable body unconditionally fires an
  `open` event and calls `e.stopPropagation()` before a `selectable` host's own click-to-select
  listener ever runs, so selecting requires precisely hitting a small checkbox that's only
  `opacity: 1` on hover/focus (`#select-checkbox { top/left: var(--uui-size-4); opacity: 0 }` in
  `@umbraco-ui/uui`'s own `card.element.js`) — every attempt to hover, zoom, scroll, or query the
  DOM between a click and a follow-up check closed the modal outright in this specific
  Claude-in-Chrome setup (most likely a resize/viewport-change side effect of those actions that the
  modal manager treats as a dismiss trigger), so the click-then-verify loop needed to land this
  precisely never got a clean opportunity to succeed. This is 100% pre-existing, unmodified Umbraco
  core picker UI (`UMB_USER_PICKER_MODAL`, used unchanged throughout the rest of the product) — not
  code this feature introduces or could introduce a regression in.
  **Why this is still accepted as done:** the exact mechanism this final click-through would prove
  (`share()` call → real `POST .../shares` → refetch → row renders) is the same mechanism already
  proven end-to-end via real HTTP in SC-10 (create, re-share, revoke, 404-after-revoke, real email,
  `hasActiveShares` flip), and the component wiring that calls it
  (`#onChooseRecipient` → `umbOpenModal` → `share()` → `#fetchShares()`) was independently confirmed
  correct by two rounds of code review in SC-14. The one remaining link — a vendor component
  correctly resolving `value.selection` on pick and `_submitModal()` on "Choose" — is standard,
  widely-used Umbraco core behavior outside this feature's own risk surface.
- **SC-14** — "Shared with" context panel section. Commit `61a45215`. Two review rounds: (1) FAIL —
  one Important bug: revoke removed the recipient's row from the UI optimistically, before the
  DELETE call was even confirmed — on a failed request the owner would see the row gone while the
  recipient still had real access, a security-relevant UI/reality mismatch; fixed to only remove the
  row after a confirmed-successful response. Also fixed two related suggestions alongside it: a real
  request-token guard (mirroring `UaiConversationWorkspaceContext`'s own `#requestToken`) so an
  older in-flight shares fetch can never clobber a newer one (e.g. the initial fetch landing after a
  post-share refetch), and a title-fallback inconsistency (`?? ` → `?.trim() ||`, matching
  `tree-item.element.ts`). Also narrowed `conversation.repository.ts`'s `share`/`getShares`/
  `revokeShare` (SC-11) from taking a full `ConversationResponseModel` to a plain `id: string` —
  disclosed scope deviation into an already-committed file; reviewer confirmed it's safe (nothing
  called the old signature yet, the data-source underneath already took `id`) and the fix also
  updated `SPEC.md`'s stale method signatures to match. (2) PASS. Live-verified in the running demo
  site: the section renders first, above Contexts/Resources, owner-only; lists a real share with its
  mode; the trash icon appears on hover; "Choose" opens the real `UAI_SHARE_PICKER_MODAL` (confirmed
  via its real tag and action buttons) — screenshot at
  `screenshots/context-panel-shared-with.png`. vitest 72/72.
- **SC-15** — Share-picker modal. Commit `d88c08ad`. Built before SC-14 (order swapped — see
  `DECISION-LOG.md`). Two review rounds: (1) FAIL — one Important finding: both fields used bare
  `uui-label`/manual help text instead of this repo's standard `umb-property-layout` wrapping
  (`CLAUDE.md`'s "mirror an existing editor" rule), and the `uui-select` had no `label` attribute
  for screen readers; fixed by mirroring `Umbraco.AI`'s own `resource-options-modal.element.ts`
  pattern. Also took the reviewer's two free suggestions alongside the required fix: swapped a
  hand-rolled `UmbPickerInputContext<UmbUserItemModel, UmbUserDetailModel>` construction for the
  real, already-exported `UmbUserPickerInputContext` (same wiring, simpler), and removed a no-op
  `min = 1`. (2) PASS. Confirmed real and correct against CMS 18.0.0 source (not guessed): the
  user-picker's `max = 1` genuinely constrains `UMB_USER_PICKER_MODAL` to a single pick
  (`multiple: false`, traced through `picker-input.context.js`) — resolving `ARCHITECTURE.md`
  decision 7's open question. The modal only resolves its value; the caller (SC-14) will invoke
  `UaiConversationRepository.share(...)` after it closes, mirroring the existing rename-modal split.
  Activated the staged `share-conversation.spec.ts` as real, un-skipped helper tests. No end-to-end
  browser check yet (no real caller exists until SC-14/SC-16) — code-level mockup comparison was the
  bar for this task. vitest 63/63.
- **SC-13** — EntitySign manifest + owner's tree-item badge. Commit `f40f49c5`. First use of
  Umbraco's `entitySign` extension mechanism in this codebase. Two review rounds: (1) FAIL — the
  manifest/wiring itself matched real CMS source exactly (verified field-for-field against core's
  own `is-protected` entitySign and `media-tree-item`'s bundle usage; `iconColorAlias: 'positive'`
  from the plan isn't a real token, swapped for `'green'`, the real one core uses for this same
  positive/success styling), but 2 Important gaps: the staged `shared-indicator.spec.ts` was never
  activated into real tests, and the badge had no cut-out ring because `--umb-sign-bundle-bg` was
  never set on the tree item (copied verbatim from core's own tree-item-base styles, one rule per
  hover/active/selected state). (2) PASS — tests activated as `tree-item.helpers.ts`/`.test.ts`
  (pulling the flag/icon-name logic into pure, independently-testable functions), staged spec
  deleted, `grouping.test.ts`'s `hasActiveShares` fixture gap (flagged in SC-11's own entry) filled
  in. Live-verified in the running demo site, not just build/test: re-shared a real conversation to
  flip `hasActiveShares` true, pinned it too, and visually confirmed the green share badge renders
  with a real cut-out ring, correctly alongside the pushpin icon (AC6), with a "Shared" hover
  tooltip and nothing else opening (AC5, AC7) — screenshot at `screenshots/sidebar-shared-badge.png`
  (Copilot section → sidebar → a pinned+shared conversation's icon). vitest 54/54.
- **SC-12** — Detail model + workspace-context + context-panel readonly plumbing. Commit
  `726484a3`. One review round, PASS first try. `UaiConversationDetailModel` gains
  `isShared`/`readonly` (derived once at mapping time) and an optional `ownerDisplayName`;
  `toSharedConversationDetailModel()` maps a `SharedConversationResponseModel` with `readonly: true`
  unconditionally. `isReadonly$` and the context panel's 4 `isArchived` checks switch to `readonly`.
  The chat view's notice text now distinguishes archived from shared-by-owner, following a real
  interpolated-`localize.term()` precedent from `Umbraco.AI.Agent.UI`. Added a currently-uncalled
  `setSharedConversationId()` on the workspace context as SC-18's future entry point (no route to a
  shared conversation exists yet). Reviewer confirmed the readonly derivation can't go stale and
  `ownerDisplayName` can't leak onto an owned conversation, and left 4 non-blocking suggestions for
  SC-18 to pick up: unify `#reload()`'s two load paths instead of a separate method, decide whether
  `setSharedConversationId` should no-op on an already-open id like `setConversationId` does, add a
  generic fallback if `ownerDisplayName` is ever empty, and add an element-level test for the
  notice-picking logic. `dotnet`/`npm` builds clean; vitest 50/50 (up from 45 after SC-11).
- **SC-11** — OpenAPI client regen + `conversation.repository.ts` additions. Commit `7c2ba810`.
  Two review rounds: (1) PASS with one requested fix — `share()`/`revokeShare()` didn't dispatch
  `UaiEntityActionEvent.updated(...)` on success the way `update()` does, so the owner's sidebar
  `hasActiveShares` badge (SC-13) would go stale after the first share or last revoke until some
  unrelated refresh happened to occur; fixed by mirroring `update()`'s exact awaited-dispatch
  pattern. (2) PASS, confirmed via a targeted re-check of just the fix. Confirmed the OpenAPI
  client regen genuinely picked up all 5 new endpoints (`ai-copilot-workspace-management` doc name
  matches `CopilotWorkspaceConstants.ApiName`) plus `hasActiveShares` on `ConversationResponseModel`;
  confirmed the `share()` wire body (`recipientUserKeys: [recipientUserKey]`) matches the generated
  `ShareConversationRequestModel` field name/casing exactly, not just type-checks against a loose
  type. Backend-data-access-only task — no UI wiring, no new manifests, no callers added (that's
  SC-12 onward) — so the build/typecheck + 45 passing vitest specs is the applicable gate, same
  precedent as SC-01/SC-02's "no runtime entry point yet" foundation tasks. One deferred item: test
  fixtures in `grouping.test.ts`/`types.test.ts` are missing the now-required `hasActiveShares`
  field (invisible to `tsc` since `*.test.ts` is excluded from the build) — left for SC-13 to add
  when it starts testing the badge, per reviewer's note.
- **SC-10** — Wire: backend endpoints resolve for real. No commit (verification-only task, no
  code changes). Built the frontend once (`npm run build`) since this worktree had never built it
  — the Copilot Workspace section didn't even appear in the backoffice nav until the static assets
  existed; a `dotnet build` alone doesn't regenerate the Vite output. Seeded a real conversation
  with messages directly into the demo site's SQLite file (no AI connection configured in this
  environment, so going through a live chat completion wasn't possible) — hit and fixed a real
  gotcha worth remembering: EF Core's SQLite GUID-to-TEXT mapping stores/compares uppercase, so a
  lowercase-cased seeded `Id` 404s on every by-id lookup even though list queries (scanned, not
  indexed-compared the same way) still return it; fixed by uppercasing the seeded GUIDs. Verified
  every endpoint for real via two Umbraco API Users (client-credentials OAuth — a demo site has no
  way to borrow the interactive backoffice's in-memory bearer token, and API Users are the
  supported, intended path for exactly this): create a Snapshot share (confirmed a real `.eml`
  landed in the SMTP pickup directory, matching the approved mockup's text and linking to the real
  conversation route), `hasActiveShares` flipped true on the owner's paged list (and confirmed, not
  a regression, that `GetConversationAsync`/`GetById` still always report `false` — the documented
  SC-05 limitation), listed the share, read it back via `shared-view` as the recipient (correct
  `ownerDisplayName`), saw it in `shared-with-me`, re-shared the same recipient as Live (updated the
  existing row — same `dateShared`, no second email), confirmed Live then surfaced a message added
  after the original share while Snapshot had kept it frozen (live-exercised SC-09's cutoff-freeze
  fix, decision 9, with a real third message seeded after the share), revoked (204), confirmed the
  next `shared-view` 404s, `shared-with-me` goes empty, `hasActiveShares` flips back to `false`, and
  revoking an already-revoked share 404s rather than silently no-op succeeding (US2 AC9). All green
  against the real running app, real HTTP, real auth — nothing bypassed.
- **SC-09** — Recipient-side share Management API controllers. Commit `cd77a0ec`. Two review
  rounds: (1) FAIL — a builder deleted two pending DI-resolution test placeholders on a false
  premise (claimed SC-08 set a delete-them precedent; SC-08 actually left its own skipped and
  retargeted). Restored both and aligned all three placeholders (including the already-committed
  SC-08 one) on identical wording pointing at SC-10's live verification. Also resolved a new
  finding into an explicit decision (ARCHITECTURE.md decision 9, STORIES.md US4 AC7): a Snapshot
  share's `dateModified`/`lastMessageAt` were about to leak that the owner kept chatting after the
  cutoff — even with no message content exposed, the metadata alone said "something happened after
  this." Fixed by freezing both fields at the cutoff message's own timestamp for Snapshot shares
  only. This required a disclosed, reviewed widening of SC-07's already-committed
  `IAIConversationShareReadService.GetSharedConversationAsync` return tuple (adding the share's
  `ScopeMode` — data already in scope, no new query) since the Web layer had no other way to tell
  Snapshot from Live; reviewer confirmed this was the right call given the interface is pre-release
  (only exists on this feature branch, no external consumer to break). (2) PASS. `dotnet
  build`/`dotnet test`: 164 unit + 3 skipped + 2 integration passing throughout.
- **SC-08** — Owner-side share Management API controllers. Commit `848707f9`. Two review rounds:
  (1) FAIL — `Enum.TryParse` alone accepts a numeric string (`"7"`) for a 2-member enum with no
  corresponding value, letting a nonsense scope mode silently flow through and behave like Live
  (no cutoff ever gets set for an undefined value); fixed by requiring `Enum.IsDefined` too. The
  builder's own addition beyond the dispatch — a distinct 400 for an unparseable scope mode, plus
  catching the cutoff-unresolvable exception before the general ownership/not-found catch (C#
  matches the first applicable `catch`, so order matters: the more specific exception type must
  come first or it's unreachable) — both held up under review. (2) PASS, plus 2 added test cases
  (a GET happy path, the "Unknown user" display-name fallback) that were real coverage gaps, not
  required fixes. `dotnet build`/`dotnet test`: 157 unit + 3 skipped + 2 integration passing
  throughout.
- **SC-07** — Recipient-side shared-read service methods. Commit `9f2044c0`. Two review rounds:
  (1) FAIL — 1 Important finding: a cross-service test (revoke via SC-04's service, then read via
  this one) used a mock with no actual shared state, so it would have passed even if revoke were a
  complete no-op — rewritten with a stateful mock and proven to genuinely fail when the revoke
  callback is disabled. Plus two cheap fixes: a misleadingly-named test (asserted a `null` return but
  was named `_ThrowsNotFound`) and lingering plan-doc citations in two files that a follow-up round
  caught the interface file had too, after the first fix only touched the implementation. (2) PASS.
  Follows the existing `ConversationChatHistoryProvider` precedent for a specialized in-assembly
  reader bypassing the owner-gated service — confirmed this is a real precedent, not invented.
  `dotnet build`/`dotnet test`: 147 unit + 3 skipped + 2 integration passing throughout.
- **SC-06** — Share-notification email. Commit `feab5c0a`. Pre-build research corrected a real
  inaccuracy in ARCHITECTURE.md decision 5 (the design doc said the sharing service "publishes
  `SendEmailNotification`" directly — checked against the real `Umbraco.Cms` source and found that's
  backwards; the actual mechanism is `IEmailSender.SendAsync`). Two review rounds on the build
  itself: (1) FAIL — 1 Critical (the email's "view conversation" link used an invented route and the
  wrong constant — the section *alias* instead of its *pathname* — so it pointed nowhere real) + 3
  Important (a happy-path test loose enough to pass even with the broken link; the handler could
  still throw outside its one `try/catch`, which — because `AIConversationShareService` awaits this
  notification inline per recipient — would have failed the owner's whole share request and silently
  dropped any remaining recipients; an unnecessary/incorrectly-justified assembly-visibility change
  that a simpler, already-proven form would have covered). All fixed, verified against the real
  frontend route files and a working sibling precedent rather than re-guessed. (2) PASS. Also
  resolved as an explicit, documented trade-off (not a defect): the email sends inline rather than
  queued to a background worker, accepting SMTP latency on the share request for what's scoped as a
  low-volume, internal-collaboration signal. `dotnet build`/`dotnet test`: 137 unit + 2 skipped + 2
  integration passing throughout.
- **SC-05** — `hasActiveShares` on the owner's paged conversation list. Commit `9b9c0460`. Two
  review rounds: (1) FAIL — the new tests each set up only one conversation, so they'd still pass if
  the production query dropped its `ConversationId` correlation entirely (every row would report the
  same global flag); fixed by adding a two-conversations-same-owner test proving the flag differs
  per row in the same page. Also added, same round: a doc-comment disclosure that the field is only
  accurate from the paged list endpoint (always `false` from `GetById`/create/update, which don't
  compute it), and moved the new SQLite test scaffolding into a shared `Fixtures/` folder for later
  Conversations-repository tests to reuse instead of re-copying it. (2) PASS. First EF-Core-backed
  (not pure-mock) test harness for this product's repository layer — justified since a mocked
  repository can't meaningfully prove a correlated-subquery shape is actually correlated. `dotnet
  build`/`dotnet test`: 130 unit + 2 skipped + 2 integration passing throughout (one known
  pre-existing flaky integration test, unrelated, noted but not chased).
- **SC-04** — Owner-side sharing service methods. Commit `3cb6d512`. Three review rounds: (1) FAIL
  — 3 Important findings: a new domain notification (`AIConversationShareCreatedNotification`)
  didn't match this codebase's real notification shape (missing `EventMessages`, verified against
  `AIAgentSelectedNotification`'s real precedent rather than the builder's unverified claim there
  was none); a stale plan-task-id code comment; and the serious one — a Snapshot share whose cutoff
  message couldn't be resolved (empty conversation, or a race with regenerate) silently fell back to
  behaving like a Live share with no ceiling, the opposite of what the owner asked for. Resolved by
  the orchestrator as a new, explicit design decision (ARCHITECTURE.md decision 8, STORIES.md AC11):
  fail the share request outright rather than silently widening it. Also settled during this round:
  no unit tests existed for the new service, against this codebase's own convention of every service
  having them — added real tests (moving the two staged spec stubs into the real test project) rather
  than deferring. (2) PASS on the code, with two non-blocking suggestions (a loose exception-type
  assertion that wouldn't have caught the exact bug just fixed if it regressed; a wrong doc-comment
  cross-reference). (3) Fixed both anyway since they were cheap and the first one meaningfully
  strengthens the regression test; final PASS. `dotnet build`/`dotnet test`: 125 unit (up from 111)
  + 2 integration passing throughout, 2 unit tests honestly left skipped pointing at SC-07/SC-08 work
  they depend on.
- **SC-03** — SqlServer + SQLite migrations. Commit `3d9a38c3`. One review round, PASS first try.
  Verified live on both providers (not just generated): applied via `dotnet ef database update`
  against a throwaway SQLite file and a throwaway SQL Server Docker container, inspected the real
  schema (`sys.indexes`/`.schema`) to confirm the filtered unique index, cascade delete, and
  uniqueness-violation behavior all work as designed; reviewer independently re-ran the same checks
  rather than trusting the builder's report, including spinning up its own SQL Server container.
  Orchestrator then smoke-tested through the actual application lifecycle: started the demo site for
  real, confirmed `[INF] Running Umbraco.AI.Agent.Conversations database migrations...` /
  `...completed successfully` in its startup log, and confirmed the `umbracoAIConversationsShare`
  table (with the filtered index) exists in the demo site's real SQLite database file — not just a
  standalone throwaway db.
- **SC-02** — EF Core entity + repository implementation (Persistence). Commits `0c0f6d12`
  (feature) + `54983aed` (follow-up: inlined two SC-01 doc comments that pointed at
  ARCHITECTURE.md by section, found during this task's review). Four review rounds: (1) FAIL —
  the `SaveAsync` insert path copied `Id`/`DateShared`/`DateRevoked` straight from the caller's
  domain object instead of stamping them server-side, which could silently insert an
  already-revoked "new" share, a wrong-dated one, or collide on a reused id from an old revoked
  row; also flagged new comments (plus two pre-existing SC-01 ones) pointing at `ARCHITECTURE.md`
  by name. (2) FAIL — one leftover `SPEC.md` comment pointer missed in the first fix. (3) PASS on
  the code; (4) final confirmation pass, PASS. The filtered unique index
  (`(ConversationId, SharedWithUserKey)` unique where `DateRevoked IS NULL`) — the first of its
  kind in this codebase — was verified via scratch migrations against both SQL Server and SQLite;
  the plain filter string worked identically on both, no provider branching needed. No runtime
  entry point exists yet (no migration applied, no controller) — `dotnet build`/`dotnet test`
  (111 unit + 2 integration, unrelated to this change) are the applicable gate until SC-03/SC-08
  land.
- **SC-01** — Domain model + repository interface (Core). Commit `eac90601`. Three review rounds:
  (1) FAIL — 3 Important findings, the key one a real gap in `ARCHITECTURE.md` itself (the
  recipient-uniqueness constraint didn't say whether it applied across revoked rows, which as
  written would have permanently blocked re-sharing with anyone ever revoked); resolved by filtering
  the uniqueness to active rows only and documenting it. (2) FAIL — one stale doc comment left after
  the first fix round. (3) PASS. `dotnet build` clean throughout; no runtime entry point exists yet
  for this foundation task (nothing calls these types until SC-02), so the build itself is the smoke
  gate here, per the reviewer's own conclusion.
