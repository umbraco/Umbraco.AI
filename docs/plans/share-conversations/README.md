# Share Conversations

Read these in order:

1. [x] [BRIEF.md](./BRIEF.md) — owner picks named colleagues and shares a conversation as a
   Snapshot or Live copy; the recipient is emailed and sees it in their own "shared with me" list;
   the owner can see it's shared, with whom, and can revoke it. Nothing deferred for v1.
2. [x] [ARCHITECTURE.md](./ARCHITECTURE.md) — new `AIConversationShareEntity` child table; shared
   reads go through a separate code path from owned reads; attachments shown as-is with a
   share-time disclosure warning, not filtered; the owner's conversation list shows "shared" via a
   real Umbraco entity-sign badge (stacks with "pinned", doesn't replace it); sharing lives entirely
   in the context panel's "Shared with" section — no entity action, no separate manage modal, same
   list-plus-picker shape as the existing Contexts/Resources sections. Re-verified against the real
   `v18/dev` code.
3. [x] [SPEC.md](./SPEC.md) — the shares CRUD endpoints, `shared-with-me`/`shared-view`, the
   context panel's "Shared with" section + single-recipient share-picker modal (real user picker,
   scope dropdown), read-only chat/context-panel reuse, and the tree-item entity-sign indicator,
   each as an observable contract, with a mockup screenshot for every screen.
4. [x] [STORIES.md](./STORIES.md) — 4 stories (share, see+manage, find shared-with-me, read
   shared-with-me), 29 acceptance criteria total, Given/When/Then with sad paths
5. [x] [PLAN.md](./PLAN.md) — 19 tasks, first parallel group is SC-04/SC-05/SC-07 (backend
   services); no longer blocked on the Copilot RC — it's live on both dev lines
6. [x] [BUILD-LOG.md](./BUILD-LOG.md) — all 19 tasks shipped on both `v18` (38 commits,
   whole-feature review passed, 1 Critical fix for a real data-exposure bug a regenerate could
   cause) and `v17` (3 commits: docs, backend, frontend). Owner-side fully live-verified on both
   lines; recipient-side live-verified on v18, accepted via code-diff parity on v17 after a
   browser-automation environment issue (not a code defect) blocked a fresh v17 login check

Also see [RESEARCH.md](./RESEARCH.md) — the competitive research behind this feature (written for
the CTO), and [DECISION-LOG.md](./DECISION-LOG.md) for why things changed along the way.
