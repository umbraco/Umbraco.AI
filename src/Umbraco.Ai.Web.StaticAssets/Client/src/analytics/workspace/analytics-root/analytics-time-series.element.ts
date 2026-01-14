import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UsageTimeSeriesPointModel } from '../../../api/types.gen.js';

@customElement("uai-analytics-time-series")
export class UaiAnalyticsTimeSeriesElement extends UmbLitElement {

    @property({ type: Array })
    data?: UsageTimeSeriesPointModel[];

    override render() {
        if (!this.data || this.data.length === 0) return null;

        return html`
            <uui-box headline="Usage Over Time" class="time-series-box">
                <div class="time-series-placeholder">
                    <p>Time series chart will be displayed here</p>
                    <p class="detail">${this.data.length} data points available</p>
                </div>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .time-series-box {
                min-height: 300px;
            }

            .time-series-placeholder {
                padding: var(--uui-size-space-6);
                text-align: center;
                color: var(--uui-color-text-alt);
            }

            .time-series-placeholder .detail {
                font-size: var(--uui-type-small-size);
                margin-top: var(--uui-size-space-2);
            }
        `,
    ];
}

export default UaiAnalyticsTimeSeriesElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-time-series": UaiAnalyticsTimeSeriesElement;
    }
}
