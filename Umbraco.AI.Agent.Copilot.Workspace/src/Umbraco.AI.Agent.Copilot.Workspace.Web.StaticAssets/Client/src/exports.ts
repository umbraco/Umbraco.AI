/**
 * Public API exports for the @umbraco-ai/agent-copilot-workspace package.
 *
 * This package is a self-contained backoffice section: all of its Lit elements self-register (via
 * `@customElement`) and are wired through manifests, so it deliberately exposes **no** reusable
 * elements or contexts to other packages. Only the shared aliases/entity types in `constants.ts` —
 * which the backend and any integrators may need to reference — are part of the public surface.
 */
export * from "./constants.js";
