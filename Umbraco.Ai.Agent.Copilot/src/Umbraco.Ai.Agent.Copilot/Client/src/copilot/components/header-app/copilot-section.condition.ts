import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
  UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { observeSectionChanges, isSectionAllowed } from "../../section-detector.js";

export interface UaiCopilotSectionConditionConfig extends UmbConditionConfigBase {
  /**
   * The section pathnames where the copilot should be available.
   * These are the URL path segments (e.g., "content", "media").
   */
  sectionPathnames: string[];
}

/**
 * Condition that checks if the current URL section is in the allowed list.
 * Uses URL-based detection which works at the header level.
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
  #cleanup: (() => void) | null = null;

  constructor(
    host: UmbControllerHost,
    args: UmbConditionControllerArguments<UaiCopilotSectionConditionConfig>
  ) {
    super(host, args);

    this.#cleanup = observeSectionChanges((pathname) => {
      this.permitted = isSectionAllowed(pathname, this.config.sectionPathnames);
    });
  }

  override hostDisconnected(): void {
    super.hostDisconnected();
    this.#cleanup?.();
  }
}

export { UaiCopilotSectionCondition as api };
