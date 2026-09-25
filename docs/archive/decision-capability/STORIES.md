# Stories

> **Status:** Archived 24-09-2026. Completed spike (all 12 tasks done, never merged). Superseded by the full feature plan in `docs/plans/decision-capability-release/`, which builds on this spike's Core code.

## Definition of Ready / Definition of Done

This is a throwaway architecture spike (`docs/plans/decision-capability/BRIEF.md`), so these
are lighter than a shipping feature's:

**Ready** — role/capability/value stated (even where the "role" is an internal maintainer,
not an editor), Given/When/Then acceptance criteria cover the happy path, out-of-scope is
explicit, passes INVEST.

**Done** — all acceptance criteria pass as executable specs via `bdd-specs`; sad-path
criteria (validation errors, disabled-flag behavior) covered; the whole surface is inert
(`Umbraco:AI:Experimental:Decision` defaults `false`) so merging this to `v18/dev` changes no
observable behavior for anyone who hasn't opted in. No backport to `v17/dev` — this stays on
`v18/dev` only, per the brief.

---

## DC-1 — Decision capability exists, but only when explicitly enabled

As an Umbraco AI maintainer,
I want a new `Decision` capability value that's invisible and unusable until explicitly
turned on,
so that adding it can't change behavior for any existing connection, profile, or provider.

### Acceptance criteria

**AC1 — New enum member, existing one untouched**
```
Given  Umbraco.AI.Core.Models.AICapability
When   the enum is inspected
Then   it has a member Decision with underlying value 8
And    the existing Moderation member (value 3) is unchanged
```

**AC2 — Disabled by default**
```
Given  a default AIExperimentalOptions (no configuration set)
When   AIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision) is called
Then   it returns false
```

**AC3 — Enabled when the flag is set**
```
Given  AIExperimentalOptions.Decision is set to true
When   AIExperimentalFeatures.IsCapabilityEnabled(AICapability.Decision) is called
Then   it returns true
```

**AC4 — Hidden from a connection's advertised capabilities while disabled**
```
Given  a connection whose provider declares a Decision capability
And    AIExperimentalOptions.Decision is false (default)
When   the connection's capabilities are resolved (AIConnectionService)
Then   AICapability.Decision is not present in the result
```

**AC5 — No connections reported for the capability while disabled**
```
Given  a connection whose provider declares a Decision capability
And    AIExperimentalOptions.Decision is false (default)
When   AIConnectionService.GetConnectionsByCapabilityAsync(AICapability.Decision, ...) is called
Then   it returns an empty list
```

**AC6 — Profile creation rejected while disabled**
```
Given  AIExperimentalOptions.Decision is false (default)
When   a profile with Capability = AICapability.Decision is created via AIProfileService
Then   an InvalidOperationException is thrown
And    its message tells the caller to enable Umbraco:AI:Experimental:Decision
```

**AC7 — All three unlocked once enabled**
```
Given  AIExperimentalOptions.Decision is true
When   the same three operations in AC4–AC6 are repeated
Then   the capability is visible, connections are reported, and profile creation succeeds
       (subject to the provider actually declaring the capability)
```

### Out of scope

The actual `IAIDecisionClient` contract (DC-2), the capability plumbing that uses this gate
(DC-3).

---

## DC-2 — A typed decision has one consistent shape, whatever kind of question was asked

As a capability provider author building against `IAIDecisionClient`,
I want `AskAsync` to validate its input and shape its output the same way for every
provider,
so that callers can trust the response's shape without knowing which provider answered.

### Acceptance criteria — happy path

**AC1 — Binary**
```
Given  an AIDecisionQuestion with Kind = Binary and a non-empty Prompt
When   AskAsync resolves
Then   the response's Kind is Binary
And    BinaryAnswer is non-null
And    SelectedChoice and Score are both null
And    Confidence is present and within [0.0, 1.0]
```

**AC2 — Choice**
```
Given  an AIDecisionQuestion with Kind = Choice, a non-empty Prompt, and Choices containing
       at least 2 entries
When   AskAsync resolves
Then   the response's Kind is Choice
And    SelectedChoice is non-null and is one of the question's Choices
And    BinaryAnswer and Score are both null
And    Confidence is present and within [0.0, 1.0]
```

