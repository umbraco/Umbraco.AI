/**
 * Shared weighted-score calculation for test runs. Used by the run detail view and the run
 * mapper (list and overview) so every surface reports the same number.
 */

/** The grader fields the calculation needs. */
export interface UaiWeightedScoreGraderResult {
    score: number;
    weight: number;
}

/** The run fields the mean calculation needs. */
export interface UaiWeightedScoreRun {
    status: string;
    weightedScore?: number | null;
}

/**
 * Weighted score of a single run: sum(score * weight) / sum(weight), as a 0..1 fraction.
 * Returns null when there are no grader results (e.g. a run that errored before grading), and 0 when
 * the total weight is 0.
 */
export function computeWeightedScore(
    graderResults: readonly UaiWeightedScoreGraderResult[] | null | undefined,
): number | null {
    if (!graderResults || graderResults.length === 0) return null;

    const totalWeight = graderResults.reduce((sum, r) => sum + r.weight, 0);
    if (totalWeight <= 0) return 0;

    return graderResults.reduce((sum, r) => sum + r.score * r.weight, 0) / totalWeight;
}

/** Whether a run has finished (passed, failed or errored). Shared by the pass metrics and the mean score. */
export function isCompletedRun(run: { status: string }): boolean {
    const status = run.status.toLowerCase();
    return status === "passed" || status === "failed" || status === "error";
}

/**
 * Mean weighted score across a test's runs, as a 0..1 fraction, or null when no run has completed.
 *
 * Runs are counted the same way the pass metrics count them: every completed run
 * (passed, failed or error) is in the denominator; running/pending runs are ignored. A completed run
 * with no weighted score (it errored before any grader ran) counts as 0, just as pass@k treats an
 * errored run as "not passed" rather than dropping it. Failed runs use their real score.
 */
export function computeMeanWeightedScore(runs: readonly UaiWeightedScoreRun[]): number | null {
    const completed = runs.filter(isCompletedRun);
    if (completed.length === 0) return null;

    return completed.reduce((sum, r) => sum + (r.weightedScore ?? 0), 0) / completed.length;
}
