import type { UaiGuardrailEntityType } from "./entity.js";

/**
 * Rule within a guardrail.
 */
export interface UaiGuardrailRuleModel {
    id: string;
    evaluatorId: string;
    name: string;
    phase: "PreGenerate" | "PostGenerate";
    action: "Block" | "Warn";
    config: Record<string, unknown> | null;
    sortOrder: number;
}

/**
 * Detail model for a guardrail (full data for editing).
 */
export interface UaiGuardrailDetailModel {
    unique: string;
    entityType: UaiGuardrailEntityType;
    alias: string;
    name: string;
    rules: UaiGuardrailRuleModel[];
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

/**
 * Item model for a guardrail (summary for lists).
 */
export interface UaiGuardrailItemModel {
    unique: string;
    entityType: UaiGuardrailEntityType;
    alias: string;
    name: string;
    ruleCount: number;
    dateCreated: string | null;
    dateModified: string | null;
}

/**
 * Configuration for a guardrail rule (used in the rule config builder/editor).
 */
export interface UaiGuardrailRuleConfig {
    id: string;
    evaluatorId: string;
    name: string;
    phase: "PreGenerate" | "PostGenerate";
    action: "Block" | "Warn";
    config: Record<string, unknown> | null;
    sortOrder: number;
}

/**
 * Creates an empty rule configuration with default values.
 */
export function createEmptyRuleConfig(): UaiGuardrailRuleConfig {
    return {
        id: crypto.randomUUID(),
        evaluatorId: "",
        name: "",
        phase: "PostGenerate",
        action: "Block",
        config: null,
        sortOrder: 0,
    };
}

/**
 * Returns a human-readable summary of a rule configuration.
 */
export function getRuleSummary(rule: UaiGuardrailRuleConfig, evaluatorName?: string): string {
    const parts: string[] = [];
    if (evaluatorName) parts.push(evaluatorName);
    parts.push(rule.phase);
    parts.push(rule.action);
    return parts.join(" · ");
}
