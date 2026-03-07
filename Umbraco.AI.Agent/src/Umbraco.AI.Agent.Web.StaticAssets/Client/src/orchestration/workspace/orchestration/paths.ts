import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_AI_SECTION_PATHNAME } from "@umbraco-ai/core";
import { UAI_ORCHESTRATION_ENTITY_TYPE } from "../../constants.js";

export const UAI_ORCHESTRATION_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_ORCHESTRATION_ENTITY_TYPE,
});

export const UAI_CREATE_ORCHESTRATION_WORKSPACE_PATH_PATTERN = new UmbPathPattern(
    "create",
    UAI_ORCHESTRATION_WORKSPACE_PATH,
);

export const UAI_EDIT_ORCHESTRATION_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    "edit/:unique",
    UAI_ORCHESTRATION_WORKSPACE_PATH,
);
