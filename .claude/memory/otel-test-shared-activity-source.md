---
name: otel-test-shared-activity-source
description: Tests with an ActivityListener on AITelemetry.SourceName must filter by span name, since every capability's middleware shares the "Umbraco.AI" source
type: gotcha
---

A test that captures spans with an `ActivityListener` on `AITelemetry.SourceName` must filter
`ActivityStopped` by the span's `OperationName` (e.g. `"gen_ai.decision"`). It must not just
keep the last activity it sees.

**Why:** chat, embedding, image-generation, speech-to-text and decision OpenTelemetry
middleware all emit on the one shared `"Umbraco.AI"` source. `ActivitySource.AddActivityListener`
is process-wide, and xUnit runs test classes in parallel, so an unfiltered listener can capture
another class's span and flake. Caught in review on the decision-capability-release build (T2).

**How to apply:** whenever writing or reviewing a test that subscribes an `ActivityListener`
to the Umbraco AI source, filter on the exact span name the middleware passes to
`StartActivity`.
