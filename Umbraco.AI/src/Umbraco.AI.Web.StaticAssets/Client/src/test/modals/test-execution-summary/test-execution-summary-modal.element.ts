import { html, css, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiTestExecutionSummaryModalData,
    UaiTestExecutionSummaryModalValue,
} from "./test-execution-summary-modal.token.js";
import { UaiTestExecutionRepository } from "../../repository/test-execution/test-execution.repository.js";
import type {
    UaiTestExecutionResult,
    UaiTestMetrics,
} from "../../repository/test-execution/types.js";

@customElement("uai-test-execution-summary-modal")
export class UaiTestExecutionSummaryModalElement extends UmbModalBaseElement<
    UaiTestExecutionSummaryModalData,
    UaiTestExecutionSummaryModalValue
> {
    @state()
    private _loading = true;

    @state()
    private _result?: UaiTestExecutionResult;

    @state()
    private _error?: string;

    #repository: UaiTestExecutionRepository;

    constructor() {
        super();
        this.#repository = new UaiTestExecutionRepository(this);
    }

    async firstUpdated() {
        const executionId = this.data?.executionId;
        if (!executionId) {
            this._error = "No execution ID provided.";
            this._loading = false;
            return;
        }

        const { data, error } = await this.#repository.requestExecutionResult(executionId);
        if (error || !data) {
            this._error = "Failed to load execution results.";
        } else {
            this._result = data;
        }
        this._loading = false;
    }

    #formatPercent(value: number): string {
        return `${(value * 100).toFixed(1)}%`;
    }

    #getPercentColor(value: number): string {
        if (value >= 1) return "var(--uui-color-positive)";
        if (value <= 0) return "var(--uui-color-danger)";
        return "var(--uui-color-warning)";
    }

    #renderMetricsRow(label: string, getValue: (m: UaiTestMetrics) => unknown) {
        if (!this._result) return nothing;

        const columns = [
            getValue(this._result.defaultMetrics),
            ...this._result.variationMetrics.map((v) => getValue(v.metrics)),
            getValue(this._result.aggregateMetrics),
        ];

        return html`<tr>
            <td style="font-weight: 600; padding: 8px 12px; white-space: nowrap;">${label}</td>
            ${columns.map(
                (val) => html`<td style="padding: 8px 12px; text-align: center;">${val}</td>`,
            )}
        </tr>`;
    }

    #renderPercentRow(label: string, getValue: (m: UaiTestMetrics) => number) {
        if (!this._result) return nothing;

        const allMetrics = [
            this._result.defaultMetrics,
            ...this._result.variationMetrics.map((v) => v.metrics),
            this._result.aggregateMetrics,
        ];

        return html`<tr>
            <td style="font-weight: 600; padding: 8px 12px; white-space: nowrap;">${label}</td>
            ${allMetrics.map((m) => {
                const val = getValue(m);
                return html`<td style="padding: 8px 12px; text-align: center; color: ${this.#getPercentColor(val)}; font-weight: 600;">
                    ${this.#formatPercent(val)}
                </td>`;
            })}
        </tr>`;
    }

    #renderTable() {
        if (!this._result) return nothing;

        const headers = [
            "Default",
            ...this._result.variationMetrics.map((v) => v.variationName),
            "Aggregate",
        ];

        return html`
            <div style="overflow-x: auto;">
                <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                    <thead>
                        <tr style="border-bottom: 2px solid var(--uui-color-border);">
                            <th style="padding: 8px 12px; text-align: left;">Metric</th>
                            ${headers.map(
                                (h) => html`<th style="padding: 8px 12px; text-align: center; white-space: nowrap;">${h}</th>`,
                            )}
                        </tr>
                    </thead>
                    <tbody>
                        ${this.#renderMetricsRow("Runs", (m) => m.totalRuns)}
                        ${this.#renderMetricsRow(
                            "Passed",
                            (m) => html`${m.passedRuns}<span style="opacity: 0.5;">/${m.totalRuns}</span>`,
                        )}
                        ${this.#renderPercentRow("pass@k", (m) => m.passAtK)}
                        ${this.#renderPercentRow("pass^k", (m) => m.passToTheK)}
                    </tbody>
                </table>
            </div>
        `;
    }

    render() {
        return html`
            <umb-body-layout>
                <div slot="header">
                    <h3 style="margin: 0;">Execution Summary</h3>
                </div>

                ${this._loading
                    ? html`<div style="display: flex; justify-content: center; padding: 40px;">
                        <uui-loader></uui-loader>
                    </div>`
                    : this._error
                        ? html`<div style="padding: 20px; color: var(--uui-color-danger);">${this._error}</div>`
                        : this.#renderTable()}

                <div slot="actions">
                    <uui-button
                        label="Close"
                        look="secondary"
                        @click=${() => this._rejectModal()}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            tbody tr:nth-child(odd) {
                background: var(--uui-color-surface-alt);
            }

            tbody tr:hover {
                background: var(--uui-color-surface-emphasis);
            }
        `,
    ];
}

export default UaiTestExecutionSummaryModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-execution-summary-modal": UaiTestExecutionSummaryModalElement;
    }
}
