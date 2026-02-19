import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import type { TestMetricsResponseModel } from "../../api/types.gen.js";

/**
 * Test run results viewer displaying aggregate metrics.
 * Shows pass@k, pass^k, and links to individual run details.
 */
@customElement("umbraco-ai-test-run-results")
export class UmbracoAITestRunResultsElement extends UmbElementMixin(LitElement) {
    @property({ type: Object })
    metrics?: TestMetricsResponseModel;

    private _renderMetrics() {
        if (!this.metrics) {
            return html`<div class="empty">No metrics available</div>`;
        }

        const passPercentage = this.metrics.totalRuns > 0
            ? (this.metrics.passedRuns / this.metrics.totalRuns) * 100
            : 0;

        return html`
            <div class="metrics-container">
                <div class="metric-card">
                    <div class="metric-label">Total Runs</div>
                    <div class="metric-value">${this.metrics.totalRuns}</div>
                </div>

                <div class="metric-card">
                    <div class="metric-label">Passed Runs</div>
                    <div class="metric-value passed">
                        ${this.metrics.passedRuns} / ${this.metrics.totalRuns}
                    </div>
                </div>

                <div class="metric-card">
                    <div class="metric-label">pass@k</div>
                    <div class="metric-value">
                        ${(this.metrics.passAtK * 100).toFixed(1)}%
                    </div>
                    <div class="metric-description">
                        Probability of ≥1 success
                    </div>
                </div>

                <div class="metric-card">
                    <div class="metric-label">pass^k</div>
                    <div class="metric-value">
                        ${(this.metrics.passToTheK * 100).toFixed(1)}%
                    </div>
                    <div class="metric-description">
                        Probability all succeed
                    </div>
                </div>
            </div>

            <div class="progress-bar">
                <div
                    class="progress-fill ${passPercentage === 100 ? 'success' : passPercentage > 0 ? 'partial' : 'failure'}"
                    style="width: ${passPercentage}%"
                ></div>
            </div>
        `;
    }

    private _renderRunsList() {
        if (!this.metrics || this.metrics.runIds.length === 0) {
            return html``;
        }

        return html`
            <div class="runs-section">
                <h3>Individual Runs</h3>
                <div class="runs-list">
                    ${this.metrics.runIds.map(
                        (runId, index) => html`
                            <div class="run-item">
                                <span class="run-number">Run ${index + 1}</span>
                                <a
                                    href="#/section/ai/workspace/test-run/${runId}"
                                    class="run-link"
                                >
                                    View Details →
                                </a>
                            </div>
                        `
                    )}
                </div>
            </div>
        `;
    }

    render() {
        return html`
            <div class="container">
                <h2>Test Execution Results</h2>
                ${this._renderMetrics()}
                ${this._renderRunsList()}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        .container {
            padding: 20px;
        }

        h2 {
            margin-top: 0;
            margin-bottom: 20px;
        }

        h3 {
            margin-top: 30px;
            margin-bottom: 15px;
        }

        .empty {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
        }

        .metrics-container {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 20px;
        }

        .metric-card {
            background: var(--uui-color-surface);
            padding: 20px;
            border-radius: 8px;
            border: 1px solid var(--uui-color-border);
        }

        .metric-label {
            font-size: 14px;
            color: var(--uui-color-text-alt);
            margin-bottom: 8px;
            font-weight: 500;
        }

        .metric-value {
            font-size: 32px;
            font-weight: 600;
            color: var(--uui-color-text);
        }

        .metric-value.passed {
            color: var(--uui-color-positive);
        }

        .metric-description {
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-top: 5px;
        }

        .progress-bar {
            height: 8px;
            background: var(--uui-color-surface-alt);
            border-radius: 4px;
            overflow: hidden;
            margin-bottom: 30px;
        }

        .progress-fill {
            height: 100%;
            transition: width 0.3s ease;
        }

        .progress-fill.success {
            background: var(--uui-color-positive);
        }

        .progress-fill.partial {
            background: var(--uui-color-warning);
        }

        .progress-fill.failure {
            background: var(--uui-color-danger);
        }

        .runs-section {
            background: var(--uui-color-surface);
            padding: 20px;
            border-radius: 8px;
            border: 1px solid var(--uui-color-border);
        }

        .runs-list {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .run-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px;
            background: var(--uui-color-surface-alt);
            border-radius: 4px;
        }

        .run-number {
            font-weight: 500;
        }

        .run-link {
            color: var(--uui-color-interactive);
            text-decoration: none;
            font-size: 14px;
        }

        .run-link:hover {
            text-decoration: underline;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-run-results": UmbracoAITestRunResultsElement;
    }
}
