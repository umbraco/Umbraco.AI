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
- 21-10-2026: The experimental-capability save error now names the exact key
  (`Umbraco:AI:Experimental:<Capability>`), built from the enum name. This relies on each option
  property having the same name as its enum member. (T1)
- 21-10-2026: The list item enum is `AIExtractionListItemType`, not `AIListItemType`, so it reads
  as part of Extraction. `AIDurationUnit` keeps its name. (T2)
- 21-10-2026: `IsFound` is worked out from the value (null, or an empty list), never set by a
  provider. The answer checker also rejects confidence or null probability outside 0 to 1, a null
  list, and a duration in a different unit from the one asked for. (T2)
- 21-10-2026: The `EnabledCapabilitiesController` entry moved from T6 into T3. Adding
  `IAIExtractionCapability` makes the capability surface test fail without it, and no red test is
  committed. T6 runs straight after T3 to close the short gap where a settings save drops a stored
  Extraction default. (T3)
- 21-10-2026: The builder overloads default the alias to "extraction" when the caller doesn't set
  one. Decision's builder throws instead. Single-field calls get an id of "f" plus a GUID, because
  the field id must start with a letter. (T3)
- 21-10-2026: `AIExtractionService` checks the experimental flag itself and throws before any
  profile lookup, event or provider call. Decision's service has no such check. (T3)
- 21-10-2026: The extract endpoint reuses the shared `UsageModel` (`inputTokens`, `outputTokens`,
  `totalTokens`), not the `inputTokenCount` names first written in SPEC. Matching the existing API
  wins; SPEC fixed. (T8)
- 21-10-2026: Duration unit and list item type are lowercase strings on the wire, read
  case-insensitively. A bad value is a 400. `confidence`, `nullProbability` and `span` are left out
  when empty; `value` is written as null when not found. (T8)
- 21-10-2026: The Cloudflare client leaves a missing answer out instead of throwing, so Core's
  answer checker reports it with a clear message. An unasked answer is passed through as a bare text
  answer with its raw JSON, for Core to reject. A 2xx body with `success:false` throws
  `InvalidOperationException` (an Unknown error, not a network error). Error text has the token
  masked, then is cut to 500 characters. (T5)
- 21-10-2026: The Clairic model is fixed in the URL; `options.ModelId` is ignored. A datetime with
  `Z` or an offset comes back as UTC, otherwise `Unspecified`. (T5)
- 21-10-2026: Unconfirmed Clairic wire-shape guesses for T9 to check: request `fields` keyed by id
  with `items.type` and lowercase plural duration units, `reference_date`; response `answers`,
  `p_null`, `span`/`spans`, `model`; ISO date/time strings; 3040/3036 errors as 429 with `errors[]`;
  `Retry-After` presence; whether `models/search` accepts the token. (T5)
- 21-10-2026: The Extract Values action owns only its own rules: at most 20 fields, a known kind, a
  required and valid duration unit, a valid list item type, and the reference date format. Alias
  shape and uniqueness, empty content, no fields and decimals are left to Core. Stored field JSON is
  `{ kind, alias, instructions, decimals?, integer, unit?, itemType? }`. Outputs: date
  `yyyy-MM-dd`, time `HH:mm:ss`, date-time `yyyy-MM-dd'T'HH:mm:ssK`, lists as strings. (T17)
- 21-10-2026: The field modal's alias rule is Core's field id rule (start with a letter or `_`,
  then letters, numbers and `_`, up to 64), not "start with a letter". Core, the Automate action and
  Automate's binding paths all accept a leading `_`, so the modal shouldn't be stricter. SPEC and
  EX-9 AC14 updated. (T13)
- 21-10-2026: The field modal uses the general Cancel/Save/Edit/Remove keys, not per-modal keys
  like Decision's, and shows the instructions and decimals errors under their own fields. (T13)
- 21-10-2026: The field modal's binding example sits in the property layout's description slot as
  code, because `umb-property-layout` renders the description as UFM and blanks `{…}`. (T14)
- 21-10-2026: With the flag off, an existing Extract Values step fails with Automate's own "not
  available" message, because the compose-time exclusion removes the action. The action's
  "Extraction is disabled." check stays as a fallback. SPEC and EX-10 AC16 updated to what the
  live run showed. (T18)
- 21-10-2026: Recipe checks ran three of the idea's recipes; Ask Pick-One was left out of the
  triage recipe (no Decision provider on the demo) and the voice-note recipe was blocked by
  Transcribe Audio, not Extraction. (T19)
- 21-10-2026: The extract endpoint and the Extract Values action share one internal enum-name
  parser in Core, and Automate uses Core's field id rule directly (both via InternalsVisibleTo,
  like Agent and Deploy). The parser trims surrounding spaces, so " Hours " is now accepted by the
  API too; numbers and comma lists are still a 400. (T20)
- 26-10-2026: Reversed the 21-10 default alias. The builder overloads now require `WithAlias(...)`
  like every other inline builder, because the alias is how calls stay uniquely named in the
  logs and audit trail. The profile overloads still use "extraction", like Decision's "decision".
  Decision and Image Generation gain the in-service flag check instead (#660).
