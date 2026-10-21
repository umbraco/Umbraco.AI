# Decision Log

- 20-10-2026: First version runs on Cloudflare Clairic plus a chat fallback. Clairic alone would
  need every customer to have a Cloudflare account; fallback alone loses the speed and cost pitch.
- 20-10-2026: Ships on v18 and v17 together, like Decision. Automate customers on the LTS line are
  part of the upsell.
- 20-10-2026: TypeSafe is not a provider. Its API has no value-extraction question type (checked
  docs.typesafe.ai/api).
- 20-10-2026: Extraction is its own capability, not new Decision question types. The answer shapes
  and providers are different.
- 20-10-2026: The Automate "Extract Values" action is part of the first release, not a follow-up.
  It is the reason for the feature.
- 20-10-2026: Chat fallback downgraded to nice-to-have. If design finds it messy or costly, defer it
  to a follow-up. Cloudflare + the Automate action is the minimum release.
- 20-10-2026: Extraction copies Decision file for file (capability 9, own experimental flag, own
  client). No migrations needed.
- 20-10-2026: One field subclass per type with a typed answer, not one class with a type property.
  Keeps the single-field `ExtractAsync<TAnswer>` cast-free, same reason Decision chose it.
- 20-10-2026: Numbers are decimal (mostly money); durations stay a number plus unit (months and
  years don't fit TimeSpan); list answers are strings in v1.
- 20-10-2026: Chat fallback deferred. Needs a two-capability profile picker and a strict-schema test
  matrix across 15+ providers, and loses confidence scores. If revived: consumer-side in the
  service, like DecisionAgentSelector → LLMAgentSelector. A pseudo-capability on every chat provider
  was rejected.
- 20-10-2026: Management API gets one batch `extract` endpoint, unlike Decision's single-question
  `ask`, because extraction is almost always several fields over one text.
- 20-10-2026: Cloudflare connection check uses the free model-search call, not a paid probe like
  TypeSafe's.
- 20-10-2026: Automate outputs always include every alias with `found`, unlike Ask Questions which
  leaves missing answers out, so bindings and If checks always resolve.
- 20-10-2026: Out of v1: estimate/forecast types, images, null threshold, guardrails and graders.
- 20-10-2026: The real Clairic wire shape is checked in the first wire task (T9), before the
  OpenAPI client is generated. The docs never show a REST response, so the frontend waits until
  the backend shape is proven.
- 20-10-2026: The Automate action waits only for the Core service (T3), not the provider, so it
  can be built against the fake client while the Cloudflare client is written.
- 20-10-2026: The Danish-culture number check is a recorded result in T19, not a pass/fail gate.
  The fix belongs to Automate; we raise an issue if it fails.
- 20-10-2026: v17 port and docs start only after the v18 PR is open (T21), same order as Decision.
- 20-10-2026: Pending specs are written to `specs/` inside this plan folder, mirroring their final
  test-project paths, not straight into the test projects. They reference types that don't exist
  yet, so in a test project they would break the trunk build. Each `feature-build` task moves its
  spec files into place and removes the `Pending` skip. EX-11, EX-13 and EX-14 are checked live
  (wire tasks, port, docs), so they get no unit specs.
- 20-10-2026: `AIExtractionExecutedNotification` keeps Decision's shape, with no response on it.
  EX-8 AC6 now checks the handler runs once with the call marked successful.
