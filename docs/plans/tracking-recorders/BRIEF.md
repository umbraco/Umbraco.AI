# Brief

Source: issue [#528](https://github.com/umbraco/Umbraco.AI/issues/528), opened 08-10-2026.

## Problem

`AIOperationTracker` and `AIOperationScope` are meant to track an AI call. Together they also do
the recording for everything that consumes that tracking:

- time the call and decide how it ended (tracking)
- capture the call's identity: profile, provider, model, feature (tracking)
- create the audit entry, find its parent, queue its start and end status (audit)
- tag the current OpenTelemetry `Activity` (tracing)
- feed the test-run usage collector (`AIUsageCollectionScope`)
- build and queue the usage analytics record (analytics)

Because each output is wired in separately, the same facts are worked out in several places with
different rules. Mapping the pipeline for #528 found duration measured in up to 8 places, token
usage mapped 4 different ways for "no data", identity read by 3 extractors with different field
sets, and success or failure decided separately by tracker, audit, analytics, notifications and
OpenTelemetry. Three real bugs came out of the same mess (#529, #530, #531). Two are fixed.

**What a developer does today:** to add or change one kind of recording, they edit the tracker
and the operation scope, which every AI call goes through, and hope the other recordings still
line up.

### Who it's for

- Maintainers of `Umbraco.AI.Core`. This is internal plumbing. No public API changes.

## Goal

The tracker keeps three jobs:

1. time the call
2. capture its identity once
3. decide one outcome: succeeded, failed or blocked

Everything that stores or forwards that information becomes an internal **recorder**: audit,
analytics, the test-run counter and Activity tagging. A recorder can drop or reshape what the
tracker hands it, but never measures anything again.

## Out of scope

- Public recorder or notification extension points. They can be added later if a real need
  shows up. Shared plumbing stays internal; only deliberate hook points are public.
- Changing what the existing public Executed notifications carry.
- The usage dashboard and aggregation jobs (#530, fixed separately).
- Nested calls overwriting the parent's runtime context (#524). The tracker already captures
  identity at the start; the shared context itself is a separate fix.

## Constraints

- Every AI call goes through this code. Each step must leave behaviour unchanged unless the step
  says otherwise, and the existing tracker, client and collector tests must keep passing
  unedited, apart from construction changes.
- `IAIOperationTracker.TrackAsync` / `BeginAsync` keep their shape. The tracking clients (chat,
  embedding, speech to text, image, and the Decision capability in #419) must not need changes
  beyond what a step lists.
- Both active lines (v18, v17). v18 first, ported once the v18 PR is approved.
