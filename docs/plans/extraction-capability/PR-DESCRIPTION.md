Port of #655 to v17 | [Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v17/feature/extraction-capability/docs/plans/extraction-capability)

## Why the change

Brings the experimental Extraction capability (typed values out of text via Cloudflare Clairic, the
`Umbraco.AI.Cloudflare` provider, and Automate's Extract Values action) to the Umbraco 17 LTS line,
so Automate customers on 17 get it in the same release as 18.

## What it looks like

Same screens as #655 (AI › Profiles, AI › Settings › "Default Extraction Profile", and the Extract
Values step in Automate), in the v17 backoffice styling. Hidden unless
`Umbraco:AI:Experimental:Extraction` is `true`. See #655 for screenshots.

## Special things to note

- **Needs a decision:** the same open decisions as #655 apply here: raise the `Umbraco.AI.Core`
  floor for Cloudflare, Automate and Deploy at release (Automate now uses two Core internals via
  `InternalsVisibleTo`); Deploy import not checked live (no licence); the Automate decimal-comma
  issue; the 100-second Clairic timeout assumption.
- **Port order:** opened while #655 is still in review, at the requester's call. Any change made
  on #655 during review needs copying here.
- Copied from #655 during review: the builder overloads require `WithAlias(...)` like every other
  inline builder (no `"extraction"` default), so each call is uniquely named in the logs.
- How it was ported: the 20 source commits were cherry-picked with `-x` in order. Two commits were
  not: the v18 generated client (regenerated from the v17 demo site instead) and the v18 PR
  description.
- v17-specific changes:
  - `Umbraco.AI.Cloudflare` is `17.0.0`; README, CLAUDE.md and CHANGELOG point at Umbraco 17; lock
    files resolve CMS `[17.5.0, 18.0.0)`.
  - The v17 generated client has no `$type` on the base field and answer models, so
    `extraction.server.data-source.ts` maps through the concrete generated unions, like Decision's
    v17 data source. The public `UaiExtraction*` types are the same as on v18.
  - Frontend tests use the v17 `UmbControllerHostElementMixin` host (`UmbElementControllerHost`
    doesn't exist on v17).
  - The field-list property editor manifest drops `meta.keywords`, which v17 doesn't support, like
    Decision's v17 manifest.
- The regenerated v17 client also picked up a few unrelated, already-pending changes (a `variant`
  field on property value operations, `UpdateSettingsError`, `X | null` ordering). v17's committed
  client was slightly out of date.
- `AICapability.Extraction = 9` on both lines, so profiles and Deploy artifacts move between 17 and
  18 unchanged. No migrations.
- Live on a fresh v17 demo site: Cloudflare connection test passed, `POST extraction/extract`
  returned all seven field types correctly, an Automate Extract Values run completed with the right
  outputs and If branch, the field modal renders, and `UaiExtractionController` returns typed
  answers. Tests: Core unit 2244, integration 34, Cloudflare 126, Deploy 44, Automate 274,
  frontend 241.

## Change outline

The change itself is described in #655. v17-only diff on top of the cherry-picks:

```diff
 Umbraco.AI.Cloudflare/version.json
-  "version": "18.0.0"
+  "version": "17.0.0"
 Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/extraction/repository/extraction.server.data-source.ts
-import type { ExtractionFieldModel, ExtractionAnswerModel } from "../../api/types.gen.js";
+type ExtractionFieldWireModel = ExtractRequestModel["fields"][number];      // concrete union, carries $type
+type ExtractionAnswerWireModel = ExtractResponseModel["answers"][string];   // concrete union, carries $type
 Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/property-editors/extraction-field-list/manifests.ts
-    keywords: [...],
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_014KRhwDban1s3aQSXzYcwbt
