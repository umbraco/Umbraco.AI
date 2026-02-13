import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_AI_SECTION_PATHNAME } from "../../../constants.js";
import { UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";

export const UAI_CONTEXT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_CONTEXT_ENTITY_TYPE,
});

export const UAI_CREATE_CONTEXT_WORKSPACE_PATH_PATTERN = new UmbPathPattern("create", UAI_CONTEXT_WORKSPACE_PATH);

export const UAI_EDIT_CONTEXT_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
    "edit/:unique",
    UAI_CONTEXT_WORKSPACE_PATH,
);
