import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN = UMB_WORKSPACE_PATH_PATTERN.createRoutePattern({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_CONTEXT_ENTITY_TYPE,
    routing: "create",
});

export const UAI_EDIT_CONTEXT_WORKSPACE_PATH_PATTERN = UMB_WORKSPACE_PATH_PATTERN.createRoutePattern({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_CONTEXT_ENTITY_TYPE,
    routing: "edit/:unique",
});
