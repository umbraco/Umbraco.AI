import type { PromptResponseModel, PromptItemResponseModel, ScopeModel } from "../api/types.gen.js";
import { UAI_PROMPT_ENTITY_TYPE } from "./constants.js";
import type { UAiAgentScope, UaiScopeRule } from "./property-actions/types.js";
import type { UAiAgentDetailModel, UAiAgentItemModel } from "./types.js";

/**
 * Maps API scope model to internal scope model.
 */
function mapScopeFromApi(apiScope: ScopeModel | null | undefined): UAiAgentScope | null {
    if (!apiScope) return null;

    return {
        allowRules: (apiScope.allowRules ?? []).map(mapScopeRuleFromApi),
        denyRules: (apiScope.denyRules ?? []).map(mapScopeRuleFromApi),
    };
}

function mapScopeRuleFromApi(rule: { propertyEditorUiAliases?: string[] | null; propertyAliases?: string[] | null; contentTypeAliases?: string[] | null }): UaiScopeRule {
    return {
        propertyEditorUiAliases: rule.propertyEditorUiAliases ?? null,
        propertyAliases: rule.propertyAliases ?? null,
        contentTypeAliases: rule.contentTypeAliases ?? null,
    };
}

/**
 * Maps internal scope model to API scope model.
 */
function mapScopeToApi(scope: UAiAgentScope | null): ScopeModel | null {
    if (!scope) return null;

    return {
        allowRules: scope.allowRules.map(rule => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            contentTypeAliases: rule.contentTypeAliases,
        })),
        denyRules: scope.denyRules.map(rule => ({
            propertyEditorUiAliases: rule.propertyEditorUiAliases,
            propertyAliases: rule.propertyAliases,
            contentTypeAliases: rule.contentTypeAliases,
        })),
    };
}

export const UAiAgentTypeMapper = {
    toDetailModel(response: PromptResponseModel): UAiAgentDetailModel {
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

    toItemModel(response: PromptItemResponseModel): UAiAgentItemModel {
        return {
            unique: response.id,
            entityType: UAI_PROMPT_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            description: response.description ?? null,
            isActive: response.isActive,
        };
    },

    toCreateRequest(model: UAiAgentDetailModel) {
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

    toUpdateRequest(model: UAiAgentDetailModel) {
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
