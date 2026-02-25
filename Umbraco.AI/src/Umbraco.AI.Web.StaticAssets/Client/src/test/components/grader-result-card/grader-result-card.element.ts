import { LitElement, html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import type { TestGraderResultResponseModel } from "../../../api/types.gen.js";
import { codeBlockStyles } from "../../../core/styles/code-block.styles.js";

/**
 * Self-contained card for displaying a single grader result.
 * Manages its own expand/collapse toggle.
 */
@customElement("uai-grader-result-card")
export class UaiGraderResultCardElement extends LitElement {
    @property({ attribute: false })
    result!: TestGraderResultResponseModel;

    @property({ type: Boolean })
    expanded = false;

    @state()
    private _expanded = false;

    protected willUpdate(changedProperties: Map<PropertyKey, unknown>) {
        if (changedProperties.has("expanded") && changedProperties.get("expanded") === undefined) {
            this._expanded = this.expanded;
        }
    }

    private _toggle() {
        this._expanded = !this._expanded;
    }

    private _formatJson(json: string): string {
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json;
        }
    }

    private _renderMetadataValue(value: unknown) {
        if (typeof value === "string") {
            if (value.length > 100) {
                return html`<pre class="code-block">${value}</pre>`;
            }
            return html`${value}`;
        }
        if (typeof value === "number" || typeof value === "boolean") {
            return html`<code>${String(value)}</code>`;
        }
        return html`<pre class="code-block">${JSON.stringify(value, null, 2)}</pre>`;
    }

    private _renderMetadata(metadataJson: string) {
        try {
            const parsed = JSON.parse(metadataJson);
            if (typeof parsed === "object" && parsed !== null && !Array.isArray(parsed)) {
                return html`
                    <table class="metadata-table">
                        <thead>
                            <tr>
                                <th>Key</th>
                                <th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${Object.entries(parsed).map(
                                ([key, value]) => html`
                                    <tr>
                                        <td>${key}</td>
                                        <td>${this._renderMetadataValue(value)}</td>
                                    </tr>
                                `,
                            )}
                        </tbody>
                    </table>
                `;
            }
            return html`<pre class="code-block">${this._formatJson(metadataJson)}</pre>`;
        } catch {
            return html`<pre class="code-block">${metadataJson}</pre>`;
        }
    }

    render() {
        const result = this.result;
        const graderName = result.graderName || "Grader";

        return html`
            <div class="grader-result ${result.passed ? "passed" : "failed"}">
                <button class="grader-card-header" @click=${this._toggle}>
                    <span class="grader-status">${result.passed
                        ? html`<uui-icon name="icon-check" color="var(--uui-color-positive)"></uui-icon>`
                        : html`<uui-icon name="icon-delete" color="var(--uui-color-danger)"></uui-icon>`
                    }</span>
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
                    <span class="chevron">${this._expanded ? "\u25BC" : "\u25B6"}</span>
                </button>
                ${this._expanded
                    ? html`
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
                      `
                    : nothing}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }

        .grader-result {
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

        .grader-card-body {
            padding: 15px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .grader-field {
            margin: 0;
        }

        .grader-field label {
            display: block;
            font-size: 12px;
            color: var(--uui-color-text-alt);
            margin-bottom: 5px;
            font-weight: 500;
        }

        .failure-message {
            color: white;
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

        ${codeBlockStyles}

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
        "uai-grader-result-card": UaiGraderResultCardElement;
    }
}
