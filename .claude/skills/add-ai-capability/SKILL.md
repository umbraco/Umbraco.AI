---
name: add-ai-capability
description: Add a new AICapability to Umbraco.AI.Core (e.g. Media, Moderation, TextToSpeech, or any future capability) — the file-for-file checklist and non-obvious rules for the Core plumbing, plus a pointer to the Deploy/Management-API/frontend work a real shippable capability also needs. Use when scaffolding a brand-new capability, not a new provider (see add-provider for that).
---

# Adding a new AI capability to Umbraco.AI.Core

Distilled from building the `Decision` capability (docs/plans/decision-capability/,
2026-09). Nine of that build's twelve tasks needed at least one real fix-and-re-review
round — not style nits, actual bugs — almost always because a step below was skipped or
done in the wrong order. This is not a "nice to have" checklist; it's the list of things
that actually cost a review round each time they were missed.

> **Scope of what's below, read this first.** The `Decision` build this skill is drawn
> from was an explicit architecture *spike* — its own brief named "no backoffice UI, no
> Management API" as non-goals. Everything through "Commit hygiene" below is
> `Umbraco.AI.Core`-only, and every claim in that part was validated by an actual build
> (or, for the two gotchas, by a real review catching a real bug). The final section,
> **"Beyond Core,"** covers what `Umbraco.AI.Deploy`, the Management API, and the
> frontend also need for a real, shippable capability — that section was assembled by
> reading the existing `ImageGeneration`/`SpeechToText` code afterwards, on request, and
> was **not exercised by a build**. Treat it as a map of where to look, not a
> battle-tested checklist the way the Core section is.

## Before writing any code: verify the design against real files, not memory

**The single biggest source of rework in this build was writing a signature or type
shape into `ARCHITECTURE.md`/`SPEC.md` from a *remembered* pattern instead of an *opened*
file.** Concretely: a design doc said the new service should take an `IdOrAlias`
parameter, because that's how the root `CLAUDE.md`'s "IdOrAlias Pattern" section
describes profile lookup — except that type lives in `Umbraco.AI.Web`, which
`Umbraco.AI.Core` cannot reference (circular dependency), and the actual Core-layer
services (`IAIChatService`, `IAISpeechToTextService`) use plain `Guid`/`string`
overloads or builder `WithProfile(Guid)`/`WithProfile(string)` methods instead. This
wasn't caught until a builder tried to actually compile it three tasks later.

**Rule:** whenever a design doc is about to say "this mirrors how `[sibling capability]`
does X" for anything more specific than folder/file structure — a method signature, a
settings-mutation pattern, a shared type — open that sibling's real source file and
confirm, right then, before writing it down. `IAIChatService.cs`, `AISpeechToTextService.cs`,
and `IAISpeechToTextService.cs` are the fastest sources of truth for "how does Umbraco AI
actually do X today."

## If the capability needs a type M.E.AI doesn't provide: mirror M.E.AI's own conventions, not idiomatic C#

If no `Microsoft.Extensions.AI` type exists for the new capability's request/options/response
shape (true for anything genuinely novel — check first, most capabilities *do* have an
M.E.AI type to wrap), the new proprietary type must follow M.E.AI's own conventions for
that category, not "good modern C#" defaults:

- **Options types are mutable classes with a `Clone()` method**, like `ChatOptions`,
  `SpeechToTextOptions`, `EmbeddingGenerationOptions` — **not an immutable record with
  `with {}`**. The two-parameter capability base's `ApplyCapabilitySettings` hook needs an
  object it can actually mutate in place; an immutable record forces the hook to return a
  new value instead, which then requires every wrapper in the chain to thread that return
  value through — a different, non-standard shape from every other capability. Building
  this the record way first cost 2 extra review rounds before landing on the mutable-class
  shape.
- Mirror the exact `Clone()` idiom: `public MyCapabilityOptions Clone() => new() { Field
  = Field, ... };` — every field copied explicitly, one place to update when a field is
  added.
- Question/request-shape types that are never mutated mid-flight (nothing applies
  settings to them) don't need this — a plain sealed class with init-only properties is
  fine. Only the *options* type that flows through `ApplyCapabilitySettings` needs the
  mutable-class-with-`Clone()` treatment.

