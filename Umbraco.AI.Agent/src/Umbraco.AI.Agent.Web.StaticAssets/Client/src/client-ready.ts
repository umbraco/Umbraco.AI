/**
 * Resolves once the agent client is configured with auth (see app.ts's onInit).
 *
 * Lives in its own module rather than app.ts so exports.ts and other consumers can import it without
 * reaching into app.ts by relative path — that pattern is what silently double-registered every custom
 * element bundled into app.ts (confirmed live: "already been used with this registry" on
 * uai-user-group-tool-permissions). See .claude/memory/frontend-entry-points.md.
 */
let agentClientReadyResolve: (() => void) | undefined;
export const agentClientReady = new Promise<void>((resolve) => {
    agentClientReadyResolve = resolve;
});

/** Called once by app.ts's onInit after auth configuration completes. */
export function resolveAgentClientReady(): void {
    agentClientReadyResolve?.();
    agentClientReadyResolve = undefined;
}
