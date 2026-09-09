/**
 * Vitest-only stand-in for `@umbraco-ai/agent`.
 *
 * That package's root entry point unconditionally re-exports its whole surface — including Lit UI
 * components (e.g. the agent picker) that import `@umbraco-ai/core`. `@umbraco-ai/core` is built and
 * externalized for the real app (see this product's `vite.config.ts` `rollupOptions.external:
 * [/^@umbraco-ai/]`) and is never meant to be resolved by a plain module loader — it has no "main"/
 * "module" entry, only a `types` rollup, so vitest cannot load it (or anything that transitively
 * imports it) the way it can `@umbraco-ai/agent-ui` and `@umbraco-ai/agent`'s own source (both of which
 * point `main` at their TS source and resolve fine standalone).
 *
 * `vitest.config.ts` aliases `@umbraco-ai/agent` to this file (test-only — the real build/runtime never
 * sees it) so a test can import a class like `@umbraco-ai/agent-ui`'s `UaiRunController` — which needs
 * `UaiAgentClient`/`UaiHttpAgent` only for its *default* client-owned strategy — without dragging in
 * that unrelated UI-component graph. Extend this stub if a test needs another real export.
 */
export class UaiAgentClient {
    static create(): UaiAgentClient {
        return new UaiAgentClient();
    }
}

export class UaiHttpAgent {}

export function classifyContentKind(): string {
    return "text";
}
