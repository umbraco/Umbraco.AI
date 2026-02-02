import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_AGENT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_AGENT_ENTITY_TYPE,
});

export const UAI_CREATE_AGENT_WORKSPACE_PATH_PATTERN = new UmbPathPattern(
    "create",
    UAI_AGENT_WORKSPACE_PATH
);

export const UAI_EDIT_AGENT_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    "edit/:unique",
    UAI_AGENT_WORKSPACE_PATH
);
