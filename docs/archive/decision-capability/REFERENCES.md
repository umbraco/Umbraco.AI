# References

> **Status:** Archived 24-09-2026. Completed spike (all 12 tasks done, never merged). Superseded by the full feature plan in `docs/plans/decision-capability-release/`, which builds on this spike's Core code.

External resources worth checking before anyone revisits this spike — most
usefully alongside `DECISION-LOG.md`'s 2026-09-24 entries, which explain why
each one matters.

## Jev's real API

- [docs.typesafe.ai/api](https://docs.typesafe.ai/api) — the actual wire
  contract (`POST /v1/systemone`, the batch `state`/`questions` request
  shape, the `noul`/`choice`/`score` answer types). Live-confirmed during
  T11; see `DECISION-LOG.md`'s "T11: Jev's real wire format confirmed" entry
  for what didn't match this repo's initial guesses.

## The M.E.AI proposal — the actual go/no-go trigger to watch

- [dotnet/extensions#7764](https://github.com/dotnet/extensions/issues/7764)
  — "[API Proposal]: Add a provider-neutral abstraction for decision-oriented
  AI models." Filed 2026-09-19 (5 days before this spike), explicitly
  motivated by Jev. Proposes `BinaryDecisionQuestion`/`ChoiceDecisionQuestion`/
  `ScoreDecisionQuestion` — the same three-primitive split this spike
  independently arrived at, including "no separate confidence for binary,
  just P(true)."
  - **As of 2026-09-24**: labeled `untriaged`, 3 comments, all from two
    outside contributors (`mo3in`, `joslat` — neither a Microsoft employee),
    no dotnet/Microsoft maintainer response yet. **This is the thing to
    watch** — a scheduled routine polls it twice daily and emails on real
    movement (a maintainer comment, a label/state change, or a linked PR
    directly against `dotnet/extensions`). See `claude.ai/code/routines` for
    that routine if it needs adjusting or disabling.
- [dotnet/extensions#7587](https://github.com/dotnet/extensions/issues/7587)
  — a comparable, still-unresolved API proposal for a *different* new
  capability (`IDocumentExtractionClient`), filed 2026-06-25. Useful as a
  second data point on how long a fresh M.E.AI capability proposal typically
  sits before real movement.

## The Microsoft Agent Framework prototype — NOT the same thing as the above

- [microsoft/agent-framework#8563](https://github.com/microsoft/agent-framework/pull/8563)
  (pull request, open, not merged) — ".NET: Add IDecisionClient
  (experimental), DecisionLoopEvaluator, and the Microsoft.Agents.AI.TypeSafe
  (Jev) provider." A real, substantial PR (5,404 additions across 54 files,
  20 review comments — all from the author replying to an automated bot, no
  human maintainer review yet), explicitly described in its own text as
  living in `Microsoft.Agents.AI.Abstractions` as a throwaway copy, "intended
  to be deleted once MEAI ships the abstraction." **This is a different repo
  from `dotnet/extensions`** — activity here does not by itself mean the
  real M.E.AI proposal (#7764) is progressing; the routine above deliberately
  ignores this repo for that reason.
- [microsoft/agent-framework#8545](https://github.com/microsoft/agent-framework/issues/8545)
  — ".NET: [Feature]: Integrate Microsoft.Extensions.AI decision-model
  inference into agent decision points." Explicitly contingent on #7764
  landing in M.E.AI first.
- [microsoft/agent-framework#8562](https://github.com/microsoft/agent-framework/issues/8562)
  — "Python: .NET: [Feature]: Add IDecisionClient (reference of
  dotnet/extensions#7764), DecisionLoopEvaluator, and a TypeSafe (Jev)
  provider package." The tracking issue #8563's PR was opened against.

## Precedent used for the "how long does this take" estimate

- [dotnet/extensions#6648](https://github.com/dotnet/extensions/issues/6648)
  — "Add support for text-to-image," filed 2025-07-23. The closest
  comparable "brand new M.E.AI capability category" precedent: as of this
  spike (14+ months later), `IImageGenerator` is still marked experimental
  (`MEAI001`) — the same gated, can-break-anytime state this repo's own
  `ImageGeneration` capability is in, and the state this spike's `Decision`
  capability was deliberately built to match.
