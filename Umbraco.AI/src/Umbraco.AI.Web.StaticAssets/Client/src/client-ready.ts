/**
 * Resolves once the core client is configured with auth (see app.ts's onInit).
 *
 * Lives in its own module rather than app.ts so exports.ts can re-export it without creating an
 * app.ts <-> exports.ts import cycle by relative path — that cycle is what silently double-registered
 * every custom element bundled into app.ts (confirmed live: "already been used with this registry" on
 * uai-user-group-settings-list). See .claude/memory/frontend-entry-points.md.
 */
let coreClientReadyResolve: (() => void) | undefined;
export const coreClientReady = new Promise<void>((resolve) => {
    coreClientReadyResolve = resolve;
});

/** Called once by app.ts's onInit after auth configuration completes. */
export function resolveCoreClientReady(): void {
    coreClientReadyResolve?.();
    coreClientReadyResolve = undefined;
}
