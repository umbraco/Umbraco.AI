import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UaiTestRunDetailRepository } from "../../repository/test-run-detail/test-run-detail.repository.js";
import type {
    TestRunComparisonResponseModel,
    TestGraderComparisonResponseModel,
    TestUsageEntryComparisonResponseModel,
} from "../../../api/types.gen.js";

/**
 * Component that displays a side-by-side comparison between a baseline and comparison test run.
 */
@customElement("uai-test-run-comparison")
export class UaiTestRunComparisonElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    baselineRunId?: string;

    @property({ type: String })
    comparisonRunId?: string;

    @state()
    private _comparison?: TestRunComparisonResponseModel;

    @state()
    private _isLoading = true;

    @state()
    private _error?: string;

    private _repository!: UaiTestRunDetailRepository;

    constructor() {
        super();
        this._repository = new UaiTestRunDetailRepository(this);
    }

    async connectedCallback() {
        super.connectedCallback();
        if (this.baselineRunId && this.comparisonRunId) {
            await this._loadComparison();
        }
    }

    private async _loadComparison() {
        this._isLoading = true;
        this._error = undefined;
        const { data, error } = await this._repository.requestComparison(
            this.baselineRunId!,
            this.comparisonRunId!,
        );
        if (error) {
            console.error("Failed to load comparison:", error);
            this._error = "Failed to load comparison data.";
        } else {
            this._comparison = data;
        }
        this._isLoading = false;
    }

    private _getStatusColor(status: string): string {
        switch (status.toLowerCase()) {
            case "passed": return "positive";
            case "failed":
            case "error": return "danger";
            case "running": return "warning";
            default: return "default";
        }
    }

    private _formatDuration(ms: number): string {
        if (ms < 1000) return `${ms}ms`;
        if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
        return `${(ms / 60000).toFixed(1)}m`;
    }

    private _formatDelta(delta: number): string {
        const prefix = delta > 0 ? "+" : "-";
        return `${prefix}${this._formatDuration(Math.abs(delta))}`;
    }

    private _getVerdict() {
        if (!this._comparison) return { color: "default", label: "Unknown" };
        const { isRegression, isImprovement } = this._comparison;

        if (isRegression) return { color: "danger", label: "Regression" };
        if (isImprovement) return { color: "positive", label: "Improvement" };
        return { color: "default", label: "No Change" };
    }

    private _renderSummary() {
        if (!this._comparison) return nothing;

        const { baselineRun, comparisonRun, durationChangeMs } = this._comparison;
        const verdict = this._getVerdict();

        return html`
            <uui-box headline="Result">
                <uui-tag slot="header-actions" color=${verdict.color} look="primary">
                    ${verdict.label}
                </uui-tag>
                <div class="summary-metrics">
                    <div class="metric-item">
                        <span class="metric-label">Runs</span>
                        <span class="metric-value">
                            #${baselineRun.runNumber}
                            <uui-icon name="icon-arrow-right" class="metric-arrow"></uui-icon>
                            #${comparisonRun.runNumber}
                        </span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label">Status</span>
                        <span class="metric-value">
                            <uui-tag color=${this._getStatusColor(baselineRun.status)} look="primary">${baselineRun.status}</uui-tag>
                            <uui-icon name="icon-arrow-right" class="metric-arrow"></uui-icon>
                            <uui-tag color=${this._getStatusColor(comparisonRun.status)} look="primary">${comparisonRun.status}</uui-tag>
                        </span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label" title="Overall run time, including grading">Run duration</span>
                        <span class="metric-value">
                            ${this._formatDuration(baselineRun.durationMs)}
                            <uui-icon name="icon-arrow-right" class="metric-arrow"></uui-icon>
                            ${this._formatDuration(comparisonRun.durationMs)}
                            ${durationChangeMs !== 0
                                ? html`<span class="delta ${durationChangeMs > 0 ? "negative" : "positive"}">${this._formatDelta(durationChangeMs)}</span>`
                                : nothing}
                        </span>
                    </div>
                </div>
            </uui-box>
        `;
    }

    /** Renders "before → after" with a signed delta, where an increase is shown as worse. */
    private _renderUsageMetric(label: string, baseline: number, comparison: number, delta: number, format: (n: number) => string, title?: string) {
        return html`
            <div class="metric-item">
                <span class="metric-label" title=${title ?? nothing}>${label}</span>
                <span class="metric-value">
                    ${format(baseline)}
                    <uui-icon name="icon-arrow-right" class="metric-arrow"></uui-icon>
                    ${format(comparison)}
                    ${delta !== 0
                        ? html`<span class="delta ${delta > 0 ? "negative" : "positive"}">${delta > 0 ? "+" : "-"}${format(Math.abs(delta))}</span>`
                        : nothing}
                </span>
            </div>
        `;
    }

    private _formatCount = (n: number) => n.toLocaleString();

    private _renderUsageEntry(entry: TestUsageEntryComparisonResponseModel) {
        const name = [entry.providerId, entry.modelId].filter(Boolean).join(" / ") || entry.capability;
        const feature = entry.featureAlias ?? entry.featureType;
        const side = !entry.baselineEntry
            ? html`<uui-tag look="outline" color="warning">Only in comparison</uui-tag>`
            : !entry.comparisonEntry
              ? html`<uui-tag look="outline" color="warning">Only in baseline</uui-tag>`
              : nothing;

        return html`
            <div class="usage-entry">
                <div class="usage-entry-name">
                    <strong>${name}</strong>
                    ${feature ? html`<span class="usage-entry-feature">${feature}</span>` : nothing}
                    ${side}
                </div>
                <span class="metric-value">
                    ${this._formatCount(entry.baselineEntry?.totalTokens ?? 0)}
                    <uui-icon name="icon-arrow-right" class="metric-arrow"></uui-icon>
                    ${this._formatCount(entry.comparisonEntry?.totalTokens ?? 0)} tokens
                </span>
            </div>
        `;
    }

    private _renderUsage() {
        if (!this._comparison) return nothing;

        const { baselineRun, comparisonRun, usageComparison } = this._comparison;
        const baseline = baselineRun.outcome?.usage;
        const comparison = comparisonRun.outcome?.usage;

        if (!usageComparison || !baseline || !comparison) {
            return html`
                <uui-box headline="AI Usage">
                    <div class="usage-note">
                        Not available. One or both runs have no recorded AI usage.
                    </div>
                </uui-box>
            `;
        }

        return html`
            <uui-box headline="AI Usage">
                <div class="summary-metrics">
                    ${this._renderUsageMetric("Total tokens", baseline.totalTokens, comparison.totalTokens, usageComparison.totalTokensChange, this._formatCount)}
                    ${this._renderUsageMetric("Input tokens", baseline.inputTokens, comparison.inputTokens, usageComparison.inputTokensChange, this._formatCount)}
                    ${this._renderUsageMetric("Output tokens", baseline.outputTokens, comparison.outputTokens, usageComparison.outputTokensChange, this._formatCount)}
                    ${this._renderUsageMetric(
                        "AI call time",
                        baseline.durationMs,
                        comparison.durationMs,
                        usageComparison.callDurationChangeMs,
                        (n) => this._formatDuration(n),
                        "Summed time of the AI calls, excluding grading",
                    )}
                    ${this._renderUsageMetric("Failed calls", baseline.failedCallCount, comparison.failedCallCount, usageComparison.failedCallCountChange, this._formatCount)}
                </div>
                ${usageComparison.hasUnreportedCalls
                    ? html`<div class="usage-note">Some calls reported no usage, so token figures are approximate.</div>`
                    : nothing}
                ${usageComparison.breakdownChanged
                    ? html`
                          <div class="usage-note">These runs used different models or features.</div>
                          <div class="usage-entries">
                              ${usageComparison.entries.map((e) => this._renderUsageEntry(e))}
                          </div>
                      `
                    : nothing}
            </uui-box>
        `;
    }

    private _renderGraderComparison(gc: TestGraderComparisonResponseModel) {
        const baselineScore = gc.baselineResult?.score ?? 0;
        const comparisonScore = gc.comparisonResult?.score ?? 0;
        const baselinePassed = gc.baselineResult?.passed ?? false;
        const comparisonPassed = gc.comparisonResult?.passed ?? false;
        const scoreDelta = gc.scoreChange;
        const scorePercent = (val: number) => (val * 100).toFixed(1);

        return html`
            <div class="grader-card ${gc.changed ? "changed" : ""}">
                <div class="grader-header">
                    <strong>${gc.graderName || gc.graderId}</strong>
                    ${gc.changed
                        ? html`<uui-tag look="outline" color=${scoreDelta > 0 ? "positive" : scoreDelta < 0 ? "danger" : "default"}>
                            ${scoreDelta > 0 ? "+" : ""}${scorePercent(scoreDelta)}%
                        </uui-tag>`
                        : html`<uui-tag look="outline" color="default">No change</uui-tag>`}
                </div>
                <div class="grader-details">
                    <div class="grader-metric">
                        <span class="grader-metric-label">Score</span>
                        <span class="grader-metric-values">
                            ${scorePercent(baselineScore)}%
                            <uui-icon name="icon-arrow-right"></uui-icon>
                            ${scorePercent(comparisonScore)}%
                        </span>
                    </div>
                    <div class="grader-metric">
                        <span class="grader-metric-label">Pass/Fail</span>
                        <span class="grader-metric-values">
                            <uui-tag color=${baselinePassed ? "positive" : "danger"} look="primary">
                                ${baselinePassed ? "Pass" : "Fail"}
                            </uui-tag>
                            <uui-icon name="icon-arrow-right"></uui-icon>
                            <uui-tag color=${comparisonPassed ? "positive" : "danger"} look="primary">
                                ${comparisonPassed ? "Pass" : "Fail"}
                            </uui-tag>
                        </span>
                    </div>
                </div>
            </div>
        `;
    }

    private _renderGraderComparisons() {
        if (!this._comparison?.graderComparisons?.length) return nothing;

        return html`
            <uui-box headline="Grader Comparisons">
                <div class="grader-list">
                    ${this._comparison.graderComparisons.map((gc) => this._renderGraderComparison(gc))}
                </div>
            </uui-box>
        `;
    }

    render() {
        if (this._isLoading) {
            return html`<div class="loading">Loading comparison...</div>`;
        }

        if (this._error) {
            return html`<div class="error">${this._error}</div>`;
        }

        if (!this._comparison) {
            return html`<div class="empty">No comparison data available</div>`;
        }

        return html`
            <div class="container">
                ${this._renderSummary()}
                ${this._renderUsage()}
                ${this._renderGraderComparisons()}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        uui-tag {
            white-space: nowrap;
        }

        .loading,
        .empty,
        .error {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
        }

        .error {
            color: var(--uui-color-danger);
        }

        /* --- Summary metrics --- */

        .summary-metrics {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .metric-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .metric-label {
            font-size: 12px;
            font-weight: 500;
            color: var(--uui-color-text-alt);
            text-transform: uppercase;
        }

        .metric-value {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .metric-arrow {
            color: var(--uui-color-text-alt);
            font-size: 12px;
        }

        .delta {
            font-size: 12px;
            font-weight: 500;
        }

        .delta.positive {
            color: var(--uui-color-positive);
        }

        .delta.negative {
            color: var(--uui-color-danger);
        }

        /* --- Usage --- */

        .usage-note {
            margin-top: 12px;
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        .usage-entries {
            display: flex;
            flex-direction: column;
            gap: 8px;
            margin-top: 8px;
        }

        .usage-entry {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 12px;
            border: 1px solid var(--uui-color-border);
            padding: 8px 12px;
        }

        .usage-entry-name {
            display: flex;
            align-items: center;
            gap: 8px;
            flex-wrap: wrap;
        }

        .usage-entry-feature {
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        /* --- Grader comparisons --- */

        uui-box + uui-box {
            margin-top: 20px;
        }

        .grader-list {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .grader-card {
            border: 1px solid var(--uui-color-border);
            padding: 16px;
        }

        .grader-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 12px;
        }

        .grader-details {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .grader-metric {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .grader-metric-label {
            font-size: 12px;
            color: var(--uui-color-text-alt);
            font-weight: 500;
        }

        .grader-metric-values {
            display: flex;
            align-items: center;
            gap: 8px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-comparison": UaiTestRunComparisonElement;
    }
}
