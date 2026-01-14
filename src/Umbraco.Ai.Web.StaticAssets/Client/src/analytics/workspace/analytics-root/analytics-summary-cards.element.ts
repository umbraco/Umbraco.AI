import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UsageSummaryResponseModel } from '../../../api/types.gen.js';

@customElement("uai-analytics-summary-cards")
export class UaiAnalyticsSummaryCardsElement extends UmbLitElement {

    @property({ type: Object })
    summary?: UsageSummaryResponseModel;

    private _formatNumber(num: number): string {
        return new Intl.NumberFormat().format(num);
    }

    private _formatPercentage(num: number): string {
        return `${(num * 100).toFixed(1)}%`;
    }

    override render() {
        if (!this.summary) return null;

        const { totalRequests, totalTokens, inputTokens, outputTokens, successRate, averageDurationMs } = this.summary;

        return html`
            <div class="summary-cards">
                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-activity"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatNumber(totalRequests)}</div>
                        <div class="card-label">Total Requests</div>
                    </div>
                </uui-card>

                <uui-card class="summary-card">
                    <div class="card-icon"><uui-icon name="icon-coin"></uui-icon></div>
                    <div class="card-content">
                        <div class="card-value">${this._formatNumber(totalTokens)}</div>
                        <div class="card-label">Total Tokens</div>
                        <div class="card-detail">
                            ${this._formatNumber(inputTokens)} in · ${this._formatNumber(outputTokens)} out
                        </div>
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
                        <div class="card-value">${averageDurationMs}ms</div>
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
                gap: var(--uui-size-space-4);
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
                gap: var(--uui-size-space-1);
            }

            .card-value {
                font-size: var(--uui-type-h3-size);
                font-weight: 700;
                line-height: 1.2;
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
