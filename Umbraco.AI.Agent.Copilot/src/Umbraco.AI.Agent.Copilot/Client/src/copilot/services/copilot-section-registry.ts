import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { switchMap } from "@umbraco-cms/backoffice/external/rxjs";
import type { ManifestUaiCopilotCompatibleSection } from "../types/section-compatibility.js";

/**
 * Registry service for copilot section compatibility.
 *
 * Discovers all sections that declare copilot support via
 * ManifestUaiCopilotCompatibleSection manifests.
 *
 * This replaces the hardcoded section list with a dynamic,
 * extensible registry that third-party packages can contribute to.
 */
export class UaiCopilotSectionRegistry extends UmbControllerBase {
    #compatibleSectionPathnames$: Observable<string[]>;

    constructor(host: UmbControllerHost) {
        super(host);

        // Query all copilot section compatibility manifests
        this.#compatibleSectionPathnames$ = umbExtensionsRegistry
            .byType("uaiCopilotCompatibleSection")
            .pipe(
                switchMap(async (compatibilityManifests) => {
                    const pathnames: string[] = [];

                    for (const manifest of compatibilityManifests) {
                        // Cast to our specific type
                        const compat = manifest as ManifestUaiCopilotCompatibleSection;

                        // Look up the actual section manifest by alias
                        const sectionManifest = await umbExtensionsRegistry.getByAlias(compat.section);

                        // Extract pathname from section meta
                        // Section manifests have meta.pathname property
                        if (sectionManifest && "meta" in sectionManifest && sectionManifest.meta) {
                            const meta = sectionManifest.meta as { pathname?: string };
                            if (meta.pathname) {
                                pathnames.push(meta.pathname);
                            }
                        }
                    }

                    return pathnames;
                })
            );
    }

    /**
     * Observable that emits the list of compatible section pathnames
     * whenever the manifests change.
     *
     * @example
     * ```ts
     * const registry = new UaiCopilotSectionRegistry(this);
     * this.observe(
     *     registry.compatibleSectionPathnames$,
     *     (pathnames) => console.log("Compatible sections:", pathnames)
     * );
     * ```
     */
    get compatibleSectionPathnames$(): Observable<string[]> {
        return this.#compatibleSectionPathnames$;
    }
}