## File-for-file mirror, in this order (copy the most recently-added sibling capability, currently `SpeechToText/`)

1. **Core types** (own feature folder, e.g. `Decision/`): the client interface
   (`IAI<Foo>Client`), request/question type, options type (see above), response type,
   and a `<Foo>Diagnostics` static class holding `DiagnosticId = "UMBRACOAI_<FOO>"`.
2. **Sad-path validation wrapper** (e.g. `ValidatingDecisionClient`): enforces the
   capability's own input-shape rules before any request reaches a provider. Built and
   tested in isolation first — but see the wrapping-order gotcha below before assuming
   it's wired in correctly later.
3. **`IAI<Foo>Capability` + `AI<Foo>CapabilityBase` (three arities)** in
   `Providers/IAICapability.cs`, mirroring `IAISpeechToTextCapability`/
   `AISpeechToTextCapabilityBase<...>` exactly: no-settings base, `<TSettings>` base,
   `<TSettings, TCapabilitySettings>` base with the `ApplyCapabilitySettings` hook.
4. **`DeclaredSettings<Foo>Client` / `CapabilitySettings<Foo>Client<T>`** in `Providers/`,
   mirroring the SpeechToText equivalents — these wrap the client so per-model settings
   declarations are enforced on every request.
5. **Remaining feature-folder plumbing**: `IAI<Foo>ClientFactory`/`AI<Foo>ClientFactory`,
   `IAI<Foo>Middleware` + `AI<Foo>MiddlewareCollection(Builder)`, `AITracking<Foo>Client` +
   `AITracking<Foo>Middleware`, `AIOpenTelemetry<Foo>Middleware`,
   `AIErrorClassifying<Foo>Client`, `AI<Foo>Executing/ExecutedNotification`,
   `Scoped<Profile/Inline><Foo>Client`.
6. **`IAI<Foo>Service`/`AI<Foo>Service`**, mirroring `IAISpeechToTextService`: typed
   `Guid`/`string` overloads (not `IdOrAlias` — see above) that delegate into a builder
   overload, plus the builder overload itself if the capability needs the notification
   lifecycle (see below).
7. **DI registration** in `Configuration/UmbracoBuilderExtensions.Collections.cs` (the
   middleware collection-builder accessor, `[Experimental]`-marked the same way
   `AIImageGenerationMiddleware()` is) and `Configuration/UmbracoBuilderExtensions.cs`
   (the actual service registrations — unconditional/singleton; the experimental gate
   lives *only* in `AIExperimentalFeatures`, never at registration time).

## Two gotchas that don't show up until real integration

- **The validating/sad-path wrapper (step 2) must sit OUTSIDE the error-classifying
  client and the tracking/telemetry middleware, not innermost.** Copying the literal
  SpeechToText wrapping order gets this backwards, because none of the existing siblings'
  `Scoped*Inline*` wrappers ever sit on an execute path the way this one might — there's
  no precedent forcing the order to be obviously right. If a caller's validation error
  (e.g. `ArgumentException`) passes through the error classifier first, it gets caught
  and rethrown as a provider failure, which is wrong and also gets mis-logged as a
  provider error. **Write a factory-level regression test that builds a client through
  the real factory (not the wrapper class in isolation) and asserts an invalid request
  throws the caller's own exception type, not the provider-failure type** — this is the
  only kind of test that actually catches a regression here.
- **If a client makes HTTP calls, take `IHttpClientFactory` in the provider's
  constructor and call `.CreateClient()` — never `new HttpClient()`.** Every existing
  HTTP-calling provider (`FireworksAIProvider`, `OpenRouterProvider`,
  `MicrosoftFoundryProvider`) does this; the returned client is factory-pooled, so the
  wrapping `IAI<Foo>Client.Dispose()` should be an empty no-op, not a call to
  `httpClient.Dispose()`.

## The experimental gate needs zero extra code in `AIConnectionService`/`AIProfileService`, but prove it

