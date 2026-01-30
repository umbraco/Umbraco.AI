import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_TEST_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UAI_TEST_ENTITY_TYPE,
});

export const UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{}>(
    "create",
    UAI_TEST_WORKSPACE_PATH
);

export const UAI_EDIT_TEST_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    "edit/:unique",
    UAI_TEST_WORKSPACE_PATH
);
