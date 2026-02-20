import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestTranscriptResponseModel } from "../../../api/types.gen.js";

interface TranscriptMessage {
    role: string;
    content: string;
}

interface TranscriptToolCall {
    name: string;
    arguments?: string;
    result?: string;
}

interface TranscriptTiming {
    [key: string]: number | string;
}

/**
 * Renders the execution transcript for a test run.
 * Shows messages, tool calls, reasoning, timing, and final output.
 */
@customElement("uai-test-run-transcript")
export class UaiTestRunTranscriptElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    runId?: string;

    @state()
    private _transcript?: TestTranscriptResponseModel;

    @state()
    private _isLoading = true;

    @state()
    private _expandedToolCalls = new Set<number>();

    @state()
    private _reasoningExpanded = false;

    private _repository!: AITestRepository;

    constructor() {
        super();
        this._repository = new AITestRepository(this);
    }

    async connectedCallback() {
        super.connectedCallback();
        if (this.runId) {
            await this._loadTranscript();
        } else {
            this._isLoading = false;
        }
    }

    private async _loadTranscript() {
        this._isLoading = true;
        try {
            this._transcript = await this._repository.getTestRunTranscript(this.runId!) ?? undefined;
        } catch (error) {
            console.error("Failed to load transcript:", error);
        } finally {
            this._isLoading = false;
        }
    }

    private _parseJson<T>(json: string | null | undefined): T | null {
        if (!json) return null;
        try {
            return JSON.parse(json) as T;
        } catch {
            return null;
        }
    }

    private _formatJson(json: string): string {
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json;
        }
    }

    private _toggleToolCall(index: number) {
        const newSet = new Set(this._expandedToolCalls);
        if (newSet.has(index)) {
            newSet.delete(index);
        } else {
            newSet.add(index);
        }
        this._expandedToolCalls = newSet;
    }

    private _renderMessages() {
        const messages = this._parseJson<TranscriptMessage[]>(this._transcript?.messagesJson);
        if (!messages || messages.length === 0) {
            return html`<div class="section-empty">No messages recorded</div>`;
        }

        return html`
            <div class="messages-list">
                ${messages.map(msg => html`
                    <div class="message message-${msg.role}">
                        <div class="message-role">${msg.role}</div>
                        <div class="message-content">${msg.content}</div>
                    </div>
                `)}
            </div>
        `;
    }

    private _renderToolCalls() {
        const toolCalls = this._parseJson<TranscriptToolCall[]>(this._transcript?.toolCallsJson);
        if (!toolCalls || toolCalls.length === 0) {
            return html`<div class="section-empty">No tool calls recorded</div>`;
        }

        return html`
            <div class="tool-calls-list">
                ${toolCalls.map((call, index) => html`
                    <div class="tool-call">
                        <button
                            class="tool-call-header"
                            @click=${() => this._toggleToolCall(index)}
                        >
                            <span class="tool-call-expand">${this._expandedToolCalls.has(index) ? '\u25BC' : '\u25B6'}</span>
                            <span class="tool-call-name">${call.name}</span>
                        </button>
                        ${this._expandedToolCalls.has(index) ? html`
                            <div class="tool-call-body">
                                ${call.arguments ? html`
                                    <div class="tool-call-section">
                                        <label>Arguments</label>
                                        <pre class="code-block">${this._formatJson(call.arguments)}</pre>
                                    </div>
                                ` : nothing}
                                ${call.result ? html`
                                    <div class="tool-call-section">
                                        <label>Result</label>
                                        <pre class="code-block">${this._formatJson(call.result)}</pre>
                                    </div>
                                ` : nothing}
                            </div>
                        ` : nothing}
                    </div>
                `)}
            </div>
        `;
    }

    private _renderReasoning() {
        const reasoning = this._parseJson<string[]>(this._transcript?.reasoningJson);
        if (!reasoning || reasoning.length === 0) {
            return html`<div class="section-empty">No reasoning recorded</div>`;
        }

        return html`
            <div class="reasoning-container">
                <button
                    class="reasoning-toggle"
                    @click=${() => { this._reasoningExpanded = !this._reasoningExpanded; }}
                >
                    <span>${this._reasoningExpanded ? '\u25BC' : '\u25B6'}</span>
                    <span>${reasoning.length} reasoning step${reasoning.length !== 1 ? 's' : ''}</span>
                </button>
                ${this._reasoningExpanded ? html`
                    <div class="reasoning-steps">
                        ${reasoning.map((step, i) => html`
                            <div class="reasoning-step">
                                <span class="step-number">${i + 1}.</span>
                                <span>${step}</span>
                            </div>
                        `)}
                    </div>
                ` : nothing}
            </div>
        `;
    }

    private _renderTiming() {
        const timing = this._parseJson<TranscriptTiming>(this._transcript?.timingJson);
        if (!timing || Object.keys(timing).length === 0) {
            return html`<div class="section-empty">No timing data recorded</div>`;
        }

        return html`
            <table class="timing-table">
                <thead>
                    <tr>
                        <th>Metric</th>
                        <th>Value</th>
                    </tr>
                </thead>
                <tbody>
                    ${Object.entries(timing).map(([key, value]) => html`
                        <tr>
                            <td>${key}</td>
                            <td>${typeof value === 'number' ? `${value}ms` : value}</td>
                        </tr>
                    `)}
                </tbody>
            </table>
        `;
    }

    private _renderFinalOutput() {
        if (!this._transcript?.finalOutputJson) {
            return html`<div class="section-empty">No final output recorded</div>`;
        }

        return html`
            <pre class="code-block">${this._formatJson(this._transcript.finalOutputJson)}</pre>
        `;
    }

    render() {
        if (this._isLoading) {
            return html`<div class="loading"><uui-loader></uui-loader></div>`;
        }

        if (!this._transcript) {
            return html`<div class="empty">No transcript available for this run</div>`;
        }

        return html`
            <div class="container">
                <uui-box headline="Messages">
                    ${this._renderMessages()}
                </uui-box>

                <uui-box headline="Tool Calls">
                    ${this._renderToolCalls()}
                </uui-box>

                <uui-box headline="Reasoning">
                    ${this._renderReasoning()}
                </uui-box>

                <uui-box headline="Timing">
                    ${this._renderTiming()}
                </uui-box>

                <uui-box headline="Final Output">
                    ${this._renderFinalOutput()}
                </uui-box>
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

        .loading {
            display: flex;
            justify-content: center;
        }

        uui-box {
            margin-top: 20px;
        }

        uui-box:first-child {
            margin-top: 0;
        }

        .section-empty {
            color: var(--uui-color-text-alt);
            text-align: center;
            padding: 20px;
        }

        /* Messages */
        .messages-list {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .message {
            padding: 12px;
            border-radius: 8px;
            border: 1px solid var(--uui-color-border);
        }

        .message-system {
            background: var(--uui-color-surface-alt);
            border-left: 3px solid var(--uui-color-warning);
        }

        .message-user {
            background: var(--uui-color-surface);
            border-left: 3px solid var(--uui-color-interactive);
        }

        .message-assistant {
            background: var(--uui-color-surface);
            border-left: 3px solid var(--uui-color-positive);
        }

        .message-role {
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            color: var(--uui-color-text-alt);
            margin-bottom: 6px;
        }

        .message-content {
            font-size: 13px;
            line-height: 1.5;
            white-space: pre-wrap;
            word-break: break-word;
        }

        /* Tool Calls */
        .tool-calls-list {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .tool-call {
            border: 1px solid var(--uui-color-border);
            border-radius: 6px;
            overflow: hidden;
        }

        .tool-call-header {
            display: flex;
            align-items: center;
            gap: 8px;
            width: 100%;
            padding: 10px 12px;
            background: var(--uui-color-surface-alt);
            border: none;
            cursor: pointer;
            font-size: 13px;
            text-align: left;
            color: var(--uui-color-text);
        }

        .tool-call-header:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .tool-call-expand {
            font-size: 10px;
            color: var(--uui-color-text-alt);
        }

        .tool-call-name {
            font-weight: 600;
            font-family: monospace;
        }

        .tool-call-body {
            padding: 12px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .tool-call-section label {
            display: block;
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-bottom: 4px;
            font-weight: 500;
        }

        /* Reasoning */
        .reasoning-container {
            display: flex;
            flex-direction: column;
        }

        .reasoning-toggle {
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 10px 0;
            background: none;
            border: none;
            cursor: pointer;
            font-size: 13px;
            color: var(--uui-color-text);
        }

        .reasoning-toggle:hover {
            color: var(--uui-color-interactive);
        }

        .reasoning-steps {
            display: flex;
            flex-direction: column;
            gap: 8px;
            padding-left: 10px;
            border-left: 2px solid var(--uui-color-border);
        }

        .reasoning-step {
            font-size: 13px;
            line-height: 1.5;
        }

        .step-number {
            color: var(--uui-color-text-alt);
            font-weight: 600;
            margin-right: 4px;
        }

        /* Timing */
        .timing-table {
            width: 100%;
            border-collapse: collapse;
        }

        .timing-table th,
        .timing-table td {
            text-align: left;
            padding: 8px 12px;
            border-bottom: 1px solid var(--uui-color-border);
            font-size: 13px;
        }

        .timing-table th {
            font-weight: 600;
            color: var(--uui-color-text-alt);
            font-size: 12px;
        }

        .timing-table td:last-child {
            font-family: monospace;
        }

        /* Code block */
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
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-transcript": UaiTestRunTranscriptElement;
    }
}
