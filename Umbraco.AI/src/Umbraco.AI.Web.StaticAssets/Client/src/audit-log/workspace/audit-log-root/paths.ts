import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_AI_SECTION_PATHNAME } from "../../../constants.ts";
import { UAI_AUDIT_LOG_ROOT_ENTITY_TYPE } from "../../entity.ts";

export const UAI_AUDIT_LOG_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_AUDIT_LOG_ROOT_ENTITY_TYPE,
});
