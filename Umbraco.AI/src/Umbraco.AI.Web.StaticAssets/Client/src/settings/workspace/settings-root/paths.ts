import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_AI_SECTION_PATHNAME } from "../../../constants.ts";
import { UAI_SETTINGS_ROOT_ENTITY_TYPE } from "../../entity.js";

export const UAI_SETTINGS_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_SETTINGS_ROOT_ENTITY_TYPE,
});
