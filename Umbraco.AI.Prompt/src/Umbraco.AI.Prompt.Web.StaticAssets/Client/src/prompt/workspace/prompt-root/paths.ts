import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROMPT_ROOT_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_PROMPT_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_PROMPT_ROOT_ENTITY_TYPE,
});
