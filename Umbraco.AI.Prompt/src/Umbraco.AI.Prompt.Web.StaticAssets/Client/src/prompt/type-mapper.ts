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
        allowRules: (apiScope.allowRules ?? []).map(mapScopeRuleFromApi),
        denyRules: (apiScope.denyRules ?? []).map(mapScopeRuleFromApi),
    };
}

function mapScopeRuleFromApi(rule: {
    propertyEditorUiAliases?: string[] | null;
    propertyAliases?: string[] | null;
    contentTypeAliases?: string[] | null;
}): UaiScopeRule {
    return {
        propertyEditorUiAliases: rule.propertyEditorUiAliases ?? null,
        propertyAliases: rule.propertyAliases ?? null,
        contentTypeAliases: rule.contentTypeAliases ?? null,
    };
}

/**
 * Maps internal scope model to API scope model.
 */
function mapScopeToApi(scope: UaiPromptScope | null): ScopeModel | null {
    if (!scope) return null;

    return {
        allowRules: scope.allowRules.map((rule) => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            contentTypeAliases: rule.contentTypeAliases,
        })),
        denyRules: scope.denyRules.map((rule) => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            contentTypeAliases: rule.contentTypeAliases,
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
            instructions: response.instructions,
            profileId: response.profileId ?? null,
            contextIds: response.contextIds ?? [],
            tags: response.tags ?? [],
            scope: mapScopeFromApi(response.scope),
            isActive: response.isActive,
            includeEntityContext: response.includeEntityContext,
            optionCount: response.optionCount ?? 1,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version,
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
            dateModified: response.dateModified,
        };
    },

    toCreateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            instructions: model.instructions,
            description: model.description,
            profileId: model.profileId,
            contextIds: model.contextIds,
            tags: model.tags,
            scope: mapScopeToApi(model.scope),
            includeEntityContext: model.includeEntityContext,
            optionCount: model.optionCount,
        };
    },

    toUpdateRequest(model: UaiPromptDetailModel) {
        return {
            alias: model.alias,
            name: model.name,
            instructions: model.instructions,
            description: model.description,
            profileId: model.profileId,
            contextIds: model.contextIds,
            tags: model.tags,
            scope: mapScopeToApi(model.scope),
            isActive: model.isActive,
            includeEntityContext: model.includeEntityContext,
            optionCount: model.optionCount,
        };
    },
};
