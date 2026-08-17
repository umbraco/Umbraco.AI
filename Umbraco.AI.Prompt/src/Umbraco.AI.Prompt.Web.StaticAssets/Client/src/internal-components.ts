// Side-effect-only entry point: registers internal components (referenced only by tag
// name in templates) on every page load. Kept separate from app.ts, which is also the
// address other add-ons resolve "@umbraco-ai/prompt" to.
export * from "./index.js";
