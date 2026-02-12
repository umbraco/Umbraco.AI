import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { Observable, combineLatest } from "@umbraco-cms/backoffice/external/rxjs";
import { map } from "@umbraco-cms/backoffice/external/rxjs";
import type { ManifestUaiCopilotCompatibleSection } from "../types/section-compatibility.js";
import { ManifestSection } from "@umbraco-cms/backoffice/section";

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

        // Observe both compatibility manifests AND section manifests
        // This ensures we recompute when either changes (sections might load after compatibility manifests)
        this.#compatibleSectionPathnames$ = combineLatest([
            umbExtensionsRegistry.byType<string, ManifestUaiCopilotCompatibleSection>("uaiCopilotCompatibleSection"),
            umbExtensionsRegistry.byType<string, ManifestSection>("section"),
        ]).pipe(
            map(([compatibilityManifests, sectionManifests]) => {
                const pathnames: string[] = [];

                for (const compatManifest of compatibilityManifests) {
                    const sectionManifest = sectionManifests.find((s) => s.alias === compatManifest.section);
                    if (sectionManifest?.meta?.pathname) {
                        pathnames.push(sectionManifest.meta.pathname);
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
