import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestRunResponseModel, TestGraderResultResponseModel } from "../../../api/types.gen.js";

/**
 * Individual test run detail viewer.
 * Shows single run details including outcome, grader results, and transcript reference.
 */
@customElement("uai-test-run-detail")
export class UaiTestRunDetailElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    runId?: string;

    @state()
    private _run?: TestRunResponseModel;

    @state()
    private _isLoading = true;

    @state()
    private _expandedGraders = new Set<number>();

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
            if (this._run) {
                this._initExpandedGraders();
            }
        } catch (error) {
            console.error("Failed to load run:", error);
        } finally {
            this._isLoading = false;
        }
    }

    private _initExpandedGraders() {
        const expanded = new Set<number>();
        this._run?.graderResults.forEach((result, index) => {
            if (!result.passed) {
                expanded.add(index);
            }
        });
        this._expandedGraders = expanded;
    }

    private _toggleGrader(index: number) {
        const newSet = new Set(this._expandedGraders);
        if (newSet.has(index)) {
            newSet.delete(index);
        } else {
            newSet.add(index);
        }
        this._expandedGraders = newSet;
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
                ${this._run.graderResults.map((result, index) => this._renderGraderCard(result, index))}
            </div>
        `;
    }

    private _renderGraderCard(result: TestGraderResultResponseModel, index: number) {
        const isExpanded = this._expandedGraders.has(index);
        const graderName = result.graderName || "Grader";

        return html`
            <div class="grader-result ${result.passed ? 'passed' : 'failed'}">
                <button
                    class="grader-card-header"
                    @click=${() => this._toggleGrader(index)}
                >
                    <span class="grader-status">${result.passed ? '\u2713' : '\u2717'}</span>
                    <span class="grader-name">${graderName}</span>
                    <span class="grader-header-meta">
                        ${result.graderType
                            ? html`<span class="grader-type-badge">${result.graderType}</span>`
                            : nothing}
                        ${result.weight !== 1.0
                            ? html`<span class="grader-weight">Weight: ${result.weight}</span>`
                            : nothing}
                        ${result.negate
                            ? html`<span class="grader-negate-badge">NEGATED</span>`
                            : nothing}
                    </span>
                    <span class="grader-score">Score: ${(result.score * 100).toFixed(1)}%</span>
                    <span class="chevron">${isExpanded ? '\u25BC' : '\u25B6'}</span>
                </button>
                ${isExpanded ? html`
                    <div class="grader-card-body">
                        ${result.failureMessage
                            ? html`
                                <div class="grader-field">
                                    <label>Failure Message</label>
                                    <div class="failure-message">${result.failureMessage}</div>
                                </div>
                            `
                            : nothing}
                        ${result.expectedValue
                            ? html`
                                <div class="grader-field">
                                    <label>Expected Value</label>
                                    <pre class="code-block">${this._formatJson(result.expectedValue)}</pre>
                                </div>
                            `
                            : nothing}
                        ${result.actualValue
                            ? html`
                                <div class="grader-field">
                                    <label>Actual Value</label>
                                    <pre class="code-block">${this._formatJson(result.actualValue)}</pre>
                                </div>
                            `
                            : nothing}
                        ${result.metadataJson
                            ? html`
                                <div class="grader-field">
                                    <label>Metadata</label>
                                    ${this._renderMetadata(result.metadataJson)}
                                </div>
                            `
                            : nothing}
                        <div class="grader-meta">
                            <span>Severity: ${result.severity}</span>
                            ${result.graderTypeId
                                ? html`<span>Type ID: ${result.graderTypeId}</span>`
                                : nothing}
                        </div>
                    </div>
                ` : nothing}
            </div>
        `;
    }

    private _renderMetadata(metadataJson: string) {
        try {
            const parsed = JSON.parse(metadataJson);
            if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
                return html`
                    <table class="metadata-table">
                        <thead>
                            <tr>
                                <th>Key</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${Object.entries(parsed).map(([key, value]) => html`
                                <tr>
                                    <td>${key}</td>
                                    <td>${this._renderMetadataValue(value)}</td>
                                </tr>
                            `)}
                        </tbody>
                    </table>
                `;
            }
            return html`<pre class="code-block">${this._formatJson(metadataJson)}</pre>`;
        } catch {
            return html`<pre class="code-block">${metadataJson}</pre>`;
        }
    }

    private _renderMetadataValue(value: unknown) {
        if (typeof value === 'string') {
            if (value.length > 100) {
                return html`<pre class="code-block">${value}</pre>`;
            }
            return html`${value}`;
        }
        if (typeof value === 'number' || typeof value === 'boolean') {
            return html`<code>${String(value)}</code>`;
        }
        return html`<pre class="code-block">${JSON.stringify(value, null, 2)}</pre>`;
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

                <uui-box headline="Outcome">
                    ${this._renderOutcome()}
                </uui-box>

                <uui-box headline="Grader Results">
                    ${this._renderGraderResults()}
                </uui-box>

                ${this._run.metadataJson
                    ? html`
                        <uui-box headline="Metadata">
                            <pre class="code-block">${this._formatJson(this._run.metadataJson)}</pre>
                        </uui-box>
                    `
                    : null}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        .loading,
        .empty {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
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
            white-space: pre-wrap;
            word-break: break-word;
        }

        .graders-list {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .grader-result {
            border-radius: 6px;
            border: 1px solid var(--uui-color-border);
            border-left: 4px solid;
            overflow: hidden;
        }

        .grader-result.passed {
            border-left-color: var(--uui-color-positive);
        }

        .grader-result.failed {
            border-left-color: var(--uui-color-danger);
        }

        /* Grader card header */
        .grader-card-header {
            display: flex;
            align-items: center;
            gap: 10px;
            width: 100%;
            padding: 12px 15px;
            background: var(--uui-color-surface-alt);
            border: none;
            cursor: pointer;
            font-size: 13px;
            text-align: left;
            color: var(--uui-color-text);
        }

        .grader-card-header:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .grader-status {
            font-size: 18px;
            flex-shrink: 0;
        }

        .grader-result.passed .grader-status {
            color: var(--uui-color-positive);
        }

        .grader-result.failed .grader-status {
            color: var(--uui-color-danger);
        }

        .grader-name {
            font-weight: 600;
            flex-shrink: 0;
        }

        .grader-header-meta {
            display: flex;
            align-items: center;
            gap: 6px;
            flex: 1;
            min-width: 0;
        }

        .grader-type-badge {
            display: inline-block;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 11px;
            background: var(--uui-color-surface);
            color: var(--uui-color-text-alt);
            border: 1px solid var(--uui-color-border);
        }

        .grader-negate-badge {
            display: inline-block;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 11px;
            font-weight: 600;
            background: var(--uui-color-warning);
            color: white;
        }

        .grader-weight {
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        .grader-score {
            font-weight: 600;
            font-size: 13px;
            flex-shrink: 0;
            margin-left: auto;
        }

        .chevron {
            font-size: 10px;
            color: var(--uui-color-text-alt);
            flex-shrink: 0;
        }

        /* Grader card body */
        .grader-card-body {
            padding: 15px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .grader-field {
            margin: 0;
        }

        .failure-message {
            color: var(--uui-color-danger);
            padding: 10px;
            background: var(--uui-color-danger-emphasis);
            border-radius: 4px;
        }

        .grader-meta {
            display: flex;
            gap: 15px;
            font-size: 12px;
            color: var(--uui-color-text-alt);
        }

        /* Metadata table */
        .metadata-table {
            width: 100%;
            border-collapse: collapse;
        }

        .metadata-table th,
        .metadata-table td {
            text-align: left;
            padding: 8px 12px;
            border-bottom: 1px solid var(--uui-color-border);
            font-size: 13px;
        }

        .metadata-table th {
            font-weight: 600;
            color: var(--uui-color-text-alt);
            font-size: 12px;
        }

        .metadata-table td:first-child {
            font-weight: 500;
            white-space: nowrap;
            width: 1%;
        }

        .metadata-table code {
            font-family: monospace;
            font-size: 12px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail": UaiTestRunDetailElement;
    }
}
