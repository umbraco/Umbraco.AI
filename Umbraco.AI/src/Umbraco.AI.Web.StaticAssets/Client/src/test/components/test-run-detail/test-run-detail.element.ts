import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UaiTestRunDetailRepository } from "../../repository/test-run-detail/test-run-detail.repository.js";
import type {
    TestRunResponseModel,
    TestGraderResultResponseModel,
    TestUsageEntryResponseModel,
} from "../../../api/types.gen.js";
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
            </div>
        `;
    }

    private _formatCount = (n: number) => n.toLocaleString();

    private _formatDuration(ms: number): string {
        if (ms < 1000) return `${ms}ms`;
        if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
        return `${(ms / 60000).toFixed(1)}m`;
    }

    /**
     * Token figures are a lower bound when some calls reported no usage, so mark them with "≥".
     * When no call reported usage at all there is no figure to show, so show "—" rather than zero.
     */
    private _formatTokens(n: number, unreportedCallCount: number, callCount: number): string {
        if (unreportedCallCount === 0) return this._formatCount(n);
        if (unreportedCallCount >= callCount) return "—";
        return `≥ ${this._formatCount(n)}`;
    }

    private _renderUsageMetric(label: string, value: string, title?: string, valueClass = "") {
        return html`
            <div class="usage-metric">
                <div class="score-label" title=${title ?? nothing}>${label}</div>
                <div class="usage-metric-value ${valueClass}">${value}</div>
            </div>
        `;
    }

    private _renderUsageEntry(entry: TestUsageEntryResponseModel) {
        const tokens = (n: number) => this._formatTokens(n, entry.unreportedCallCount, entry.callCount);
        return html`
            <uui-table-row>
                <uui-table-cell>
                    <div>${[entry.providerId, entry.modelId].filter(Boolean).join(" / ") || "—"}</div>
                    <div class="usage-secondary">
                        ${[entry.capability, entry.profileAlias ?? entry.profileId].filter(Boolean).join(" · ")}
                    </div>
                </uui-table-cell>
                <uui-table-cell>${entry.featureAlias ?? entry.featureType ?? "—"}</uui-table-cell>
                <uui-table-cell class="numeric">${tokens(entry.inputTokens)}</uui-table-cell>
                <uui-table-cell class="numeric">${tokens(entry.outputTokens)}</uui-table-cell>
                <uui-table-cell class="numeric">${tokens(entry.totalTokens)}</uui-table-cell>
                <uui-table-cell class="numeric">
                    ${this._formatCount(entry.callCount)}
                    ${entry.failedCallCount > 0
                        ? html`<span class="failed-count">(${this._formatCount(entry.failedCallCount)} failed)</span>`
                        : nothing}
                </uui-table-cell>
                <uui-table-cell class="numeric">${this._formatDuration(entry.durationMs)}</uui-table-cell>
            </uui-table-row>
        `;
    }

    private _renderUsage() {
        const usage = this._run?.outcome?.usage;
        if (!usage || usage.callCount === 0) {
            return html`<div class="section-empty">No AI usage recorded for this run</div>`;
        }

        const hasUnreported = usage.unreportedCallCount > 0;
        const tokens = (n: number) => this._formatTokens(n, usage.unreportedCallCount, usage.callCount);

        return html`
            <div class="usage-container">
                <div class="usage-grid">
                    ${this._renderUsageMetric("Input Tokens", tokens(usage.inputTokens))}
                    ${this._renderUsageMetric("Output Tokens", tokens(usage.outputTokens))}
                    ${this._renderUsageMetric("Total Tokens", tokens(usage.totalTokens))}
                    ${this._renderUsageMetric("AI Calls", this._formatCount(usage.callCount))}
                    ${this._renderUsageMetric(
                        "Failed Calls",
                        this._formatCount(usage.failedCallCount),
                        undefined,
                        usage.failedCallCount > 0 ? "failure" : "",
                    )}
                    ${this._renderUsageMetric(
                        "AI Call Time (summed)",
                        this._formatDuration(usage.durationMs),
                        "Time of each AI call added together. Calls can overlap, so this can be longer than the run duration.",
                    )}
                </div>
                ${hasUnreported
                    ? html`
                          <div class="usage-note">
                              ${this._formatCount(usage.unreportedCallCount)} of ${this._formatCount(usage.callCount)}
                              calls reported no token counts, so token figures marked "≥" are a lower bound
                              and "—" means none were reported.
                          </div>
                      `
                    : nothing}
                ${usage.breakdown.length
                    ? html`
                          <div class="usage-table">
                              <uui-table>
                                  <uui-table-head>
                                      <uui-table-head-cell>Model / Profile</uui-table-head-cell>
                                      <uui-table-head-cell>Feature</uui-table-head-cell>
                                      <uui-table-head-cell class="numeric">Input</uui-table-head-cell>
                                      <uui-table-head-cell class="numeric">Output</uui-table-head-cell>
                                      <uui-table-head-cell class="numeric">Total</uui-table-head-cell>
                                      <uui-table-head-cell class="numeric">Calls</uui-table-head-cell>
                                      <uui-table-head-cell class="numeric">Time</uui-table-head-cell>
                                  </uui-table-head>
                                  ${usage.breakdown.map((e) => this._renderUsageEntry(e))}
                              </uui-table>
                          </div>
                      `
                    : nothing}
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
                    <uai-info-card label="Run Duration">${this._run.durationMs}ms</uai-info-card>
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

                <uui-box headline="AI Usage">
                    ${this._renderUsage()}
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

        .usage-container {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }

        .usage-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 15px;
        }

        .usage-metric {
            text-align: center;
        }

        .usage-metric-value {
            font-size: 20px;
            font-weight: 600;
        }

        .usage-metric-value.failure,
        .failed-count {
            color: var(--uui-color-danger);
        }

        .failed-count {
            font-size: 12px;
            margin-left: 4px;
        }

        .usage-secondary {
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        .usage-note {
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        .usage-table {
            overflow-x: auto;
        }

        /* Size to the columns so a narrow modal scrolls the table instead of clipping its last columns. */
        .usage-table uui-table {
            box-sizing: border-box;
            width: max-content;
            min-width: 100%;
        }

        uui-table-head-cell,
        uui-table-cell {
            height: auto;
            white-space: nowrap;
        }

        uui-table-row:nth-child(even) {
            background-color: var(--uui-color-surface-emphasis);
        }

        .numeric {
            text-align: right;
            white-space: nowrap;
        }

        ${codeBlockStyles}
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail": UaiTestRunDetailElement;
    }
}
