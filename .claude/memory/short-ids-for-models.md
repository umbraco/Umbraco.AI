---
name: short-ids-for-models
description: When a tool shows a model entity ids, use the shortest unique Guid prefix (min 8), mapped back only within the run's own list, and refuse ambiguous ids
type: decision
---

When an AI tool or prompt lists entities for the model to refer back to (memories today; content,
media, agents or any other Guid-keyed item later), show each one as a short id: the first 8
characters of its Guid, lengthened only where two items in that run's list would otherwise share
one. Map ids back only among the items this run listed or created, and refuse an id that matches
more than one, never guess. Agent memory's internal `AIShortIdMap` is the first implementation.

**Why:** full Guids cost about 28 extra characters per item on every run and are easy for a model
to mistype. A fixed 8-character prefix left a small chance of a clash, so the agent could act on
the wrong item. Per-run numbers (m1, m2) were rejected because the Copilot sidebar rebuilds its
list each run, so a number in an older turn can point at a different item. A stored short-code
column was rejected because it only works for tables that add it. Decided during the agent-memory
design, 26-11-2026 (docs/plans/agent-memory/DECISION-LOG.md).

**How to apply:** for a new tool that lists entities to a model, reuse `AIShortIdMap` rather than
inventing another scheme. When a second product needs it, move it to `Umbraco.AI.Core` as
internal and grant that product `InternalsVisibleTo`. Existing tools that pass full Guids are
candidates to refactor onto it later; don't change them as part of unrelated work.
