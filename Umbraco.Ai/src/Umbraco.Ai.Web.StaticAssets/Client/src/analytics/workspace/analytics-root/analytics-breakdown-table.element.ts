import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UsageBreakdownItemModel } from '../../../api/types.gen.js';

@customElement("uai-analytics-breakdown-table")
export class UaiAnalyticsBreakdownTableElement extends UmbLitElement {

    @property({ type: String })
    headline = '';

    @property({ type: Array })
    data?: UsageBreakdownItemModel[];
    
    @property({ type: Function })
    nameTemplate: (name: string) => unknown = (name: string) => name;

    private _formatNumber(num: number): string {
        return new Intl.NumberFormat().format(num);
    }

    private _formatPercentage(num: number): string {
        return `${(num * 100).toFixed(1)}%`;
    }

    override render() {
        if (!this.data || this.data.length === 0) {
            return html`
                <uui-box headline=${this.headline} class="breakdown-box">
                    <p class="no-data">No data available</p>
                </uui-box>
            `;
        }

        return html`
            <uui-box headline=${this.headline} class="breakdown-box">
                <uui-table>
                    <uui-table-head>
                        <uui-table-head-cell>Name</uui-table-head-cell>
                        <uui-table-head-cell style="text-align: right;">Requests</uui-table-head-cell>
                        <uui-table-head-cell style="text-align: right;">Tokens</uui-table-head-cell>
                        <uui-table-head-cell style="text-align: right;">Share</uui-table-head-cell>
                    </uui-table-head>
                    ${this.data.map(item => html`
                        <uui-table-row>
                            <uui-table-cell>${item.dimensionName ?? this.nameTemplate(item.dimension)}</uui-table-cell>
                            <uui-table-cell style="text-align: right;">${this._formatNumber(item.requestCount)}</uui-table-cell>
                            <uui-table-cell style="text-align: right;">${this._formatNumber(item.totalTokens)}</uui-table-cell>
                            <uui-table-cell style="text-align: right;">${this._formatPercentage(item.percentage / 100)}</uui-table-cell>
                        </uui-table-row>
                    `)}
                </uui-table>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .breakdown-box {
                display: flex;
                flex-direction: column;
            }

            .no-data {
                padding: var(--uui-size-space-4);
                text-align: center;
                color: var(--uui-color-text-alt);
            }

            uui-table-head-cell,
            uui-table-cell {
                padding: var(--uui-size-space-3) var(--uui-size-space-2);
                height: auto;
            }

            uui-table-head-cell {
                padding-top: 0;
            }
            
            uui-box {
                height: 100%;
            }

            uui-table-row:nth-child(even) {
                background-color: var(--uui-color-surface-emphasis);
            }
        `,
    ];
}

export default UaiAnalyticsBreakdownTableElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-breakdown-table": UaiAnalyticsBreakdownTableElement;
    }
}
