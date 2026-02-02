import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UsageSummaryResponseModel } from "../../../../api";

@customElement("uai-usage-summary-cards")
export class UaiUsageSummaryCardsElement extends UmbLitElement {

    @property({ type: Object })
    summary?: UsageSummaryResponseModel;

    private _formatIntWithK(value: number, decimals: number = 1): string {

        const abs = Math.abs(value);

        if (abs < 1000) {
            return value.toString();
        }

        const formatted = (abs / 1000).toFixed(decimals);
        const clean = formatted.replace(/\.0+$/, "");

        return `${value < 0 ? "-" : ""}${clean}k`;
    }

    private _formatPercentage(num: number): string {
        return `${(num * 100).toFixed(1)}%`;
    }

    private _formatMs(ms: number, decimals: number = 1): string {
        
        const units = [
            { label: "d", value: 24 * 60 * 60 * 1000 },
            { label: "h", value: 60 * 60 * 1000 },
            { label: "m", value: 60 * 1000 },
            { label: "s", value: 1000 },
            { label: "ms", value: 1 },
        ];

        for (const unit of units) {
            if (ms >= unit.value) {
                const result = ms / unit.value;
                const rounded =
                    unit.label === "ms"
                        ? Math.round(result)
                        : Number(result.toFixed(decimals));
                return `${rounded}${unit.label}`;
            }
        }

        return "0ms";
    }

    override render() {
        if (!this.summary) return null;

        const { totalRequests, totalTokens, inputTokens, outputTokens, successRate, averageDurationMs } = this.summary;

        return html`
            <uai-analytics-summary-card
                icon="icon-activity"
                label="Total Requests"
                .value=${this._formatIntWithK(totalRequests)}
            ></uai-analytics-summary-card>

            <uai-analytics-summary-card
                    icon="icon-page-down"
                    label="Input Tokens"
                    .value=${this._formatIntWithK(inputTokens)}
            ></uai-analytics-summary-card>

            <uai-analytics-summary-card
                    icon="icon-page-down"
                    label="Output Tokens"
                    .value=${this._formatIntWithK(outputTokens)}
            ></uai-analytics-summary-card>

            <uai-analytics-summary-card
                    icon="icon-coin"
                    label="Total Tokens"
                    .value=${this._formatIntWithK(totalTokens)}
            ></uai-analytics-summary-card>

            <uai-analytics-summary-card
                    icon="icon-check"
                    label="Success Rate"
                    .value=${this._formatPercentage(successRate)}
            ></uai-analytics-summary-card>

            <uai-analytics-summary-card
                    icon="icon-time"
                    label="Avg Duration"
                    .value=${this._formatMs(averageDurationMs)}
            ></uai-analytics-summary-card>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                gap: var(--uui-size-space-5);
            }
        `,
    ];
}

export default UaiUsageSummaryCardsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-usage-summary-cards": UaiUsageSummaryCardsElement;
    }
}
