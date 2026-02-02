import type { ManifestHeaderApp } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestCondition } from "@umbraco-cms/backoffice/extension-api";
import type { UaiCopilotSectionConditionConfig } from "./copilot-section.condition.js";

/**
 * Section URL pathnames where the AI Copilot is available.
 * These correspond to the URL path: /section/{pathname}/...
 */
export const UAI_COPILOT_ALLOWED_SECTION_PATHNAMES = [
  "content",
  "media",
];

export const UAI_COPILOT_SECTION_CONDITION_ALIAS = "UmbracoAiAgent.Condition.CopilotSection";

/**
 * Custom condition for section filtering.
 *
 * WORKAROUND: We use a custom condition instead of Umb.Condition.SectionAlias
 * because the built-in condition doesn't work for headerApp extensions.
 *
 * TODO: When https://github.com/umbraco/Umbraco-CMS/issues/21486 is resolved,
 * remove this condition and use Umb.Condition.SectionAlias with oneOf directly.
 */
const conditionManifest: ManifestCondition = {
  type: "condition",
  alias: UAI_COPILOT_SECTION_CONDITION_ALIAS,
  name: "Copilot Section Condition",
  api: () => import("./copilot-section.condition.js"),
};

const headerAppManifest: ManifestHeaderApp = {
  type: "headerApp",
  alias: "UmbracoAiAgent.HeaderApp.Copilot",
  name: "AI Copilot Header App",
  element: () => import("./copilot-header-app.element.js"),
  weight: 100,
  conditions: [
    {
      alias: UAI_COPILOT_SECTION_CONDITION_ALIAS,
      sectionPathnames: UAI_COPILOT_ALLOWED_SECTION_PATHNAMES,
    } as UaiCopilotSectionConditionConfig,
  ],
};

export const headerAppManifests = [
  conditionManifest,
  headerAppManifest,
];
