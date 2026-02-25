import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestRunComparisonResponseModel, TestGraderComparisonResponseModel } from "../../../api/types.gen.js";

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

    private _repository!: AITestRepository;

    constructor() {
        super();
        this._repository = new AITestRepository(this);
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
        try {
            this._comparison = await this._repository.compareRuns(
                this.baselineRunId!,
                this.comparisonRunId!,
            );
        } catch (error) {
            console.error("Failed to load comparison:", error);
            this._error = "Failed to load comparison data.";
        } finally {
            this._isLoading = false;
        }
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
        const prefix = delta > 0 ? "+" : "";
        return `${prefix}${this._formatDuration(Math.abs(delta))}`;
    }

    private _renderSummaryBanner() {
        if (!this._comparison) return nothing;

        const { isRegression, isImprovement } = this._comparison;

        let color: string;
        let label: string;
        let icon: string;

        if (isRegression) {
            color = "danger";
            label = "Regression";
            icon = "icon-arrow-down";
        } else if (isImprovement) {
            color = "positive";
            label = "Improvement";
            icon = "icon-arrow-up";
        } else {
            color = "default";
            label = "No Change";
            icon = "icon-check";
        }

        return html`
            <div class="summary-banner">
                <uui-tag color=${color} look="primary">
                    <uui-icon name=${icon}></uui-icon>
                    ${label}
                </uui-tag>
            </div>
        `;
    }

    private _renderStatusComparison() {
        if (!this._comparison) return nothing;

        const { baselineRun, comparisonRun, durationChangeMs } = this._comparison;

        return html`
            <uui-box headline="Run Comparison">
                <div class="comparison-grid">
                    <div class="comparison-row">
                        <span class="comparison-label">Status</span>
                        <span class="comparison-value">
                            <uui-tag color=${this._getStatusColor(baselineRun.status)} look="primary">${baselineRun.status}</uui-tag>
                        </span>
                        <span class="comparison-arrow">
                            <uui-icon name="icon-arrow-right"></uui-icon>
                        </span>
                        <span class="comparison-value">
                            <uui-tag color=${this._getStatusColor(comparisonRun.status)} look="primary">${comparisonRun.status}</uui-tag>
                        </span>
                    </div>
                    <div class="comparison-row">
                        <span class="comparison-label">Duration</span>
                        <span class="comparison-value">${this._formatDuration(baselineRun.durationMs)}</span>
                        <span class="comparison-arrow">
                            <uui-icon name="icon-arrow-right"></uui-icon>
                        </span>
                        <span class="comparison-value">
                            ${this._formatDuration(comparisonRun.durationMs)}
                            <span class="delta ${durationChangeMs > 0 ? "negative" : durationChangeMs < 0 ? "positive" : ""}"
                                >(${this._formatDelta(durationChangeMs)})</span
                            >
                        </span>
                    </div>
                    <div class="comparison-row">
                        <span class="comparison-label">Run #</span>
                        <span class="comparison-value">#${baselineRun.runNumber}</span>
                        <span class="comparison-arrow">
                            <uui-icon name="icon-arrow-right"></uui-icon>
                        </span>
                        <span class="comparison-value">#${comparisonRun.runNumber}</span>
                    </div>
                </div>
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
                ${this._renderSummaryBanner()}
                ${this._renderStatusComparison()}
                ${this._renderGraderComparisons()}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
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

        .summary-banner {
            margin-bottom: 16px;
        }

        .summary-banner uui-tag {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            font-size: 14px;
            padding: 8px 16px;
        }

        uui-box {
            margin-top: 20px;
        }

        .comparison-grid {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .comparison-row {
            display: grid;
            grid-template-columns: 100px 1fr auto 1fr;
            align-items: center;
            gap: 12px;
        }

        .comparison-label {
            font-size: 12px;
            font-weight: 500;
            color: var(--uui-color-text-alt);
            text-transform: uppercase;
        }

        .comparison-value {
            display: flex;
            align-items: center;
            gap: 6px;
        }

        .comparison-arrow {
            display: flex;
            align-items: center;
            color: var(--uui-color-text-alt);
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

        .grader-list {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .grader-card {
            border: 1px solid var(--uui-color-border);
            border-radius: 6px;
            padding: 16px;
        }

        .grader-card.changed {
            border-color: var(--uui-color-border-emphasis);
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
