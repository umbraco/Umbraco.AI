import { customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { AnalyticsTimeSeriesDataPoint, AnalyticsTimeSeriesTab } from "../../../components";
import { html } from "@umbraco-cms/backoffice/external/lit";
import { UsageTimeSeriesPointModel } from "../../../../api";

@customElement("uai-usage-time-series-chart")
export class UaiUsageTimeSeriesChartElement extends UmbLitElement {
    @property({ type: Array })
    data?: UsageTimeSeriesPointModel[];

    @property({ type: String })
    dateRangeType?: "last24h" | "last7d" | "last30d";

    @property({ type: String })
    fromDate?: string;

    @property({ type: String })
    toDate?: string;

    private _transformData(): AnalyticsTimeSeriesDataPoint[] {
        if (!this.data) return [];

        return this.data.map((d) => ({
            timestamp: d.timestamp,
            inputTokens: d.inputTokens,
            // The chart can only plot a number, so an unreported point becomes 0. Safe because the series
            // itself is only shown when something in range reports caching - see _hasCachedTokenData.
            cachedInputTokens: d.cachedInputTokens ?? 0,
            outputTokens: d.outputTokens,
            requestCount: d.requestCount,
            successCount: d.successCount,
            failureCount: d.failureCount,
        }));
    }

    /**
     * Whether any point in range reports cached input tokens.
     *
     * Gates the series so an install whose providers never report caching sees the chart it had before,
     * rather than a flat zero line implying caching is configured and achieving nothing.
     */
    private _hasCachedTokenData(): boolean {
        return this.data?.some((d) => d.cachedInputTokens !== undefined && d.cachedInputTokens !== null) ?? false;
    }

    private _getTabs(): AnalyticsTimeSeriesTab[] {
        return [
            {
                id: "tokens",
                label: "Tokens",
                datasets: [
                    {
                        label: "Input Tokens",
                        dataKey: "inputTokens",
                        backgroundColor: "rgb(27, 38, 79)",
                        borderColor: "rgb(27, 38, 79)",
                    },
                    ...(this._hasCachedTokenData()
                        ? [
                              {
                                  label: "Cached Input Tokens",
                                  dataKey: "cachedInputTokens",
                                  // Not a palette token: a lighter tint of the space cadet used for Input
                                  // Tokens above, chosen because cached tokens are a subset of that total
                                  // and should read as the same series' lesser part. The palette has no
                                  // lighter companion that works here — space-cadet-light is darker than
                                  // space-cadet-dimmed, dimmed reads too dark against the navy, and the
                                  // malibu/violet-blue blues read as a separate measure rather than a
                                  // subset. Literal rather than a CSS variable because Chart.js takes
                                  // colours as resolved strings, as the other series do.
                                  backgroundColor: "rgb(102, 132, 189)",
                                  borderColor: "rgb(102, 132, 189)",
                              },
                          ]
                        : []),
                    {
                        label: "Output Tokens",
                        dataKey: "outputTokens",
                        backgroundColor: "rgb(245, 193, 188)",
                        borderColor: "rgb(245, 193, 188)",
                    },
                ],
            },
            {
                id: "requests",
                label: "Requests",
                datasets: [
                    {
                        label: "Total Requests",
                        dataKey: "requestCount",
                        backgroundColor: "rgb(27, 38, 79)",
                        borderColor: "rgb(27, 38, 79)",
                    },
                ],
            },
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
