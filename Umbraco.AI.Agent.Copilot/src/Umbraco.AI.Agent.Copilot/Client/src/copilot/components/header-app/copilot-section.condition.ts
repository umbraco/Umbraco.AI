import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UmbConditionConfigBase,
    UmbConditionControllerArguments,
    UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { combineLatest } from "@umbraco-cms/backoffice/external/rxjs";
import { startWith } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiCopilotSectionRegistry } from "../../services/copilot-section-registry.js";
import { createSectionObservable, isSectionAllowed } from "../../context-observer.js";

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

    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiCopilotSectionConditionConfig>) {
        super(host, args);

        this.#sectionRegistry = new UaiCopilotSectionRegistry(this);

        // Combine current section with compatible sections from registry
        // Use startWith to ensure registry emits immediately (even if empty) so combineLatest works
        this.observe(
            combineLatest([
                createSectionObservable(),
                this.#sectionRegistry.compatibleSectionPathnames$.pipe(startWith([])),
            ]),
            ([currentSection, compatibleSections]) => {
                this.permitted = isSectionAllowed(currentSection, compatibleSections);
            },
            "_observeSectionPermission"
        );
    }
}

export { UaiCopilotSectionCondition as api };
