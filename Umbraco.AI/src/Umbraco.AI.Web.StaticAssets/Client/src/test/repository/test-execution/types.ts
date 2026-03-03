/**
 * Frontend representation of test execution result.
 * Maps to TestExecutionResultResponseModel from API.
 * TODO: Replace with generated type after API client regeneration.
 */
export interface UaiTestExecutionResult {
    testId: string;
    executionId: string;
    batchId?: string | null;
    defaultMetrics: UaiTestMetrics;
    variationMetrics: UaiTestVariationMetrics[];
    aggregateMetrics: UaiTestMetrics;
}

export interface UaiTestMetrics {
    testId: string;
    totalRuns: number;
    passedRuns: number;
    passAtK: number;
    passToTheK: number;
    runIds: string[];
}

export interface UaiTestVariationMetrics {
    variationId: string;
    variationName: string;
    metrics: UaiTestMetrics;
}
