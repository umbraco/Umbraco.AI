import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UsageSummaryResponseModel } from '../../../api/types.gen.js';

@customElement("uai-analytics-summary-cards")
export class UaiAnalyticsSummaryCardsElement extends UmbLitElement {

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
            <div class="summary-cards">
                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-activity"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatIntWithK(totalRequests)}</div>
                        <div class="card-label">Total Requests</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-page-down"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatIntWithK(inputTokens)}</div>
                        <div class="card-label">Input Tokens</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-page-up"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatIntWithK(outputTokens)}</div>
                        <div class="card-label">Output Tokens</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-coin"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatIntWithK(totalTokens)}</div>
                        <div class="card-label">Total Tokens</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-check"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatPercentage(successRate)}</div>
                        <div class="card-label">Success Rate</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-time"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatMs(averageDurationMs)}</div>
                        <div class="card-label">Avg Duration</div>
                    </div>
                </uui-card>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .summary-cards {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                gap: var(--uui-size-space-4);
            }

            .summary-card {
                display: flex;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-5);
            }

            .card-icon {
                display: flex;
                align-items: center;
                justify-content: center;
                width: 32px;
                height: 32px;
            }

            .card-icon uui-icon {
                font-size: 1.5rem;
                color: var(--uui-color-current);
            }

            .card-content {
                flex: 1;
                display: flex;
                flex-direction: column;
            }

            .card-value {
                font-size: var(--uui-type-h3-size);
                font-weight: 700;
                line-height: 1;
            }

            .card-label {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
                font-weight: 500;
            }

            .card-detail {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiAnalyticsSummaryCardsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-summary-cards": UaiAnalyticsSummaryCardsElement;
    }
}
