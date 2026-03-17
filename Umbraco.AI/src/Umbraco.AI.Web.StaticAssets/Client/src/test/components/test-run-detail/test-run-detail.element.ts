import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UaiTestRunDetailRepository } from "../../repository/test-run-detail/test-run-detail.repository.js";
import type { TestRunResponseModel, TestGraderResultResponseModel } from "../../../api/types.gen.js";
import { codeBlockStyles } from "../../../core/styles/code-block.styles.js";


/**
 * Individual test run detail viewer.
 * Shows single run details including outcome, scores, grader results, and transcript reference.
 */
@customElement("uai-test-run-detail")
export class UaiTestRunDetailElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    runId?: string;

    @state()
    private _run?: TestRunResponseModel;

    @state()
    private _isLoading = true;

    private _repository!: UaiTestRunDetailRepository;

    constructor() {
        super();
        this._repository = new UaiTestRunDetailRepository(this);
    }

    async connectedCallback() {
        super.connectedCallback();
        if (this.runId) {
            await this._loadRun();
        }
    }

    private async _loadRun() {
        this._isLoading = true;
        const { data, error } = await this._repository.requestById(this.runId!);
        if (error) {
            console.error("Failed to load run:", error);
        } else {
            this._run = data;
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

    private _renderStatus(status: string) {
        return html`<uui-tag look="primary" color=${this._getStatusColor(status)}>${status}</uui-tag>`;
    }

    private _computeScores(graderResults: TestGraderResultResponseModel[]) {
        if (graderResults.length === 0) return null;

        const totalWeight = graderResults.reduce((sum, r) => sum + r.weight, 0);
        const weightedScore = totalWeight > 0
            ? graderResults.reduce((sum, r) => sum + r.score * r.weight, 0) / totalWeight
            : 0;
        const passed = graderResults.filter((r) => r.passed).length;
        const failed = graderResults.length - passed;

        return { weightedScore, passed, failed, total: graderResults.length };
    }

    private _renderScores() {
        if (!this._run?.graderResults?.length) return nothing;

        const scores = this._computeScores(this._run.graderResults);
        if (!scores) return nothing;

        const percentage = scores.weightedScore * 100;
        const barClass = scores.failed === 0 ? "success" : percentage >= 50 ? "partial" : "failure";

        return html`
            <uui-box headline="Score">
                <div class="scores-container">
                    <div class="scores-grid">
                        <div class="score-card">
                            <div class="score-label">Weighted Score</div>
                            <div class="score-value ${barClass}">${percentage.toFixed(1)}%</div>
                        </div>
                        <div class="score-card">
                            <div class="score-label">Graders Passed</div>
                            <div class="score-value ${scores.failed === 0 ? "success" : "partial"}">${scores.passed} / ${scores.total}</div>
                        </div>
                    </div>
                    <div class="score-bar">
                        <div class="score-bar-fill ${barClass}" style="width: ${percentage}%"></div>
                    </div>
                </div>
            </uui-box>
        `;
    }

    private _renderOutcome() {
        if (!this._run?.outcome) {
            return html`<div class="section-empty">No outcome recorded</div>`;
        }

        const outcome = this._run.outcome;
        return html`
            <div class="outcome-container">
                <uai-labeled-field label="Output Type">${outcome.outputType}</uai-labeled-field>
                ${outcome.outputValue
                    ? html`
                        <uai-labeled-field label="Output Value">
                            <pre class="code-block">${outcome.outputValue}</pre>
                        </uai-labeled-field>
                    `
                    : null}
                ${outcome.finishReason
                    ? html`<uai-labeled-field label="Finish Reason">${outcome.finishReason}</uai-labeled-field>`
                    : null}
                ${outcome.tokenUsage
                    ? html`
                        <uai-labeled-field label="Token Usage">
                            <pre class="code-block">${JSON.stringify(outcome.tokenUsage, null, 2)}</pre>
                        </uai-labeled-field>
                    `
                    : null}
            </div>
        `;
    }

    render() {
        if (this._isLoading) {
            return html`<div class="loading">Loading run details...</div>`;
        }

        if (!this._run) {
            return html`<div class="empty">Run not found</div>`;
        }

        return html`
            <div class="container">
                <uai-info-grid>
                    <uai-info-card label="Run ID">${this._run.id}</uai-info-card>
                    <uai-info-card label="Test ID">${this._run.testId}</uai-info-card>
                    <uai-info-card label="Run Number">${this._run.runNumber}</uai-info-card>
                    <uai-info-card label="Status">${this._renderStatus(this._run.status)}</uai-info-card>
                    <uai-info-card label="Duration">${this._run.durationMs}ms</uai-info-card>
                    <uai-info-card label="Executed At">${new Date(this._run.executedAt).toLocaleString()}</uai-info-card>
                    ${this._run.profileId
                        ? html`<uai-info-card label="Profile ID">${this._run.profileId}</uai-info-card>`
                        : null}
                    ${this._run.transcriptId
                        ? html`<uai-info-card label="Transcript ID">${this._run.transcriptId}</uai-info-card>`
                        : null}
                </uai-info-grid>

                ${this._renderScores()}

                <uui-box headline="Outcome">
                    ${this._renderOutcome()}
                </uui-box>

                <uui-box headline="Grader Results">
                    <uai-grader-result-list .results=${this._run.graderResults}></uai-grader-result-list>
                </uui-box>
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
        .empty {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
        }

        uui-box {
            margin-top: 20px;
        }

        .section-empty {
            color: var(--uui-color-text-alt);
            text-align: center;
            padding: 20px;
        }

        .outcome-container {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }

        .scores-container {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .scores-grid {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 15px;
        }

        .score-card {
            text-align: center;
        }

        .score-label {
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-bottom: 4px;
            font-weight: 500;
        }

        .score-value {
            font-size: 24px;
            font-weight: 600;
        }

        .score-value.success {
            color: var(--uui-color-positive);
        }

        .score-value.partial {
            color: var(--uui-color-warning);
        }

        .score-value.failure {
            color: var(--uui-color-danger);
        }

        .score-bar {
            height: 6px;
            background: var(--uui-color-surface-alt);
            border-radius: 3px;
            overflow: hidden;
        }

        .score-bar-fill {
            height: 100%;
            transition: width 0.3s ease;
        }

        .score-bar-fill.success {
            background: var(--uui-color-positive);
        }

        .score-bar-fill.partial {
            background: var(--uui-color-warning);
        }

        .score-bar-fill.failure {
            background: var(--uui-color-danger);
        }

        ${codeBlockStyles}
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail": UaiTestRunDetailElement;
    }
}
