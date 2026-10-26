[Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v18/feature/extraction-capability/docs/plans/extraction-capability)

## Why the change

Umbraco AI can now pull typed values (text, numbers, dates, times, durations, lists) out of any
text through a new experimental Extraction capability, backed by Cloudflare's Clairic model, so
Automate users can build no-code flows like "email → extract → create content" without prompting
a chat model and parsing its reply.

## What it looks like

**Where to find it:**

- AI › Profiles › any Extraction profile (Settings tab)
- AI › Settings › Defaults › "Default Extraction Profile"
- Automation › any workspace › any automation › add or double-click an "Extract Values" step
  (field list, "Add field" type picker, field modal)

**Who can see it:** only when `Umbraco:AI:Experimental:Extraction` is `true` (off by default).
Then: anyone with access to the AI section for profiles and settings, and anyone with access to an
Automate workspace for the Extract Values step.

![Extract Values step settings](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/extract-values-settings.png?raw=true)

| Field type picker | Field modal | Duplicate alias |
|---|---|---|
| ![Field type picker](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/extraction-field-picker.png?raw=true) | ![Field modal](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/extraction-field-config.png?raw=true) | ![Duplicate alias error](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/extraction-field-config-error.png?raw=true) |

| Profiles list | Extraction profile | Default profile setting |
|---|---|---|
| ![Profiles list](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/profiles-list.png?raw=true) | ![Extraction profile](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/extraction-profile.png?raw=true) | ![Default Extraction Profile](https://github.com/umbraco/Umbraco.AI/blob/c40d289ce086cd4aff39d1692e52cedde7100c1a/docs/plans/extraction-capability/screenshots/settings-extraction-default.png?raw=true) |

## Special things to note

- **Needs a decision:** the `Umbraco.AI.Core` floor for `Umbraco.AI.Cloudflare`,
  `Umbraco.AI.Automate` and `Umbraco.AI.Deploy` is still `[18.6.0, …)`. All three now use new Core
  types, and Automate uses two Core internals (`ExtractionEnumNames.TryParse`,
  `ExtractionFieldValidator.IsValidFieldId`) via `InternalsVisibleTo`. The floor must be raised to
  the Core release that ships Extraction during release prep (the ranged pack will fail if it
  isn't). Also set the real minimum in `Umbraco.AI.Cloudflare/README.md`.
- **Needs a decision:** Deploy import of the default Extraction profile was not checked end to end.
  Export was checked live (the settings `.uda` carries `DefaultExtractionProfileUdi` and the profile
  as a dependency), but the demo has no Deploy licence ("Deploy extraction will not be enabled"), so
  import is covered by the 9 unit specs only. Accept that, or check on a licensed site before
  merge.
- **Needs a decision:** on a `da-DK` server, Automate turns numbers into text with a decimal comma
  (`0,9999…`, `701,1`), so an If like "`confidence` GreaterThan `0.8`" takes the wrong branch. This
  is Automate behaviour, not this PR, and it already affects the Decision actions. An Automate issue
  is drafted (problem and expected behaviour only) and not filed yet.
- **Needs a decision:** SPEC assumes the default 100-second HttpClient timeout per Clairic attempt
  is fine (live calls took about 0.2 s). Nobody has confirmed it.
- With the flag off, an existing Extract Values step fails with Automate's own "uses
  'umbracoAI.extractValues', which is not available" message, not "Extraction is disabled.", because
  the action is excluded at startup. SPEC and EX-10 AC16 were updated to match.
- The live Clairic call proved two wire guesses wrong: list `items` must be a plain string
  (`"items": "text"`), and a not-found answer still carries a guessed `span`, which is now dropped.
  Both are fixed and pinned by tests using the real shapes.
- The Cloudflare client leaves a missing answer out and lets Core's checker reject it (clearer
  message), passes unasked answers through for Core to reject, masks the API token before cutting
  error text to 500 characters, and escapes `AccountId` in every URL.
- `AIExtractionService` checks the experimental flag itself, before any profile lookup, event or
  provider call (#660 adds the same check to Decision and Image Generation). Like every other
  inline builder, the builder overloads require `WithAlias(...)` so each call is uniquely named in
  the logs.
- The extract endpoint reuses the shared `UsageModel` (`inputTokens`…) and now accepts duration
  units and list item types with surrounding spaces (shared parser with Automate). Numbers and comma
  lists are still a 400.
- The `AIProfileService` "experimental capability not enabled" error now names the exact key
  (`Umbraco:AI:Experimental:<Capability>`).
- Recipes checked live on the demo site: ticket triage (Webhook → Extract Values → If),
  lead intake (→ Create Content) and refund triage (→ If → Request Approval, run suspended for
  approval). Ask Pick-One was left out (no Decision provider on the demo). The voice-note recipe was
  blocked earlier, in Transcribe Audio ("Audio file might be corrupted or unsupported" from OpenAI),
  not in Extraction.
- New provider package `Umbraco.AI.Cloudflare` (18.0.0): added to CI, the root solution, the demo
  and package-test install scripts, README and CLAUDE.md. The release manifest entry goes on the
  release branch.
- No migrations. `AICapability.Extraction = 9` is persisted as an int and must never change.
- The v17 port (`v17/feature/extraction-capability`) and the Umbraco.Docs pages follow as separate
  PRs.

## Change outline

The new capability copies Decision file for file: one typed field/answer pair per type, a batch
request over shared text, and the same client pipeline.

```
AIExtractionRequest { State, Fields[], ReferenceDate? }
  AITextExtractionField      → AITextExtractionAnswer      { string? Value; Span? }
  AINumberExtractionField    → AINumberExtractionAnswer    { decimal? Value }       + Decimals?, Integer, Unit?
  AIDateExtractionField      → AIDateExtractionAnswer      { DateOnly? Value }
  AITimeExtractionField      → AITimeExtractionAnswer      { TimeOnly? Value }
  AIDateTimeExtractionField  → AIDateTimeExtractionAnswer  { DateTime? Value }
  AIDurationExtractionField  → AIDurationExtractionAnswer  { decimal? Value; Unit } + Unit, Decimals?, Integer
  AIListExtractionField      → AIListExtractionAnswer      { Values[]; Spans[] }    + ItemType
AIExtractionAnswer { Confidence?, NullProbability?, IsFound }   // IsFound = value present
```

The client pipeline (outermost first), built by `AIExtractionClientFactory`:

```
IAIExtractionService.ExtractAsync / GetExtractionResponseAsync   (flag check, profile resolution)
  ScopedInlineExtractionClient
    ValidatingExtractionClient          // field ids, instructions, state, decimals → ArgumentException
      ScopedProfileExtractionClient     // runtime context, profile metadata
        AIOpenTelemetryExtractionMiddleware → AITrackingExtractionMiddleware (usage + audit)
          AIErrorClassifyingExtractionClient   // ExtractionAnswerChecker: missing/extra/wrong-type → AIProviderException
            DeclaredSettings/CapabilitySettings clients
              CloudflareExtractionClient       // POST /accounts/{id}/ai/run/@cf/cloudflare/clairic
```

What was added by product:

```diff
 Umbraco.AI/src/Umbraco.AI.Core/
+├── Extraction/            # types, service, builder, factory, middleware, notifications, validator, checker
 ├── Models/AICapability.cs           # + Extraction = 9
 ├── Settings/AIExperimentalOptions   # + Extraction (default false)
 ├── Settings/AISettings, AIOptions   # + DefaultExtractionProfileId / DefaultExtractionProfileAlias
 └── Providers/IAICapability.cs       # + IAIExtractionCapability + base classes
 Umbraco.AI/src/Umbraco.AI.Web/Api/Management/
+├── Extraction/            # POST extraction/extract, polymorphic $type models
 ├── Settings/              # + defaultExtractionProfileId
 └── Profile/               # + ExtractionProfileSettingsModel ($type "extraction")
 Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/
+├── extraction/            # public UaiExtractionController (@umbraco-ai/core)
+├── property-editors/extraction-field-list/   # Uai.PropertyEditorUi.ExtractionFieldList + field modal
 ├── profile/, settings/    # Extraction profile settings, "Default Extraction Profile" picker
 └── api/                   # regenerated client
+Umbraco.AI.Cloudflare/     # new provider package (Clairic only)
 Umbraco.AI.Automate/src/Umbraco.AI.Automate/Actions/
+└── ExtractValuesAction.cs  # "Extract Values": outputs { value, found, confidence } per alias
 Umbraco.AI.Deploy/         # + DefaultExtractionProfileUdi on the settings artifact
```

Settings:

```diff
 "Umbraco": { "AI": {
+  "DefaultExtractionProfileAlias": "cloudflare-clairic",
   "Experimental": {
     "Decision": false,
+    "Extraction": false
   }
 } }
```

Management API:

```diff
+POST /umbraco/ai/management/api/v1/extraction/extract
+  { profileIdOrAlias?, state, referenceDate?, fields: [{ $type, id, instructions, ... }] }
+  → 200 { answers: { <id>: { $type, value|values, span|spans, confidence?, nullProbability?, isFound } }, modelId, usage }
+  → 400 ProblemDetails (validation, no default, wrong capability, provider error), 404 (named profile missing or flag off)
 GET  /umbraco/ai/management/api/v1/capabilities/enabled   # + "Extraction" when the flag is on
 GET/PUT /umbraco/ai/management/api/v1/settings            # + defaultExtractionProfileId
```

Automate step output (every alias always present, so bindings and If checks always resolve):

```
${ steps.<step>.<alias>.value }       // string (text/date/time/datetime), number (number/duration), string[] (list), null when not found
${ steps.<step>.<alias>.found }       // boolean
${ steps.<step>.<alias>.confidence }  // number or null
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_014KRhwDban1s3aQSXzYcwbt
