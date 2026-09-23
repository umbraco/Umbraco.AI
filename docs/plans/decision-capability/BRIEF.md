# Brief

## Problem

TypeSafe AI shipped a model called Jev on 15-09-2026: a "System 1" model that skips
text generation entirely and returns a typed decision instead — a yes/no
(`Noul`), a pick-one (`Choice`), or an ordinal score (`Score`), each with a
calibrated confidence/probability. It's getting real community traction (a
$40M seed round, an unofficial community .NET SDK already exists). That's the
why-now: it's a new-enough shape of model that it's worth asking whether
Umbraco AI's architecture can represent it at all, while the question is still
cheap to explore.

Today, every "Provider" in Umbraco AI (OpenAI, Anthropic, etc.) wraps a
Microsoft.Extensions.AI (M.E.AI) client type — `IChatClient`,
`IEmbeddingGenerator<string, Embedding<float>>`, `ISpeechToTextClient`. Jev
doesn't fit any of them: it isn't chat, it isn't an embedding, it isn't
speech-to-text. The concrete problem this spike answers: **can a
non-autoregressive, typed-decision model be represented as an Umbraco AI
capability, and if so, how — given M.E.AI itself has no client abstraction
for this shape of model yet, and Umbraco AI's stated philosophy is "thin
wrapper around M.E.AI, no proprietary abstractions"?**

This is an internal architecture spike, not a product feature. The audience
is whoever decides whether Umbraco AI should build real support for this
class of model later — not editors, not package-consuming developers, yet.

### Existing overlap found during scoping

Umbraco AI already has a reserved capability slot for exactly this shape of
output: `AICapability.Moderation = 3` (`Umbraco.AI/src/Umbraco.AI.Core/Models/AICapability.cs`),
described in code and in `docs/reference/capabilities-feature.md` as
"content safety... provide confidence scores for moderation decisions." It
was added 23-11-2025 and has shipped in every release since (current package
version 18.3.5) — it is public API, but it has never been implemented (no
`IAIModerationCapability` interface exists yet).

**Decision made during scoping:** treat "Decision" as the general form of
this reserved slot, rather than adding a second, overlapping capability kind.

**Open question for `umb-design`, not resolved here:** the reserved slot is
named `Moderation` and is already public API (shipped since 2025-11-23,
current version 18.3.5). Per this repo's public-API rule, we cannot rename or
remove the `Moderation` enum member outright. Design needs to decide how a
general "Decision" concept coexists with that already-shipped name — e.g. add
a new `Decision` value and leave `Moderation` unused/redirected, or scope
`Moderation` narrowly (content-safety) and add `Decision` as a sibling after
all. TODO.

## Success looks like

A working, throwaway code spike — not just a written verdict. Concretely: a
minimal, disposable capability/provider that can round-trip a real typed
decision through Jev (e.g. answer a yes/no `Noul` question) via some client
abstraction sitting where `IChatClient` sits for Chat today. It doesn't need
to be production-shaped, doesn't need tests, and is expected to be thrown
away or heavily reworked if this goes further.

## The central open question: the M.E.AI gap

M.E.AI has no client type for typed yes/no/choice/score decisions today.
Umbraco AI's whole "thin wrapper, no proprietary abstractions" philosophy
(see `Umbraco.AI/CLAUDE.md`, `docs/internal/integration-philosophy.md`)
assumes M.E.AI already has the shape it needs to expose. It doesn't, here.

Decision from scoping: **do a bit of both.** The spike is free to sketch an
Umbraco-AI-specific client abstraction for this shape (since none exists
upstream to wrap), but it must be gated behind an experimental feature flag —
matching the precedent already set for `AICapability.ImageGeneration`
(`Umbraco:AI:Experimental:ImageGeneration`, default off) — rather than
presented as a real, stable capability. If M.E.AI (or Microsoft) ever ships
an official abstraction for this model shape, the expectation is Umbraco AI
would migrate to it and drop the proprietary one.

## Non-goals

- **No real `Umbraco.AI.TypeSafe` provider package.** Whatever gets built is
  throwaway, not a package meant to ship.
- **No backoffice UI work.** No Profile/Connection editor changes for this
  capability.
- **Not solving the .NET version mismatch.** The only current Jev .NET SDK
  (`RavenValentin/TypeSafe.Jev`, community-maintained, described by its own
  author as "vibe-coded") targets .NET 11 / C# 15; Umbraco AI targets
  `net10.0`. This spike doesn't need to resolve that — if the SDK can't be
  referenced directly, calling Jev's HTTP API directly for the spike is fine.
- **Not deciding the final shape of the `Moderation`/`Decision` naming
  question.** Recorded as open above, handed to `umb-design`.
- **Not evaluating whether Jev's own performance/cost claims are true.** The
  user considers that already validated by the community independently.

## Riskiest unknowns

1. **The M.E.AI gap itself** (see above) — the biggest open architectural
   question, not fully resolved here by design.
2. **Public API constraint on `AICapability.Moderation`** — already shipped,
   can't be renamed/removed; design must work around this, not through it.
3. Whether this is even the right *level* to add support at — a capability
   is a fairly deep architectural commitment (it flows through Providers,
   Connections, Profiles, middleware). A spike-only, flagged/experimental
   capability keeps this cheap to abandon, per the kill criterion below.

## What would kill this

Difficulty of building a custom capability that isn't backed by a current
M.E.AI feature. If sketching the client abstraction turns out to require a
lot of new plumbing (new middleware pipeline shape, new Profile/Connection
concepts, etc.) just to represent three primitive answer types, that's a
signal this isn't worth pursuing further right now — better to wait for
M.E.AI or the broader ecosystem to converge on a real abstraction first.

## Cross-version note

Not resolved here, flagged for whoever picks this up next: capability-level
architecture in `Umbraco.AI.Core` is shared, non-version-specific work (see
root `CLAUDE.md`, "Keep Active Versions in Sync"). If this spike leads to a
real feature later, it will need the same "port to the other active line"
conversation as any other core architecture change — not a concern for the
spike itself, which lives only on `v18/dev`.
