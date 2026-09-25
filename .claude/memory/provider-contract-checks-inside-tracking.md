---
name: provider-contract-checks-inside-tracking
description: A check that a provider's response is valid must throw inside the tracking middleware (e.g. the AIErrorClassifying* client), never in the service after the pipeline returns
type: gotcha
---

A check that rejects a provider's *response* (wrong shape or type, missing required data) must
throw from a client that sits **inside** the tracking/telemetry middleware. In practice that's
the capability's `AIErrorClassifying*Client`, which the factory places directly around the
provider's base client. It must not throw from the service after the client pipeline has
returned.

**Why:** `AITracking*Client` records usage and audit as succeeded the moment its inner call
returns. A service-level check that throws afterwards leaves three contradicting records: the
audit says success, the executed notification says failure, and the caller gets an exception.
It also skips callers who use the client factory directly. Caught in review on the
decision-capability-release build (T5).

**How to apply:** caller-input validation goes outermost (outside tracking, so it's never
counted as a provider failure). Provider-output validation goes innermost (inside tracking, so
it is). Write a factory-level test that goes through a real tracker and asserts a failure was
recorded.
