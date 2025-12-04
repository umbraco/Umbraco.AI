import type { PromptResponseModel, PromptItemResponseModel, ScopeModel } from "../api/types.gen.js";
import { UAI_PROMPT_ENTITY_TYPE } from "./constants.js";
import type { UaiPromptScope, UaiScopeRule } from "./property-actions/types.js";
import type { UaiPromptDetailModel, UaiPromptItemModel } from "./types.js";

/**
 * Maps API scope model to internal scope model.
 */
function mapScopeFromApi(apiScope: ScopeModel | null | undefined): UaiPromptScope | null {
    if (!apiScope) return null;

    return {
        includeRules: (apiScope.includeRules ?? []).map(mapRuleFromApi),
        excludeRules: (apiScope.excludeRules ?? []).map(mapRuleFromApi),
    };
}

function mapRuleFromApi(rule: { propertyEditorUiAliases?: string[] | null; propertyAliases?: string[] | null; documentTypeAliases?: string[] | null }): UaiScopeRule {
    return {
        propertyEditorUiAliases: rule.propertyEditorUiAliases ?? null,
        propertyAliases: rule.propertyAliases ?? null,
        documentTypeAliases: rule.documentTypeAliases ?? null,
    };
}

/**
 * Maps internal scope model to API scope model.
 */
function mapScopeToApi(scope: UaiPromptScope | null): ScopeModel | null {
    if (!scope) return null;

    return {
        includeRules: scope.includeRules.map(rule => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            documentTypeAliases: rule.documentTypeAliases,
        })),
        excludeRules: scope.excludeRules.map(rule => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            documentTypeAliases: rule.documentTypeAliases,
        })),
    };
}

export const UaiPromptTypeMapper = {
    toDetailModel(response: PromptResponseModel): UaiPromptDetailModel {
        return {
            unique: response.id,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            content: response.content,
            profileId: response.profileId ?? null,
            tags: response.tags ?? [],
            scope: mapScopeFromApi(response.scope),
            isActive: response.isActive,
        };
    },

    toItemModel(response: PromptItemResponseModel): UaiPromptItemModel {
        return {
            unique: response.id,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
            scope: mapScopeToApi(model.scope),
        };
    },

    toUpdateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            content: model.content,
            description: model.description,
            profileId: model.profileId,
            tags: model.tags,
            scope: mapScopeToApi(model.scope),
            isActive: model.isActive,
        };
    },
};