**AC3 — Score**
```
Given  an AIDecisionQuestion with Kind = Score and a non-empty Prompt
When   AskAsync resolves
Then   the response's Kind is Score
And    Score is non-null
And    BinaryAnswer and SelectedChoice are both null
And    Confidence is present and within [0.0, 1.0]
```

**AC4 — Score respects a supplied range**
```
Given  an AIDecisionQuestion with Kind = Score and a ScoreRange of (Min, Max)
When   AskAsync resolves
Then   the response's Score falls within [Min, Max]
```

### Acceptance criteria — sad path

**AC5 — Choice with too few options is a caller error**
```
Given  an AIDecisionQuestion with Kind = Choice and Choices containing fewer than 2 entries
       (null, empty, or a single entry)
When   AskAsync is called
Then   an ArgumentException is thrown before any provider request is made
```

**AC6 — Empty prompt is a caller error**
```
Given  an AIDecisionQuestion with an empty or whitespace-only Prompt, for any Kind
When   AskAsync is called
Then   an ArgumentException is thrown before any provider request is made
```

### Out of scope

Which real provider answers the question (DC-4); streaming or batched questions (not in
`ARCHITECTURE.md`'s scope at all).

---

## DC-3 — A Decision client goes through the same pipeline every other capability uses

As a developer consuming `IAIDecisionService`,
I want a Decision capability's client wrapped with the same per-model settings enforcement
and usage tracking every other capability gets,
so that a Decision profile behaves consistently with Chat/Embedding/SpeechToText profiles
instead of being a special case.

### Acceptance criteria

**AC1 — Per-model settings enforced**
```
Given  an AIDecisionCapabilityBase-derived capability whose GetSettingsSupport declares a
       setting unsupported for a given model
When   a client is created for that model via IAIDecisionCapability.CreateClientAsync
Then   the returned client rejects (or strips, per the existing DeclaredSettings pattern)
       that unsupported setting on every request through it
```

**AC2 — Usage is tracked**
```
Given  a resolved Decision profile
When   a request is made through IAIDecisionService.AskAsync
Then   AITrackingDecisionMiddleware records the request's usage/audit information
       the same way AITrackingSpeechToTextMiddleware does for SpeechToText
```

**AC3 — Profile-alias resolution**
```
Given  a Decision profile saved under a known alias
When   IAIDecisionService.AskAsync(string, AIDecisionQuestion, ...) is called with that
       alias
Then   it resolves the profile, builds the client via the capability's factory, and returns
       an AIDecisionResponse
```

(Final shape: `IAIDecisionService.AskAsync` has three overloads — `AskAsync(Guid profileId, ...)`,
`AskAsync(string profileAlias, ...)`, and the builder-based
`AskAsync(Action<AIDecisionBuilder> configure, ...)`. There is no single "ID or alias" parameter
type — `IdOrAlias` doesn't exist in Core; see T8's `DECISION-LOG.md` entry.)

### Out of scope

The concrete answer content (DC-2 defines the contract; DC-4 supplies a real provider).

---

## DC-4 — The spike proves the whole path against a real Jev decision

As the person running this architecture spike,
I want a disposable provider that asks the real Jev API a yes/no question through the full
Decision pipeline,
so that I have real evidence — not just a design on paper — for whether this shape of model
fits Umbraco AI's capability architecture.

### Acceptance criteria

**AC1 — End-to-end round trip**
```
Given  a disposable [AIProvider]-attributed provider implementing AIDecisionCapabilityBase,
       calling Jev's HTTP API directly via HttpClient (no community SDK reference)
And    a Decision profile configured against it
And    Umbraco:AI:Experimental:Decision set to true
When   IAIDecisionService.AskAsync is called with a Binary question (e.g. "is this text
       spam?") against real Jev credentials
Then   it returns an AIDecisionResponse with Kind = Binary, a non-null BinaryAnswer, and a
       Confidence within [0.0, 1.0]
```

**AC2 — Inert when the flag is off**
```
Given  the same provider and profile as AC1
And    Umbraco:AI:Experimental:Decision left at its default (false)
When   any part of the pipeline is exercised (profile creation, capability listing, or
       AskAsync)
Then   it behaves exactly as DC-1's AC4–AC6 describe — invisible, unconnectable, uncreatable
```

### Out of scope

`Choice`/`Score` against Jev (exercised only if time allows, per `SPEC.md`); packaging this
as a real, shippable `Umbraco.AI.TypeSafe` provider; anything the community's own SDK/tests
already validated about Jev's own performance or accuracy claims.
