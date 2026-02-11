import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { combineLatest } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiCopilotSectionRegistry } from "../../services/copilot-section-registry.js";
import { observeSectionChanges, isSectionAllowed } from "../../section-detector.js";

export interface UaiCopilotSectionConditionConfig extends UmbConditionConfigBase {
    // No config needed - sections are discovered from manifests
}

/**
 * Condition that checks if the current URL section is compatible with copilot.
 * Uses dynamic manifest-based discovery instead of hardcoded section lists.
 *
 * Sections declare compatibility via ManifestUaiCopilotCompatibleSection manifests.
 *
 * WORKAROUND: This custom condition exists because Umb.Condition.SectionAlias
 * does not work for headerApp extensions (header apps are outside section context).
 *
 * TODO: When https://github.com/umbraco/Umbraco-CMS/issues/21486 is resolved,
 * replace this with the built-in Umb.Condition.SectionAlias condition.
 */
export class UaiCopilotSectionCondition
    extends UmbConditionBase<UaiCopilotSectionConditionConfig>
    implements UmbExtensionCondition
{
    #sectionRegistry!: UaiCopilotSectionRegistry;
    #cleanup: (() => void) | null = null;

    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiCopilotSectionConditionConfig>) {
        super(host, args);

        this.#sectionRegistry = new UaiCopilotSectionRegistry(this);

        // Combine current section detection with dynamic section registry
        let currentSection: string | null = null;

        this.#cleanup = observeSectionChanges((pathname) => {
            currentSection = pathname;
            this.#updatePermission(currentSection);
        });

        // Observe registry changes
        this.observe(
            this.#sectionRegistry.compatibleSectionPathnames$,
            () => {
                this.#updatePermission(currentSection);
            },
            "_observeSectionRegistry"
        );
    }

    #updatePermission(currentSection: string | null): void {
        // Get current compatible sections from registry (may be async, so use last known value)
        // We'll combine observables properly
        this.observe(
            combineLatest([
                this.#sectionRegistry.compatibleSectionPathnames$,
            ]),
            ([compatibleSections]) => {
                this.permitted = isSectionAllowed(currentSection, compatibleSections);
            },
            "_updatePermission"
        );
    }

    override hostDisconnected(): void {
        super.hostDisconnected();
        this.#cleanup?.();
    }
}

export { UaiCopilotSectionCondition as api };
