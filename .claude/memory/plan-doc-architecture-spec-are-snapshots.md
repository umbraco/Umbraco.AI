---
name: plan-doc-architecture-spec-are-snapshots
description: BRIEF.md/ARCHITECTURE.md/SPEC.md describe current state only; revision history belongs in DECISION-LOG.md
type: decision
---

Applies to `BRIEF.md` (`umb-explore`) and `ARCHITECTURE.md`/`SPEC.md` (`umb-design`) — see the
root `CLAUDE.md`'s Feature Planning section for the `docs/plans/<feature-slug>/` convention. When
revising any of these — whether from user feedback, a re-read of the actual code, a later phase
bouncing back a question, or a gap found after the fact — **rewrite the affected section to
describe only the current approach.** Do not leave the old approach in place next to a note about
what changed, and do not narrate the revision in-line ("corrected from the first pass," "reversed
after the user pushed back," "as originally scoped here," etc.).

**Why:** established while designing the `share-conversations` feature; confirmed it applies
beyond `umb-design` specifically when a post-plan gap (a missing notification requirement)
required renumbering tasks in `PLAN.md` and updating `STORIES.md`'s acceptance criteria. That
narration belongs in `DECISION-LOG.md` instead — a dated entry, the old approach, why it changed,
one line each. `DECISION-LOG.md` is the changelog; every other plan-folder doc is what a reader (a
later phase, `umb-build-loop`, or a teammate picking this up cold) should be able to read once,
straight through, and get the actual current state — not its history.

**How to apply:** this includes renumbering — if a mid-list insertion (e.g. a new `PLAN.md` task)
would otherwise leave a gap or an out-of-order id, renumber the whole list rather than bolting the
addition on with a suffixed id (`SC-04a`). A clean sequential list stays readable; `DECISION-LOG.md`
is where the "why inserted here" reasoning lives.
