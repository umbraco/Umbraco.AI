import type { ContextResourceModel, ProjectRequestModel, ProjectResponseModel } from "../api/types.gen.js";
import { UAI_PROJECT_ENTITY_TYPE } from "../constants.js";

export type { ContextResourceModel, ProjectRequestModel, ProjectResponseModel };

/** All-zero GUID used as the `unique` for an unsaved (scaffolded) project. */
export const UAI_EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

/**
 * The workspace-editing shape for a project — an `UmbEntityModel` (`unique` + `entityType`) plus the
 * editable fields. The workspace context edits this; it maps to/from the generated API models on
 * load/submit.
 */
export interface UaiProjectDetailModel {
    entityType: string;
    unique: string;
    name: string;
    description: string | null;
    instructions: string | null;
    contextIds: string[];
    resources: ContextResourceModel[];
    dateCreated?: string;
    dateModified?: string;
}

/** Maps an API project into the workspace detail model. */
export function toProjectDetailModel(project: ProjectResponseModel): UaiProjectDetailModel {
    return {
        entityType: UAI_PROJECT_ENTITY_TYPE,
        unique: project.id,
        name: project.name,
        description: project.description ?? null,
        instructions: project.instructions ?? null,
        contextIds: [...project.contextIds],
        resources: [...project.resources],
        dateCreated: project.dateCreated,
        dateModified: project.dateModified,
    };
}

/** Maps the workspace detail model into an API request body. */
export function toProjectRequestModel(model: UaiProjectDetailModel): ProjectRequestModel {
    return {
        name: model.name.trim(),
        description: model.description?.trim() || null,
        instructions: model.instructions?.trim() || null,
        contextIds: model.contextIds,
        resources: model.resources,
    };
}

/** An empty project scaffold for the create flow. */
export function createProjectScaffold(defaultName: string): UaiProjectDetailModel {
    return {
        entityType: UAI_PROJECT_ENTITY_TYPE,
        unique: UAI_EMPTY_GUID,
        name: defaultName,
        description: null,
        instructions: null,
        contextIds: [],
        resources: [],
    };
}
