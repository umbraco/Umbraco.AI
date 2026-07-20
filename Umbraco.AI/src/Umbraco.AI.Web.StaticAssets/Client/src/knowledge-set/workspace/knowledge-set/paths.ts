import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UmbPathPattern } from "@umbraco-cms/backoffice/router";
import { UAI_AI_SECTION_PATHNAME } from "../../../constants.js";
import { UAI_KNOWLEDGE_SET_ENTITY_TYPE } from "../../entity.js";

export const UAI_KNOWLEDGE_SET_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_AI_SECTION_PATHNAME,
    entityType: UAI_KNOWLEDGE_SET_ENTITY_TYPE,
});

export const UAI_EDIT_KNOWLEDGE_SET_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ id: string }>(
    "edit/:id",
    UAI_KNOWLEDGE_SET_WORKSPACE_PATH,
);
