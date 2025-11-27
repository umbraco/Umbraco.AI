import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_CONNECTION_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_CONNECTION_ENTITY_TYPE,
});

export const UAI_CREATE_CONNECTION_WORKSPACE_PATH = `${UAI_CONNECTION_WORKSPACE_PATH}/create`;

export const UAI_EDIT_CONNECTION_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    'edit/:unique',
    UAI_CONNECTION_WORKSPACE_PATH,
);
