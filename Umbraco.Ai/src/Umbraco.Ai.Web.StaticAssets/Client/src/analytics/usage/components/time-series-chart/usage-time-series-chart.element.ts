import { customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type {
    AnalyticsTimeSeriesDataPoint,
    AnalyticsTimeSeriesTab
} from '../../../components';
import { html } from "@umbraco-cms/backoffice/external/lit";
import { UsageTimeSeriesPointModel } from "../../../../api";

@customElement("uai-usage-time-series-chart")
export class UaiUsageTimeSeriesChartElement extends UmbLitElement {

    @property({ type: Array })
    data?: UsageTimeSeriesPointModel[];

    @property({ type: String })
    dateRangeType?: 'last24h' | 'last7d' | 'last30d';

    @property({ type: String })
    fromDate?: string;

    @property({ type: String })
    toDate?: string;

    private _transformData(): AnalyticsTimeSeriesDataPoint[] {
        if (!this.data) return [];

        return this.data.map(d => ({
            timestamp: d.timestamp,
            inputTokens: d.inputTokens,
            outputTokens: d.outputTokens,
            requestCount: d.requestCount,
            successCount: d.successCount,
            failureCount: d.failureCount
        }));
    }

    private _getTabs(): AnalyticsTimeSeriesTab[] {
        return [
            {
                id: 'tokens',
                label: 'Tokens',
                datasets: [
                    {
                        label: 'Input Tokens',
                        dataKey: 'inputTokens',
                        backgroundColor: 'rgb(27, 38, 79)',
                        borderColor: 'rgb(27, 38, 79)'
                    },
                    {
                        label: 'Output Tokens',
                        dataKey: 'outputTokens',
                        backgroundColor: 'rgb(245, 193, 188)',
                        borderColor: 'rgb(245, 193, 188)'
                    }
                ]
            },
            {
                id: 'requests',
                label: 'Requests',
                datasets: [
                    {
                        label: 'Total Requests',
                        dataKey: 'requestCount',
                        backgroundColor: 'rgb(27, 38, 79)',
                        borderColor: 'rgb(27, 38, 79)'
                    }
                ]
            }
        ];
    }

    override render() {
        const chartData = this._transformData();
        const tabs = this._getTabs();

        return html`
            <uai-analytics-time-series-chart
                headline="Usage Over Time"
                .data=${chartData}
                .tabs=${tabs}
                .dateRangeType=${this.dateRangeType}
                .fromDate=${this.fromDate}
                .toDate=${this.toDate}
            ></uai-analytics-time-series-chart>
        `;
    }
}

export default UaiUsageTimeSeriesChartElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-usage-time-series-chart": UaiUsageTimeSeriesChartElement;
    }
}
