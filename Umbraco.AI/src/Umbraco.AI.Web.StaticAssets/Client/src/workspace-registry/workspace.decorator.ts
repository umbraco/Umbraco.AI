/**
 * Workspace Decorator - Intercepts workspace API instantiation
 *
 * Wraps workspace manifest API loaders to automatically register
 * workspace contexts in the registry context.
 */

import { loadManifestApi, type ManifestBase, type UmbExtensionRegistry } from "@umbraco-cms/backoffice/extension-api";
import type { ManifestWorkspace } from "@umbraco-cms/backoffice/workspace";
import { map, distinctUntilChanged } from "@umbraco-cms/backoffice/external/rxjs";
import type { UaiWorkspaceRegistryContext } from "./workspace-registry.context.js";
import type { WorkspaceEntry, WorkspaceContextLike } from "./types.js";

// Track which manifests we've wrapped (by alias)
const wrappedAliases = new Set<string>();

/**
 * Initialize workspace decoration by wrapping all workspace manifest API loaders.
 * Called from the workspace registry context constructor.
 *
 * @param extensionRegistry The Umbraco extension registry
 * @param registry The workspace registry context to register workspaces with
 */
export function initWorkspaceDecorator(
    extensionRegistry: UmbExtensionRegistry<ManifestBase>,
    registry: UaiWorkspaceRegistryContext,
): void {
    extensionRegistry.extensions
        .pipe(
            map((es) => es.filter((e): e is ManifestWorkspace => e.type === "workspace")),
            distinctUntilChanged((a, b) => a.length === b.length),
        )
        .subscribe((workspaceManifests) => {
            for (const manifest of workspaceManifests) {
                if (wrappedAliases.has(manifest.alias) || !manifest.api) continue;

                const originalApi = manifest.api;
                const alias = manifest.alias;

                manifest.api = async () => {
                    const ApiClass = await loadManifestApi(originalApi);
                    return { api: ApiClass ? createDecoratedClass(ApiClass, alias, registry) : ApiClass };
                };

                wrappedAliases.add(alias);
            }
        });
}

/**
 * Create a proxied class that auto-registers/unregisters with the registry
 */
function createDecoratedClass(OriginalClass: any, alias: string, registry: UaiWorkspaceRegistryContext): any {
    if (!OriginalClass) return OriginalClass;

    return new Proxy(OriginalClass, {
        construct(target, args, newTarget) {
            const instance = Reflect.construct(target, args, newTarget) as WorkspaceContextLike;

            // Get entity type (usually available immediately)
            let entityType: string | undefined;
            try {
                entityType = instance.getEntityType?.();
            } catch {
                // Not available
            }

            // Start with temporary UUID key
            let currentKey: string = crypto.randomUUID();

            // Create entry
            const createEntry = (unique?: string | null): WorkspaceEntry => ({
                context: instance,
                alias,
                entityType,
                entityUnique: unique ?? undefined,
            });

            // Register immediately
            registry._register(currentKey, createEntry());

            // Subscribe to unique observable for re-keying
            const uniqueObservable = instance.unique;
            let subscription: { unsubscribe?: () => void } | undefined;

            if (uniqueObservable?.subscribe && entityType) {
                subscription = uniqueObservable.subscribe((uniqueValue: string | null | undefined) => {
                    if (uniqueValue && currentKey.includes("-")) {
                        // Re-key with entity-based key
                        const entityKey = `${entityType}:${uniqueValue}`;
                        registry._rekey(currentKey, entityKey, createEntry(uniqueValue));
                        currentKey = entityKey;
                    }
                });
            }

            // Wrap destroy for cleanup
            const originalDestroy = instance.destroy?.bind(instance);
            let isDestroyed = false;

            (instance as any).destroy = () => {
                if (isDestroyed) {
                    originalDestroy?.();
                    return;
                }
                isDestroyed = true;

                subscription?.unsubscribe?.();
                registry._unregister(currentKey);
                originalDestroy?.();
            };

            return instance;
        },
    });
}
