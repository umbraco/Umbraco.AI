import type {
    GuardrailResponseModel,
    GuardrailItemResponseModel,
    GuardrailRuleModel,
    CreateGuardrailRequestModel,
    UpdateGuardrailRequestModel,
} from "../api";
import { UAI_GUARDRAIL_ENTITY_TYPE } from "./constants.js";
import type { UaiGuardrailDetailModel, UaiGuardrailItemModel, UaiGuardrailRuleModel } from "./types.js";

export const UaiGuardrailTypeMapper = {
    toDetailModel(response: GuardrailResponseModel): UaiGuardrailDetailModel {
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

    toItemModel(response: GuardrailItemResponseModel): UaiGuardrailItemModel {
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

    toRuleModel(rule: GuardrailRuleModel): UaiGuardrailRuleModel {
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

    toCreateRequest(model: UaiGuardrailDetailModel): CreateGuardrailRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            rules: model.rules.map(this.toRuleRequest),
        };
    },

    toUpdateRequest(model: UaiGuardrailDetailModel): UpdateGuardrailRequestModel {
        return {
            alias: model.alias,
            name: model.name,
            rules: model.rules.map(this.toRuleRequest),
        };
    },

    toRuleRequest(rule: UaiGuardrailRuleModel): GuardrailRuleModel {
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
