import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
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
    profileId?: string | null;
    contextIds: string[];
    testFeatureConfig: Record<string, any> | null;
    graders: TestGraderModel[];
    variations: UaiTestVariation[];
    runCount: number;
    tags: string[];
    baselineRunId?: string | null;
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

/**
 * Frontend representation of a test variation configuration.
 * Maps to AITestVariation C# model.
 */
export interface UaiTestVariation {
    id: string;
    name: string;
    description?: string | null;
    profileId?: string | null;
    runCount?: number | null;
    contextIds?: string[] | null;
    testFeatureConfig?: Record<string, any> | null;
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
 * Collection item model for test run list view.
 */
export interface UaiTestRunItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    testId: string;
    testName?: string | null;
    runNumber: number;
    status: string;
    durationMs: number;
    executedAt: string;
    batchId?: string | null;
    executionId?: string | null;
    variationId?: string | null;
    variationName?: string | null;
    profileId?: string | null;
    isBaseline: boolean;
    baselineRunId?: string | null;
}

/**
 * Filter model for scoped runs collection.
 * Extends the standard collection filter with optional test scoping.
 */
export interface UaiTestRunCollectionFilterModel extends UmbCollectionFilterModel {
    test?: { unique: string } | null;
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
 * Creates an empty variation configuration with default values.
 */
export function createEmptyVariation(): UaiTestVariation {
    return {
        id: crypto.randomUUID(),
        name: "",
        description: null,
        profileId: null,
        runCount: null,
        contextIds: null,
        testFeatureConfig: null,
    };
}

/**
 * Generates a human-readable summary for a variation.
 * Shows which fields are overridden from the default config.
 */
export function getVariationSummary(variation: UaiTestVariation): string {
    const overrides: string[] = [];
    if (variation.profileId) overrides.push("Profile");
    if (variation.contextIds?.length) overrides.push("Contexts");
    if (variation.runCount != null) overrides.push(`Run Count: ${variation.runCount}`);
    if (variation.testFeatureConfig) overrides.push("Feature Config");
    return overrides.length > 0 ? `Overrides: ${overrides.join(", ")}` : "No overrides (inherits all defaults)";
}

/**
 * Generates a human-readable summary for a grader.
 * Returns type, severity, weight, and negate status (name is shown separately in UI).
 */
export function getGraderSummary(grader: UaiTestGraderConfig, typeName?: string): string {
    const parts: string[] = [];

    if (typeName) {
        parts.push(typeName);
    }

    parts.push(`Severity: ${grader.severity}`);

    if (grader.weight !== 1.0) {
        parts.push(`Weight: ${grader.weight}`);
    }

    if (grader.negate) {
        parts.push("(Negated)");
    }

    return parts.join(" • ");
}
