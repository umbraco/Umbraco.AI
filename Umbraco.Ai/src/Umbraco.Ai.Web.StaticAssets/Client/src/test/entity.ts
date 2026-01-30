/**
 * Test entity types matching API response models
 */

export interface TestResponseModel {
    id: string;
    alias: string;
    name: string;
    description?: string;
    testTypeId: string;
    target: TestTargetModel;
    testCase: TestCaseModel;
    graders: TestGraderModel[];
    runCount: number;
    tags: string[];
    isEnabled: boolean;
    baselineRunId?: string;
    dateCreated: string;
    dateModified: string;
    createdByUserId?: string;
    modifiedByUserId?: string;
    version: number;
}

export interface TestTargetModel {
    targetId: string;
    isAlias: boolean;
}

export interface TestCaseModel {
    testCaseJson: string;
}

export interface TestGraderModel {
    id: string;
    graderTypeId: string;
    name: string;
    description?: string;
    configJson: string;
    negate: boolean;
    severity: number;
    weight: number;
    sortOrder: number;
}

export interface TestRunResponseModel {
    id: string;
    testId: string;
    testVersion: number;
    runNumber: number;
    profileId: string;
    contextIdsJson?: string;
    executedAt: string;
    executedByUserId?: string;
    durationMs: number;
    status: number;
    errorMessage?: string;
    outcome?: TestOutcomeModel;
    transcript?: TestTranscriptModel;
    graderResults: TestGraderResultModel[];
    batchId?: string;
}

export interface TestOutcomeModel {
    outputType: number;
    outputValue: string;
    finishReason?: string;
    inputTokens?: number;
    outputTokens?: number;
}

export interface TestTranscriptModel {
    id: string;
    runId: string;
    messagesJson: string;
    toolCallsJson?: string;
    reasoningJson?: string;
    timingJson?: string;
    finalOutputJson: string;
}

export interface TestGraderResultModel {
    graderId: string;
    passed: boolean;
    score?: number;
    actualValue?: string;
    expectedValue?: string;
    failureMessage?: string;
    metadataJson?: string;
}

export interface TestMetricsResponseModel {
    testId: string;
    totalRuns: number;
    passedRuns: number;
    passAtK: number;
    passToTheK: number;
    runIds: string[];
}
