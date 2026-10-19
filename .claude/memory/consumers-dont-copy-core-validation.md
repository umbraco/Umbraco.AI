---
name: consumers-dont-copy-core-validation
description: An add-on calling a Core service must not re-implement Core's input bounds; let Core's ArgumentException surface and map it, and test that with a mock that throws
type: gotcha
---

An add-on (Automate action, controller, agent tool) that calls a Core service must not copy
Core's own input bounds (counts, lengths, allowed ranges) into its own checks. Core's validator
is the single source; the add-on catches the `ArgumentException` it throws and maps it (e.g. to
`StepRunErrorCategory.Validation`). Only rules the add-on owns go in the add-on (its own field
formats, its own limits, settings Core never sees).

**Why:** two copies drift, so the add-on silently accepts or rejects differently from Core.
Caught twice on the decision-capability-release build: first-round Automate actions, then T37's
"Ask questions" action, which copied the 2..255 option / 2..10 level bounds to make a
mocked-service test pass.

**How to apply:** when a spec says "invalid input fails with no provider call", the provider is
the AI provider, which Core's outermost validating client already guarantees. Test the add-on's
mapping by having the mocked service throw `ArgumentException` and asserting the mapped result,
as `DecisionActionsTests` does for a one-option pick-one.
