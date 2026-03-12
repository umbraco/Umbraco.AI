import { html, css, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
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

    override async firstUpdated() {
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

    #findBestLabels(): Set<string> {
        if (!this._result) return new Set();

        const candidates = [
            { label: "Default", metrics: this._result.defaultMetrics },
            ...this._result.variationMetrics.map((v) => ({ label: v.variationName, metrics: v.metrics })),
        ];

        if (candidates.length < 2) return new Set();

        let bestPassAtK = -1;
        let bestPassToTheK = -1;
        for (const c of candidates) {
            if (c.metrics.passAtK > bestPassAtK || (c.metrics.passAtK === bestPassAtK && c.metrics.passToTheK > bestPassToTheK)) {
                bestPassAtK = c.metrics.passAtK;
                bestPassToTheK = c.metrics.passToTheK;
            }
        }

        return new Set(
            candidates
                .filter((c) => c.metrics.passAtK === bestPassAtK && c.metrics.passToTheK === bestPassToTheK)
                .map((c) => c.label),
        );
    }

    #renderVariationRow(label: string, metrics: UaiTestMetrics, index: number, isBest: boolean, isAggregate: boolean) {
        const bg = index % 2 === 1 ? "background: var(--uui-color-surface-alt);" : "";
        const passAtK = metrics.passAtK;
        const passToTheK = metrics.passToTheK;
        const cellStyle = isAggregate
            ? "padding: 8px 12px; border-top: 2px solid var(--uui-color-border);"
            : "padding: 8px 12px;";

        return html`<tr style="${bg}">
            <td style="${cellStyle} font-weight: 600; white-space: nowrap;">${label}</td>
            <td style="${cellStyle} text-align: center;">${metrics.totalRuns}</td>
            <td style="${cellStyle} text-align: center;">${metrics.passedRuns}<span style="opacity: 0.5;">/${metrics.totalRuns}</span></td>
            <td style="${cellStyle} text-align: center; color: ${this.#getPercentColor(passAtK)}; font-weight: 600;">${this.#formatPercent(passAtK)}</td>
            <td style="${cellStyle} text-align: center; color: ${this.#getPercentColor(passToTheK)}; font-weight: 600;">${this.#formatPercent(passToTheK)}</td>
            <td style="${cellStyle} text-align: center; width: 24px;">${isBest ? html`<uui-icon name="icon-trophy" style="color: var(--uui-color-positive);"></uui-icon>` : nothing}</td>
        </tr>`;
    }

    #renderTable() {
        if (!this._result) return nothing;

        const bestLabels = this.#findBestLabels();

        const rows: Array<{ label: string; metrics: UaiTestMetrics; isAggregate: boolean }> = [
            { label: "Default", metrics: this._result.defaultMetrics, isAggregate: false },
            ...this._result.variationMetrics.map((v) => ({ label: v.variationName, metrics: v.metrics, isAggregate: false })),
            { label: "Aggregate", metrics: this._result.aggregateMetrics, isAggregate: true },
        ];

        return html`
            <uui-box headline="Metrics">
                <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                    <thead>
                        <tr style="border-bottom: 2px solid var(--uui-color-border);">
                            <th style="padding: 8px 12px; text-align: left;">Variation</th>
                            <th style="padding: 8px 12px; text-align: center;">Runs</th>
                            <th style="padding: 8px 12px; text-align: center;">Passed</th>
                            <th style="padding: 8px 12px; text-align: center;">pass@k</th>
                            <th style="padding: 8px 12px; text-align: center;">pass^k</th>
                            <th style="width: 24px;"></th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rows.map((r, i) => this.#renderVariationRow(r.label, r.metrics, i, bestLabels.has(r.label), r.isAggregate))}
                    </tbody>
                </table>
            </uui-box>
        `;
    }

    override render() {
        return html`
            <umb-body-layout headline="Execution Summary">
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
                        @click=${this._rejectModal}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = css``;
}

export default UaiTestExecutionSummaryModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-execution-summary-modal": UaiTestExecutionSummaryModalElement;
    }
}
