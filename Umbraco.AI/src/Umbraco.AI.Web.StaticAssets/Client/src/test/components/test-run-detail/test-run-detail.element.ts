import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestRunResponseModel } from "../../../api/types.gen.js";
import { codeBlockStyles } from "../../../core/styles/code-block.styles.js";


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
                ${outcome.tokenUsageJson
                    ? html`
                        <uai-labeled-field label="Token Usage">
                            <pre class="code-block">${this._formatJson(outcome.tokenUsageJson)}</pre>
                        </uai-labeled-field>
                    `
                    : null}
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

                <uui-box headline="Outcome">
                    ${this._renderOutcome()}
                </uui-box>

                <uui-box headline="Grader Results">
                    <uai-grader-result-list .results=${this._run.graderResults}></uai-grader-result-list>
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

        ${codeBlockStyles}
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail": UaiTestRunDetailElement;
    }
}
