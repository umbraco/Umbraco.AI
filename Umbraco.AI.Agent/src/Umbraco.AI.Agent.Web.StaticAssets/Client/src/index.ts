export * from "./agent/index.js";
export * from "./core/index.js";
export * from "./transport/index.js";
// client-ready.ts must be dual-reachable (also here, not just via app.ts/exports.ts) -- otherwise it's
// bundled solo into app.js, which the backoffice loads twice (its own backofficeEntryPoint address, and
// the import-map address other packages resolve "@umbraco-ai/agent" to). Each load gets its own copy of
// the module-level agentClientReady promise, but onInit -- which resolves it -- only ever runs for the
// backofficeEntryPoint copy. A cross-package consumer importing agentClientReady via the bare specifier
// (e.g. Umbraco.AI.Agent.Copilot's sidebar) resolves through the import-map copy instead, so it awaits a
// promise nothing ever resolves and hangs forever (symptom: the copilot sidebar shows "No agents
// available" indefinitely). Re-exporting it here too lets the bundler chunk-split it into one shared,
// single-address file instead -- the same fix already applied to every double-registering custom
// element in umbraco/Umbraco.AI#352.
export * from "./client-ready.js";
