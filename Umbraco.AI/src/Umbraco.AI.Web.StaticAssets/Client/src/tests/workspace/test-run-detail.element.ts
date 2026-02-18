import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../repository/test.repository.js";
import type { TestRunResponseModel } from "../../api/types.gen.js";

/**
 * Individual test run detail viewer.
 * Shows single run details including outcome, grader results, and transcript reference.
 */
@customElement("umbraco-ai-test-run-detail")
export class UmbracoAITestRunDetailElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    runId?: string;

    @state()
    private _run?: TestRunResponseModel;

    @state()
    private _isLoading = true;

    private _repository!: AITestRepository;

    constructor() {
        super();
        this._repository = new AITestRepository(this);
    }

    async connectedCallback() {
        super.connectedCallback();
        if (this.runId) {
            await this._loadRun();
        }
    }

    private async _loadRun() {
        this._isLoading = true;
        try {
            this._run = await this._repository.getRunById(this.runId!) || undefined;
        } catch (error) {
            console.error("Failed to load run:", error);
        } finally {
            this._isLoading = false;
        }
    }

    private _renderStatus(status: string) {
        const statusClass = status.toLowerCase();
        return html`<span class="status status-${statusClass}">${status}</span>`;
    }

    private _renderOutcome() {
        if (!this._run?.outcome) {
            return html`<div class="section-empty">No outcome recorded</div>`;
        }

        const outcome = this._run.outcome;
        return html`
            <div class="outcome-container">
                <div class="outcome-field">
                    <label>Output Type</label>
                    <div>${outcome.outputType}</div>
                </div>
                ${outcome.outputValue
                    ? html`
                        <div class="outcome-field">
                            <label>Output Value</label>
                            <pre class="code-block">${outcome.outputValue}</pre>
                        </div>
                    `
                    : null}
                ${outcome.finishReason
                    ? html`
                        <div class="outcome-field">
                            <label>Finish Reason</label>
                            <div>${outcome.finishReason}</div>
                        </div>
                    `
                    : null}
                ${outcome.tokenUsageJson
                    ? html`
                        <div class="outcome-field">
                            <label>Token Usage</label>
                            <pre class="code-block">${this._formatJson(outcome.tokenUsageJson)}</pre>
                        </div>
                    `
                    : null}
            </div>
        `;
    }

    private _renderGraderResults() {
        if (!this._run || this._run.graderResults.length === 0) {
            return html`<div class="section-empty">No grader results</div>`;
        }

        return html`
            <div class="graders-list">
                ${this._run.graderResults.map(result => html`
                    <div class="grader-result ${result.passed ? 'passed' : 'failed'}">
                        <div class="grader-header">
                            <span class="grader-status">${result.passed ? '✓' : '✗'}</span>
                            <span class="grader-score">Score: ${(result.score * 100).toFixed(1)}%</span>
                        </div>
                        ${result.actualValue
                            ? html`
                                <div class="grader-field">
                                    <label>Actual Value</label>
                                    <pre class="code-block">${result.actualValue}</pre>
                                </div>
                            `
                            : null}
                        ${result.expectedValue
                            ? html`
                                <div class="grader-field">
                                    <label>Expected Value</label>
                                    <pre class="code-block">${result.expectedValue}</pre>
                                </div>
                            `
                            : null}
                        ${result.failureMessage
                            ? html`
                                <div class="grader-field">
                                    <label>Failure Message</label>
                                    <div class="failure-message">${result.failureMessage}</div>
                                </div>
                            `
                            : null}
                        <div class="grader-meta">
                            <span>Severity: ${result.severity}</span>
                        </div>
                    </div>
                `)}
            </div>
        `;
    }

    private _formatJson(json: string): string {
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json;
        }
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
                <h2>Test Run Details</h2>

                <div class="info-grid">
                    <div class="info-item">
                        <label>Run ID</label>
                        <div>${this._run.id}</div>
                    </div>
                    <div class="info-item">
                        <label>Test ID</label>
                        <div>${this._run.testId}</div>
                    </div>
                    <div class="info-item">
                        <label>Run Number</label>
                        <div>${this._run.runNumber}</div>
                    </div>
                    <div class="info-item">
                        <label>Status</label>
                        <div>${this._renderStatus(this._run.status)}</div>
                    </div>
                    <div class="info-item">
                        <label>Duration</label>
                        <div>${this._run.durationMs}ms</div>
                    </div>
                    <div class="info-item">
                        <label>Executed At</label>
                        <div>${new Date(this._run.executedAt).toLocaleString()}</div>
                    </div>
                    ${this._run.profileId
                        ? html`
                            <div class="info-item">
                                <label>Profile ID</label>
                                <div>${this._run.profileId}</div>
                            </div>
                        `
                        : null}
                    ${this._run.transcriptId
                        ? html`
                            <div class="info-item">
                                <label>Transcript ID</label>
                                <div>${this._run.transcriptId}</div>
                            </div>
                        `
                        : null}
                </div>

                <div class="section">
                    <h3>Outcome</h3>
                    ${this._renderOutcome()}
                </div>

                <div class="section">
                    <h3>Grader Results</h3>
                    ${this._renderGraderResults()}
                </div>

                ${this._run.metadataJson
                    ? html`
                        <div class="section">
                            <h3>Metadata</h3>
                            <pre class="code-block">${this._formatJson(this._run.metadataJson)}</pre>
                        </div>
                    `
                    : null}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }

        .loading,
        .empty {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
        }

        h2 {
            margin-top: 0;
            margin-bottom: 20px;
        }

        h3 {
            margin-top: 0;
            margin-bottom: 15px;
        }

        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 15px;
            margin-bottom: 30px;
        }

        .info-item {
            background: var(--uui-color-surface);
            padding: 15px;
            border-radius: 6px;
            border: 1px solid var(--uui-color-border);
        }

        .info-item label {
            display: block;
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-bottom: 5px;
            font-weight: 500;
        }

        .info-item > div {
            font-size: 14px;
            word-break: break-all;
        }

        .status {
            display: inline-block;
            padding: 4px 12px;
            border-radius: 3px;
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
        }

        .status-passed {
            background: var(--uui-color-positive);
            color: white;
        }

        .status-failed,
        .status-error {
            background: var(--uui-color-danger);
            color: white;
        }

        .status-running {
            background: var(--uui-color-warning);
            color: white;
        }

        .section {
            background: var(--uui-color-surface);
            padding: 20px;
            border-radius: 8px;
            border: 1px solid var(--uui-color-border);
            margin-bottom: 20px;
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

        .outcome-field label,
        .grader-field label {
            display: block;
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-bottom: 5px;
            font-weight: 500;
        }

        .code-block {
            background: var(--uui-color-surface-alt);
            padding: 12px;
            border-radius: 4px;
            overflow-x: auto;
            font-family: monospace;
            font-size: 12px;
            margin: 0;
        }

        .graders-list {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }

        .grader-result {
            background: var(--uui-color-surface-alt);
            padding: 15px;
            border-radius: 6px;
            border-left: 4px solid;
        }

        .grader-result.passed {
            border-left-color: var(--uui-color-positive);
        }

        .grader-result.failed {
            border-left-color: var(--uui-color-danger);
        }

        .grader-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 10px;
            font-weight: 600;
        }

        .grader-status {
            font-size: 20px;
        }

        .grader-result.passed .grader-status {
            color: var(--uui-color-positive);
        }

        .grader-result.failed .grader-status {
            color: var(--uui-color-danger);
        }

        .grader-field {
            margin-top: 10px;
        }

        .failure-message {
            color: var(--uui-color-danger);
            padding: 10px;
            background: var(--uui-color-danger-emphasis);
            border-radius: 4px;
        }

        .grader-meta {
            margin-top: 10px;
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-run-detail": UmbracoAITestRunDetailElement;
    }
}
