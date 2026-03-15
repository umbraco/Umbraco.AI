import type {
    GuardrailResponseApiModel,
    GuardrailItemResponseApiModel,
    GuardrailRuleApiModel,
    CreateGuardrailRequestApiModel,
    UpdateGuardrailRequestApiModel,
} from "./api.js";
import { UAI_GUARDRAIL_ENTITY_TYPE } from "./constants.js";
import type { UaiGuardrailDetailModel, UaiGuardrailItemModel, UaiGuardrailRuleModel } from "./types.js";

export const UaiGuardrailTypeMapper = {
    toDetailModel(response: GuardrailResponseApiModel): UaiGuardrailDetailModel {
        return {
            unique: response.id,
            entityType: UAI_GUARDRAIL_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            rules: (response.rules ?? []).map(this.toRuleModel),
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
            version: response.version ?? 1,
        };
    },

    toItemModel(response: GuardrailItemResponseApiModel): UaiGuardrailItemModel {
        return {
            unique: response.id,
            entityType: UAI_GUARDRAIL_ENTITY_TYPE,
            alias: response.alias,
            name: response.name,
            ruleCount: response.ruleCount ?? 0,
            dateCreated: response.dateCreated,
            dateModified: response.dateModified,
        };
    },

    toRuleModel(rule: GuardrailRuleApiModel): UaiGuardrailRuleModel {
        return {
            id: rule.id,
            evaluatorId: rule.evaluatorId,
            name: rule.name,
            phase: rule.phase as "PreGenerate" | "PostGenerate",
            action: rule.action as "Block" | "Warn",
            config: (rule.config as Record<string, unknown>) ?? null,
            sortOrder: rule.sortOrder,
        };
    },

    toCreateRequest(model: UaiGuardrailDetailModel): CreateGuardrailRequestApiModel {
        return {
            alias: model.alias,
            name: model.name,
            rules: model.rules.map(this.toRuleRequest),
        };
    },

    toUpdateRequest(model: UaiGuardrailDetailModel): UpdateGuardrailRequestApiModel {
        return {
            alias: model.alias,
            name: model.name,
            rules: model.rules.map(this.toRuleRequest),
        };
    },

    toRuleRequest(rule: UaiGuardrailRuleModel): GuardrailRuleApiModel {
        return {
            id: rule.id,
            evaluatorId: rule.evaluatorId,
            name: rule.name,
            phase: rule.phase,
            action: rule.action,
            config: rule.config,
            sortOrder: rule.sortOrder,
        };
    },
};