`AIConnectionService`'s capability listing and `AIProfileService`'s profile-create path
both key off `IAICapability.Kind` and `IExperimentalFeatures.IsCapabilityEnabled(kind)`
generically already — adding `AICapability.<Foo>` to the enum plus one switch-case in
`AIExperimentalFeatures` is enough; **no new code is needed in either service.** But
write the proof rather than assuming it:
- Use a **real** `AIExperimentalFeatures` backed by a mocked `IOptionsMonitor<AIExperimentalOptions>`
  — not a mocked `IAIExperimentalFeatures` — or the test only proves the service calls
  the interface, not that the real flag actually gates it.
- Cover **both** the disabled side (hidden from listing, empty from connections-by-capability,
  profile creation rejected) **and** the enabled side (positive controls) — a
  disabled-only test suite would still pass even if the capability were never wired into
  the provider at all.
- Once a real provider for the capability exists, repeat the same disabled/enabled pair
  against that specific real provider too, not just a minimal test-double — a generic
  proof and a proof against the actual shipping provider are different guarantees.

## If the capability needs an inline/notification-based execution path

Match whichever rule the *execute* path uses on a sibling capability, not the rule its
`Scoped*Inline*` wrapper class uses on a *create-client* path — those two rules can look
similar but are not the same, and picking the wrong one is silent (no compile error, just
wrong runtime behavior). Concretely: feature-metadata stamping (`FeatureType`/`FeatureId`
on the shared runtime context) is decided by `!builder.IsPassThrough` on `AIChatService`'s
and `AISpeechToTextService`'s execute paths — not by `!scopeExisted`, which is what their
`Scoped*Inline*` wrapper classes use, but only because those wrappers have never before
sat on an execute path themselves. The two rules disagree in two cases, not one: a
pass-through call with no parent scope, and a normal call made from *inside* an existing
parent scope (e.g. an agent run) — write a test for both, not just the first one that
comes to mind.

## Commit hygiene

See `umb-build-loop-gotchas` for the general rule (commit each pending spec with the
production code that first makes it pass) — it bit this build twice on exactly this kind
of feature.

## Beyond Core: what a real, shippable capability also needs

*(Found by reading the existing codebase after the fact — not exercised by a build.
Treat every path/pattern below as a starting point to verify, not a finished recipe.)*

- **`Umbraco.AI.Deploy`**: `AISettingsArtifact.cs` and
  `UmbracoAISettingsServiceConnector.cs` carry a per-capability default-profile UDI field
  (`DefaultSpeechToTextProfileUdi`, `DefaultImageGenerationProfileUdi`) that gets mapped
  to/from the corresponding `AISettings.Default<Foo>ProfileId` on export/import. A new
  capability that gets its own "default profile" setting needs the matching pair of
  fields added here, or Deploy silently drops it. (Also see the `project_imagegen_deploy_gap`
  memory entry — ImageGeneration exposed a real gap here, later fixed.)
- **Management API (`Umbraco.AI.Web`)**: existing capabilities each get their own
  controller folder, e.g. `Api/Management/SpeechToText/Controllers/TranscribeSpeechToTextController.cs`,
  `Api/Management/ImageGeneration/Controllers/ImageGenerationControllerBase.cs` — plus
  response models alongside them. Capability-listing/mapping code elsewhere in Web
  (`Api/Management/Connection/Controllers/CapabilitiesConnectionController.cs`,
  `Api/Management/Provider/Mapping/ProviderMapDefinition.cs`) also reads
  `IsCapabilityEnabled`/`AICapability` and may need a case added for the new one.
- **Frontend (`Client/src/`)**: existing capabilities each get a `repository/` +
  `controllers/` pair for calling that Management API (e.g.
  `speech-to-text/repository/speech-to-text.repository.ts`), plus a dedicated per-capability
  profile-settings editor element under
  `profile/workspace/profile/views/settings/<capability>-profile-settings.element.ts`. See
  the `project_capability_settings` memory entry — the whole capability-settings series
  across every existing capability is the real precedent to read before building this
  part, not a guess from these two file paths alone.

If a task is specifically "make this capability real and shippable," treat the section
above as a starting checklist of areas to go verify against the current codebase — the
same way the Core section above was verified — rather than as something already proven
to work.
