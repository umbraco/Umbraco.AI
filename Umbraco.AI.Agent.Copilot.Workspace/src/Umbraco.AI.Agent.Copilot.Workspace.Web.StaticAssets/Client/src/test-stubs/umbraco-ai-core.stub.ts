/**
 * Vitest-only stand-in for `@umbraco-ai/core`. See `umbraco-ai-agent.stub.ts` for why this exists:
 * `@umbraco-ai/core` has no "main"/"module" entry (only a `types` rollup) and is never resolvable by a
 * plain module loader outside the backoffice build pipeline, which externalizes it. Add an export here
 * if a test's transitive import graph needs another one.
 */
export class UaiAudioRecorder {}
export class UaiSpeechToTextController {}
