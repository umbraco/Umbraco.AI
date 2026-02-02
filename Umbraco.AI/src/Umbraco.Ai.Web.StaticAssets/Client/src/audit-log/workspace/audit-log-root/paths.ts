import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";
import { UAI_AUDIT_LOG_ROOT_ENTITY_TYPE } from "../../entity.ts";

export const UAI_AUDIT_LOG_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_AUDIT_LOG_ROOT_ENTITY_TYPE,
});
