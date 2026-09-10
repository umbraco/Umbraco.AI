/**
 * Vitest-only stand-in for `@umbraco-ai/core`.
 *
 * `@umbraco-ai/core` has no "main"/"module" entry — only a `types` rollup — so it's unresolvable by a
 * plain module loader outside the backoffice build pipeline, which externalizes it instead of bundling
 * it (see this product's `vite.config.ts` `rollupOptions.external`). `@umbraco-ai/agent`'s barrel
 * (pulled in transitively via `@umbraco-ai/agent-ui`) imports from it, so the bare specifier needs to
 * resolve to *something* for vitest's import analysis, even though this test never exercises any of its
 * exports.
 *
 * This is intentionally an empty module — no exports. Nothing in the current test suite's call path
 * touches `@umbraco-ai/core`'s exports at runtime, only at the type level (which is erased before
 * bundling), so there is nothing here to fake. If a future test genuinely needs a runtime value from
 * `@umbraco-ai/core`, add a real, empty (no invented behaviour) stand-in for that specific export here.
 */
export {};
