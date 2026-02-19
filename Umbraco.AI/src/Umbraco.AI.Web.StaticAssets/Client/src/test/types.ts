import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { TestGraderModel } from "../api/types.gen.js";

/**
 * Detail model for test workspace editing.
 */
export interface UaiTestDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description?: string | null;
    testFeatureId: string;
    testTargetId: string; // ID or alias of the target entity (prompt, agent, etc.)
    testCase: Record<string, any> | null;
    graders: TestGraderModel[];
    runCount: number;
    tags: string[];
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

/**
 * Collection item model for test list view.
 */
export interface UaiTestItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    testFeatureId: string;
    tags: string[];
    runCount: number;
    dateModified: string | null;
}

/**
 * Test feature item model for feature selection.
 */
export interface UaiTestFeatureItemModel {
    id: string;
    name: string;
    description?: string | null;
    category?: string | null;
}

/**
 * Frontend representation of a test grader configuration.
 * Maps to AITestGrader C# model.
 */
export interface UaiTestGraderConfig {
    id: string;                          // Guid - unique ID
    graderTypeId: string;                // ID of grader implementation
    name: string;                        // Display name
    description?: string;                // Optional description
    config?: Record<string, unknown>;    // Configuration object for grader type
    negate: boolean;                     // Invert pass/fail
    severity: "Info" | "Warning" | "Error";  // Severity level
    weight: number;                      // Scoring weight (default 1.0)
}

/**
 * Information about an available grader type.
 */
export interface UaiTestGraderTypeInfo {
    id: string;
    name: string;
    description?: string;
    type: "CodeBased" | "ModelBased";
}

/**
 * Creates an empty grader configuration with default values.
 */
export function createEmptyGraderConfig(): UaiTestGraderConfig {
    return {
        id: crypto.randomUUID(),
        graderTypeId: "",
        name: "",
        description: undefined,
        config: undefined,
        negate: false,
        severity: "Error",
        weight: 1.0,
    };
}

/**
 * Generates a human-readable summary for a grader.
 */
export function getGraderSummary(grader: UaiTestGraderConfig, typeName?: string): string {
    const parts: string[] = [];

    parts.push(grader.name || "Unnamed grader");
    if (typeName) {
        parts.push(`(${typeName})`);
    }
    parts.push(`${grader.severity}`);

    if (grader.negate) {
        parts.push("(Negated)");
    }

    return parts.join(" ");
}
