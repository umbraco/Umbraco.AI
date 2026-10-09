# Test grader run usage

Read these in order:

1. [x] [BRIEF.md](./BRIEF.md) — Graders get no token usage or model info for a test run; supply summed usage plus a per-model breakdown without breaking graders (#516).
2. [x] [ARCHITECTURE.md](./ARCHITECTURE.md) — Internal AsyncLocal collector fed by the operation tracker; breakdown stored on AITestTokenUsage, no migration, no grader API change.
3. [x] [SPEC.md](./SPEC.md) — New usage fields graders read via `outcome.TokenUsage`, 12 testable behaviors, additive API response fields, no UI code change.
4. [x] [STORIES.md](./STORIES.md) — 3 stories: run totals, per-model breakdown, compatible contract (22 criteria).
5. [x] [PLAN.md](./PLAN.md) — 7 tasks; T1 (types) and T2 (collector) run first in parallel, T7 is the v17 backport.
6. [x] [BUILD-LOG.md](./BUILD-LOG.md) — T1-T6 built and verified (5 code commits, 2 review fix rounds, live demo check passed); T7 v17 backport pending.

See [DECISION-LOG.md](./DECISION-LOG.md) for why things changed along the way.
