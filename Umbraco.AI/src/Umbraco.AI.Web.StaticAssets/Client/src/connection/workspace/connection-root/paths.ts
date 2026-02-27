import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_AI_SECTION_PATHNAME } from "../../../constants.js";
import { UAI_CONNECTION_ROOT_ENTITY_TYPE } from "../../constants.js";

export const UAI_CONNECTION_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
});
