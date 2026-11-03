[Plan folder](docs/plans/share-conversations/) | [v18 PR](https://github.com/umbraco/Umbraco.AI/pull/657)

## Why the change

Backports the share-conversations feature (lets a Copilot Workspace conversation owner share a read-only copy with named colleagues) from `v18/dev` to `v17/dev`, per this repo's keep-active-versions-in-sync policy — same design, same behavior, only the target CMS version differs.

## What it looks like

**Where to find it:** Copilot section › any conversation › context panel's new "Shared with" section (owner) and the sidebar's new "Shared with me" group (recipient); the owner's own conversation rows also gain a small "Shared" badge. Identical to the `v18` PR — no UI differences between the two lines.

**Who can see it:** Anyone with access to the Copilot Workspace section — same access as the rest of Copilot Workspace, no new permission.

![Sidebar shared badge](https://github.com/umbraco/Umbraco.AI/blob/203097d4701c73cde2e538fc4a4c5f35117df088/docs/plans/share-conversations/screenshots/sidebar-shared-badge.png?raw=true)

![Shared with context panel section](https://github.com/umbraco/Umbraco.AI/blob/203097d4701c73cde2e538fc4a4c5f35117df088/docs/plans/share-conversations/screenshots/context-panel-shared-with.png?raw=true)

## Special things to note

- Not a new design pass — `ARCHITECTURE.md`/`SPEC.md` apply unchanged from the `v18` PR. This PR is a mechanical port: reviewed diff-for-diff against `v18/feature/share-conversations` and confirmed byte-identical except the two items below.
- Two genuine v17-vs-v18 API differences were found and fixed, not design choices: EF Core's scope type is `IEfCoreScope<T>` on CMS 17.5.0 (`IEFCoreScope<T>` on v18), and `IEmailSender.SendAsync`'s 2-arg overload is obsolete on v17 (removed in 18) — the new notification handler calls the 4-arg overload explicitly instead.
- The two new EF Core migrations were **not** copied from the v18 PR. They were regenerated fresh against `v17/dev`'s own schema history, confirmed structurally identical to v18's migration, then renamed to v18's exact migration IDs (`20261020132135_UmbracoAIConversations_AddShares` / `20261020132143_UmbracoAIConversations_AddShares`) — deliberately matching IDs across both lines to avoid a migration-ID-drift bug class this repo has shipped before (see `.claude/memory/project_migration_id_drift.md`). Applied and rolled back on a real scratch SQLite database as part of review.
- **Needs a decision:** recipient-side live verification (logging in as the share's recipient and confirming the "Shared with me" entry, its hover tooltip, and the read-only chat render) could not be completed on this branch's own demo site — a Chrome autofill issue in this session's browser-automation environment, not a code defect, confirmed through many isolated attempts (see `DECISION-LOG.md`'s SC-19 entries for the full trace). Owner-side behavior (badge, tooltip, "Shared with" panel, real message history) was fully live-verified on this branch instead, and the recipient-rendering code itself is confirmed byte-identical to the `v18` PR, where it was fully live-verified. Flagging so a human can decide whether that's sufficient before merge or whether a manual recipient-side check is wanted first.
- Same whole-feature-review fix as the `v18` PR is included here too: a Snapshot share could silently become an unbounded Live share after a regenerate; already fixed in the ported code (see that PR's notes for the full trace).

## Change outline

Only the EF Core migration IDs are worth calling out specifically for this PR — everything else is identical to the `v18` PR's own change outline:

```diff
 Migrations/
+├── 20261020132135_UmbracoAIConversations_AddShares.cs          (SqlServer)
+├── 20261020132135_UmbracoAIConversations_AddShares.Designer.cs
+├── 20261020132143_UmbracoAIConversations_AddShares.cs           (SQLite)
+└── 20261020132143_UmbracoAIConversations_AddShares.Designer.cs
```

Same IDs as the `v18` PR's migrations, by design — see "Special things to note" above.
