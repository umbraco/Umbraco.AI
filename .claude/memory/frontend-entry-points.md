---
name: frontend-entry-points
description: Which of the five Client-package entry points (manifests.ts/app.ts/exports.ts/index.ts/internal-components.ts) a new export belongs in
type: gotcha
---

Every product's `src/*.Web.StaticAssets/Client` (or equivalent) folder — Umbraco.AI,
Umbraco.AI.Agent, Umbraco.AI.Prompt, Umbraco.AI.Agent.Copilot, and any future product built the
same way — has five entry points, each with one job. A new file/export must go in the right one:

| File | Job | Rules |
|---|---|---|
| `manifests.ts` | CMS extension manifest *definitions only* | Never re-export component barrels or add side-effect component imports here |
| `app.ts` | PUBLIC runtime entry — the address another add-on's bare specifier (`@umbraco-ai/x`) resolves to via the import map, and its own `backofficeEntryPoint` | Export ONLY `export * from "./exports.js"`, its own `onInit`/`onUnload`, and directly-declared values like `xClientReady`. Never `export * from "./index.js"` here. |
| `exports.ts` | Curated, deliberate public API — anything a real cross-package consumer needs | `api-extractor.json`'s `mainEntryPointFilePath` points here for products that publish npm types; required as the runtime curation point even without publishing, since `app.ts` only re-exports this |
| `index.ts` | Broad internal barrel — aggregates every feature `index.ts`, public or not | Used for local monorepo dev / TS project-reference resolution (`package.json`'s `main`/`exports.import`), and re-exported by `internal-components.ts` |
| `internal-components.ts` | Side-effect-only: `export * from "./index.js"` and nothing else | Its own `backofficeEntryPoint` in `public/umbraco-package.json` (loads every page) but NEVER given an importmap entry. Home for components referenced only by tag name within the same product, never imported as a real value elsewhere. |

**Why:** `app.ts` has two addresses — the import-map target and its own entry point. Umbraco
18.1's per-package cache-busting stamps the import-map address but can't reach a relative import
elsewhere in the same bundle — so if any internal file (hand-written, or bundler-injected via
shared-chunk splitting) imports from `app.js`/`app.ts` by relative path, the browser ends up
loading two different-looking copies of the same file and double-registers every custom element
bundled into it. `internal-components.ts` has only one address, ever, so it can't develop this
problem regardless of what the bundler decides to share across chunks. Established while fixing
umbraco/Umbraco.AI#324: `audit-log-collection.server.data-source.ts` was importing
`AuditLogsService`/`UaiAuditLogTypeMapper` from `../../../app.js` instead of their real modules
(`../../../api` and `../../type-mapper.js`, matching its sibling data source file).

**How to apply:**
- Genuinely public (another package needs it at runtime) → `exports.ts` only, never duplicated in
  `index.ts`'s chain too. Everything else (internal-only, tag-name-referenced components) →
  `index.ts`'s chain, reached only via `internal-components.ts`.
- Never import from `app.js`/`app.ts` by relative path from another file. It's fine to import
  something `app.ts` defines directly (e.g. `xClientReady`) since `app.ts` is genuinely where it
  lives — but service/mapper/component classes should be imported from their real defining module,
  not routed through `app.ts`.
