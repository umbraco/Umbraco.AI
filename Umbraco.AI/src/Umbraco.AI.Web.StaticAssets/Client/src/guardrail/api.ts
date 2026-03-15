/**
 * Manual API service for Guardrail endpoints.
 * This replaces the auto-generated SDK service until the OpenAPI client is regenerated.
 *
 * Note: The hey-api client expects response types as `{ statusCode: ActualType }` objects.
 * Using `client.get<T>` directly with T as the response body will cause `data` to be typed
 * as `T[keyof T]`. The correct pattern is `client.get<{ 200: T }, ErrorType, false>()`.
 */
import { client } from "../api/client.gen.js";

const BASE_URL = "/umbraco/ai/management/api/v1";
const SECURITY = [{ scheme: "bearer" as const, type: "http" as const }];

export interface GuardrailRuleApiModel {
    id: string;
    evaluatorId: string;
    name: string;
    phase: string;
    action: string;
    config: Record<string, unknown> | null;
    sortOrder: number;
}

export interface GuardrailResponseApiModel {
    id: string;
    alias: string;
    name: string;
    rules: GuardrailRuleApiModel[];
    dateCreated: string;
    dateModified: string;
    version: number;
}

export interface GuardrailItemResponseApiModel {
    id: string;
    alias: string;
    name: string;
    ruleCount: number;
    dateCreated: string;
    dateModified: string;
}

export interface PagedGuardrailItemResponseApiModel {
    items: GuardrailItemResponseApiModel[];
    total: number;
}

export interface GuardrailEvaluatorInfoApiModel {
    id: string;
    name: string;
    description: string | null;
    type: string;
    configSchema: Record<string, unknown> | null;
}

export interface CreateGuardrailRequestApiModel {
    alias: string;
    name: string;
    rules: GuardrailRuleApiModel[];
}

export interface UpdateGuardrailRequestApiModel {
    alias: string;
    name: string;
    rules: GuardrailRuleApiModel[];
}

type Errors = { [statusCode: number]: unknown };

export const GuardrailsApiService = {
    getAllGuardrails(options?: { query?: { filter?: string; skip?: number; take?: number } }) {
        return client.get<{ 200: PagedGuardrailItemResponseApiModel }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail`,
            ...options,
        });
    },

    getGuardrailById(options: { path: { id: string } }) {
        return client.get<{ 200: GuardrailResponseApiModel }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail/{id}`,
            ...options,
        });
    },

    createGuardrail(options: { body: CreateGuardrailRequestApiModel }) {
        return client.post<{ 201: void }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail`,
            ...options,
        });
    },

    updateGuardrail(options: { path: { id: string }; body: UpdateGuardrailRequestApiModel }) {
        return client.put<{ 200: void }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail/{id}`,
            ...options,
        });
    },

    deleteGuardrail(options: { path: { id: string } }) {
        return client.delete<{ 200: void }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail/{id}`,
            ...options,
        });
    },

    guardrailAliasExists(options: { path: { alias: string }; query?: { excludeId?: string } }) {
        return client.get<{ 200: boolean }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail/{alias}/exists`,
            ...options,
        });
    },

    getAllGuardrailEvaluators() {
        return client.get<{ 200: GuardrailEvaluatorInfoApiModel[] }, Errors, false>({
            security: SECURITY,
            url: `${BASE_URL}/guardrail-evaluators`,
        });
    },
};
