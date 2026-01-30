/**
 * Workspace Registry Context
 *
 * Global context that tracks active workspace contexts across the backoffice.
 * Enables cross-DOM access to workspace contexts for AI tools, which may operate
 * in a separate DOM subtree from workspace editors.
 *
 * This context is auto-provided at the backoffice root level via the globalContext manifest.
 *
 * Components can consume this context to:
 * - Get active workspaces by entity type and ID
 * - Subscribe to workspace change events
 * - Get all active workspaces
 */

import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { Subject } from "@umbraco-cms/backoffice/external/rxjs";
import { initWorkspaceDecorator } from "./workspace.decorator.js";
import type { WorkspaceEntry, WorkspaceChangeEvent, WorkspaceContextLike } from "./types.js";

/**
 * Check if a workspace context's host element is still connected to the DOM.
 * Works across shadow DOM boundaries via Node.isConnected.
 */
function isWorkspaceConnected(context: WorkspaceContextLike): boolean {
	const host = context.getHostElement?.() ?? context._host ?? context.host;
	if (host && typeof host === "object" && "isConnected" in host) {
		return (host as Node).isConnected;
	}
	return true;
}

/**
 * Global context providing workspace registry functionality.
 * Registered as a globalContext manifest - auto-instantiated at backoffice root.
 */
export class UaiWorkspaceRegistryContext extends UmbControllerBase {
	/** Type guard marker for context resolution. */
	public readonly IS_WORKSPACE_REGISTRY_CONTEXT = true;

	readonly #entries = new Map<string, WorkspaceEntry>();
	readonly #changes$ = new Subject<WorkspaceChangeEvent>();

	#navigationCleanupTimeout: ReturnType<typeof setTimeout> | null = null;
	#isNavigationCleanupSetup = false;

	constructor(host: UmbControllerHost) {
		super(host);

		// Set up navigation cleanup
		this.#setupNavigationCleanup();

		// Initialize the workspace decorator to track all workspaces
		initWorkspaceDecorator(umbExtensionsRegistry, this);

		// Provide this context for consumers
		this.provideContext(UAI_WORKSPACE_REGISTRY_CONTEXT, this);
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Public API
	// ─────────────────────────────────────────────────────────────────────────────

	/** Observable of workspace change events */
	get changes$() {
		return this.#changes$.asObservable();
	}

	/** Get a workspace by its entity type and unique ID */
	getByEntity(entityType: string, unique: string): WorkspaceEntry | undefined {
		return this.#entries.get(`${entityType}:${unique}`);
	}

	/** Get all registered workspaces */
	getAll(): WorkspaceEntry[] {
		return Array.from(this.#entries.values());
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Internal API (used by decorator)
	// ─────────────────────────────────────────────────────────────────────────────

	/** @internal */
	_register(key: string, entry: WorkspaceEntry): void {
		const eventType: WorkspaceChangeEvent["type"] = this.#entries.has(key) ? "updated" : "added";
		this.#entries.set(key, entry);
		this.#changes$.next({ type: eventType, key, entry });
		console.debug(`[WorkspaceRegistry] ${eventType}: ${entry.alias} (${key}) [size: ${this.#entries.size}]`);
	}

	/** @internal */
	_rekey(oldKey: string, newKey: string, entry: WorkspaceEntry): void {
		const existing = this.#entries.get(newKey);
		if (existing && existing.context !== entry.context) {
			this.#entries.delete(newKey);
		}
		this.#entries.delete(oldKey);
		this.#entries.set(newKey, entry);
		this.#changes$.next({ type: "updated", key: newKey, entry });
		console.debug(`[WorkspaceRegistry] rekey: ${oldKey} → ${newKey} [size: ${this.#entries.size}]`);
	}

	/** @internal */
	_unregister(key: string): void {
		const entry = this.#entries.get(key);
		if (entry) {
			this.#entries.delete(key);
			this.#changes$.next({ type: "removed", key, entry });
			console.debug(`[WorkspaceRegistry] removed: ${entry.alias} (${key}) [size: ${this.#entries.size}]`);
		}
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Private
	// ─────────────────────────────────────────────────────────────────────────────

	#cleanupDisconnected(): void {
		// Process in reverse (LIFO) order so children are removed before parents
		const entries = Array.from(this.#entries.entries()).reverse();
		const sizeBefore = this.#entries.size;
		for (const [key, entry] of entries) {
			if (!isWorkspaceConnected(entry.context as WorkspaceContextLike)) {
				this.#entries.delete(key);
				this.#changes$.next({ type: "removed", key, entry });
				console.debug(`[WorkspaceRegistry] cleanup: ${entry.alias} (${key}) [size: ${this.#entries.size}]`);
			}
		}
		if (sizeBefore !== this.#entries.size) {
			console.debug(`[WorkspaceRegistry] cleanup complete: ${sizeBefore} → ${this.#entries.size}`);
		}
	}

	#setupNavigationCleanup(): void {
		if (this.#isNavigationCleanupSetup) return;
		this.#isNavigationCleanupSetup = true;

		const scheduleCleanup = () => {
			if (this.#navigationCleanupTimeout) {
				clearTimeout(this.#navigationCleanupTimeout);
			}
			this.#navigationCleanupTimeout = setTimeout(() => {
				this.#cleanupDisconnected();
				this.#navigationCleanupTimeout = null;
			}, 500);
		};

		window.addEventListener("popstate", scheduleCleanup);
		window.addEventListener("hashchange", scheduleCleanup);

		const originalPushState = history.pushState.bind(history);
		const originalReplaceState = history.replaceState.bind(history);

		history.pushState = (...args) => {
			originalPushState(...args);
			scheduleCleanup();
		};

		history.replaceState = (...args) => {
			originalReplaceState(...args);
			scheduleCleanup();
		};
	}
}

/**
 * Context token for consuming the Workspace Registry.
 * Use this to access active workspaces from any component.
 *
 * @example
 * ```typescript
 * this.consumeContext(UAI_WORKSPACE_REGISTRY_CONTEXT, (context) => {
 *   const workspaces = context.getAll();
 *   context.changes$.subscribe(event => console.log(event));
 * });
 * ```
 */
export const UAI_WORKSPACE_REGISTRY_CONTEXT = new UmbContextToken<UaiWorkspaceRegistryContext>(
	"UaiWorkspaceRegistryContext",
	undefined,
	(context): context is UaiWorkspaceRegistryContext =>
		(context as UaiWorkspaceRegistryContext).IS_WORKSPACE_REGISTRY_CONTEXT
);

export default UaiWorkspaceRegistryContext;
