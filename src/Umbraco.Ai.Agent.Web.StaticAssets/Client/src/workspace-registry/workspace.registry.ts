/**
 * Workspace Registry - Singleton service for tracking active workspace contexts
 *
 * Enables cross-DOM access to workspace contexts for the Copilot sidebar,
 * which operates in a separate DOM subtree from workspace editors.
 */

import { Subject } from "@umbraco-cms/backoffice/external/rxjs";
import type {
  WorkspaceEntry,
  WorkspaceChangeEvent,
  WorkspaceContextLike,
} from "./types.js";

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

class WorkspaceRegistry {
  readonly #entries = new Map<string, WorkspaceEntry>();
  readonly #changes$ = new Subject<WorkspaceChangeEvent>();

  #navigationCleanupTimeout: ReturnType<typeof setTimeout> | null = null;
  #isNavigationCleanupSetup = false;

  constructor() {
    this.#setupNavigationCleanup();
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
  }

  /** @internal */
  _unregister(key: string): void {
    const entry = this.#entries.get(key);
    if (entry) {
      this.#entries.delete(key);
      this.#changes$.next({ type: "removed", key, entry });
    }
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Private
  // ─────────────────────────────────────────────────────────────────────────────

  #cleanupDisconnected(): void {
    // Process in reverse (LIFO) order so children are removed before parents
    const entries = Array.from(this.#entries.entries()).reverse();
    for (const [key, entry] of entries) {
      if (!isWorkspaceConnected(entry.context as WorkspaceContextLike)) {
        this.#entries.delete(key);
        this.#changes$.next({ type: "removed", key, entry });
      }
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

export const workspaceRegistry = new WorkspaceRegistry();
