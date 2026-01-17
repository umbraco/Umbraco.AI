import { css, html, customElement, property, query, state, type PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import type { UsageTimeSeriesPointModel } from '../../../api/types.gen.js';

@customElement("uai-analytics-time-series")
export class UaiAnalyticsTimeSeriesElement extends UmbLitElement {

    @property({ type: Array })
    data?: UsageTimeSeriesPointModel[];

    @property({ type: String })
    metric: 'tokens' | 'requests' = 'tokens';

    @property({ type: String })
    dateRangeType?: 'last24h' | 'last7d' | 'last30d';

    @property({ type: String })
    fromDate?: string;

    @property({ type: String })
    toDate?: string;

    @state()
    private _chart?: Chart;

    @query('#chart-canvas')
    private _canvasElement?: HTMLCanvasElement;

    constructor() {
        super();
        // Register Chart.js components
        Chart.register(...registerables);
    }

    override firstUpdated() {
        this._initializeChart();
    }

    override updated(changedProperties: PropertyValues) {
        super.updated(changedProperties);

        if (changedProperties.has('data') || changedProperties.has('metric')) {
            this._updateChart();
        }
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this._chart?.destroy();
    }

    private _initializeChart() {
        if (!this._canvasElement) return;

        const ctx = this._canvasElement.getContext('2d');
        if (!ctx) return;

        const config = this._getChartConfig();
        this._chart = new Chart(ctx, config);
    }

    private _updateChart() {
        if (!this._chart) {
            this._initializeChart();
            return;
        }

        const newData = this._prepareChartData();
        this._chart.data = newData;
        this._chart.update();
    }

    private _getChartConfig(): ChartConfiguration {
        return {
            type: 'bar',
            data: this._prepareChartData(),
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            boxWidth: 8,
                            padding: 20,
                            font: {
                                family: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
                            },
                            generateLabels: (chart) => {
                                const datasets = chart.data.datasets;
                                return datasets.map((dataset, i) => ({
                                    text: '  ' + dataset.label,
                                    fillStyle: dataset.backgroundColor as string,
                                    hidden: !chart.isDatasetVisible(i),
                                    lineCap: 'round',
                                    lineDash: [],
                                    lineDashOffset: 0,
                                    lineJoin: 'round',
                                    lineWidth: 0,
                                    strokeStyle: dataset.borderColor as string,
                                    pointStyle: 'circle',
                                    datasetIndex: i
                                }));
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => {
                                const label = context.dataset.label || '';
                                const value = this._formatIntWithK(context.parsed.y!);
                                return `${label}: ${value}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: true,
                        grid: {
                            display: true,
                            drawOnChartArea: true,
                            drawTicks: true,
                            color: (context) => {
                                // Make grid lines for labeled ticks darker
                                const index = context.index;
                                const totalTicks = context.chart.scales.x.ticks.length;
                                const skipInterval = Math.ceil(totalTicks / 12);
                                return index % skipInterval === 0
                                    ? 'rgba(0, 0, 0, 0.1)'
                                    : 'rgba(0, 0, 0, 0.03)';
                            },
                            tickLength: 8
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 0,
                            autoSkip: false,
                            callback: (value, index, ticks) => {
                                // Show label for every Nth tick to avoid crowding
                                const skipInterval = Math.ceil(ticks.length / 12);
                                if (index % skipInterval === 0) {
                                    // Get the actual label from the chart data
                                    return this._chart?.data.labels?.[index] || '';
                                }
                                return '';
                            }
                        }
                    },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        ticks: {
                            callback: (value) => this._formatIntWithK(Number(value))
                        }
                    }
                }
            }
        };
    }

    private _prepareChartData() {
        if (!this.data || this.data.length === 0) {
            return { labels: [], datasets: [] };
        }

        // Sort by timestamp
        const sortedData = [...this.data].sort(
            (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
        );

        // Determine granularity (hourly vs daily)
        const isHourly = sortedData.length >= 2 &&
            (new Date(sortedData[1].timestamp).getTime() - new Date(sortedData[0].timestamp).getTime()) < 2 * 60 * 60 * 1000;

        // Generate complete time series with gaps filled
        const filledData = this._fillTimeSeriesGaps(sortedData, isHourly);

        // Extract labels (x-axis: timestamps)
        const labels = filledData.map(point =>
            this._formatTimestamp(new Date(point.timestamp))
        );

        // Create datasets based on selected metric
        const datasets = this.metric === 'tokens'
            ? [
                {
                    label: 'Input Tokens',
                    data: filledData.map(p => p.inputTokens),
                    backgroundColor: 'rgb(27, 38, 79)',
                    borderColor: 'rgb(27, 38, 79)',
                    borderWidth: 1
                },
                {
                    label: 'Output Tokens',
                    data: filledData.map(p => p.outputTokens),
                    backgroundColor: 'rgb(245, 193, 188)',
                    borderColor: 'rgb(245, 193, 188)',
                    borderWidth: 1
                }
              ]
            : [
                {
                    label: 'Total Requests',
                    data: filledData.map(p => p.requestCount),
                    backgroundColor: 'rgb(27, 38, 79)',
                    borderColor: 'rgb(27, 38, 79)',
                    borderWidth: 1
                }
              ];

        return { labels, datasets };
    }

    private _fillTimeSeriesGaps(sortedData: UsageTimeSeriesPointModel[], isHourly: boolean): UsageTimeSeriesPointModel[] {
        // Use the provided date range, or fall back to data range if not provided
        const startDate = this.fromDate
            ? this._normalizeDate(new Date(this.fromDate), isHourly)
            : sortedData.length > 0
                ? this._normalizeDate(new Date(sortedData[0].timestamp), isHourly)
                : new Date();

        const endDate = this.toDate
            ? this._normalizeDate(new Date(this.toDate), isHourly)
            : sortedData.length > 0
                ? this._normalizeDate(new Date(sortedData[sortedData.length - 1].timestamp), isHourly)
                : new Date();

        const result: UsageTimeSeriesPointModel[] = [];
        const dataMap = new Map<number, UsageTimeSeriesPointModel>();

        // Create a map of existing data points by normalized timestamp
        sortedData.forEach(point => {
            const date = new Date(point.timestamp);
            const normalizedTime = this._normalizeDate(date, isHourly).getTime();
            dataMap.set(normalizedTime, point);
        });

        let currentDate = new Date(startDate);

        while (currentDate <= endDate) {
            const currentTime = currentDate.getTime();
            const existingPoint = dataMap.get(currentTime);

            if (existingPoint) {
                result.push(existingPoint);
            } else {
                // Create a zero-value point for missing period
                result.push({
                    timestamp: currentDate.toISOString(),
                    requestCount: 0,
                    totalTokens: 0,
                    inputTokens: 0,
                    outputTokens: 0,
                    successCount: 0,
                    failureCount: 0
                });
            }

            // Move to next period
            if (isHourly) {
                currentDate.setHours(currentDate.getHours() + 1);
            } else {
                currentDate.setDate(currentDate.getDate() + 1);
            }
        }

        return result;
    }

    private _normalizeDate(date: Date, isHourly: boolean): Date {
        const normalized = new Date(date);
        if (isHourly) {
            normalized.setMinutes(0, 0, 0);
        } else {
            normalized.setHours(0, 0, 0, 0);
        }
        return normalized;
    }

    private _getTimeSeriesKey(date: Date, isHourly: boolean): string {
        if (isHourly) {
            return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}-${date.getHours()}`;
        } else {
            return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
        }
    }

    private _formatTimestamp(date: Date): string {
        if (!this.data || this.data.length < 2) {
            return date.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric'
            });
        }

        // Determine if data is hourly or daily based on time difference
        const diff = new Date(this.data[1].timestamp).getTime() -
                     new Date(this.data[0].timestamp).getTime();
        const isHourly = diff < 2 * 60 * 60 * 1000; // Less than 2 hours = hourly

        if (isHourly) {
            return date.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                hour: 'numeric'
            });
        } else {
            return date.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric'
            });
        }
    }

    private _formatIntWithK(value: number, decimals: number = 1): string {
        const abs = Math.abs(value);

        if (abs >= 1_000_000) {
            return `${(value / 1_000_000).toFixed(decimals)}M`;
        }
        if (abs >= 1_000) {
            return `${(value / 1_000).toFixed(decimals)}k`;
        }
        return value.toString();
    }

    private _handleMetricToggle(metric: 'tokens' | 'requests') {
        this.metric = metric;
    }

    override render() {
        if (!this.data || this.data.length === 0) {
            return html`
                <uui-box headline="Usage Over Time" class="time-series-box">
                    <div class="empty-state">
                        <p>No data available for the selected time range.</p>
                    </div>
                </uui-box>
            `;
        }

        return html`
            <uui-box headline="Usage Over Time" class="time-series-box">
                <div class="chart-controls" slot="header-actions">
                    <uui-button-group>
                        <uui-button
                            look=${this.metric === 'tokens' ? 'primary' : 'default'}
                            label="Tokens"
                            @click=${() => this._handleMetricToggle('tokens')}
                        >
                            Tokens
                        </uui-button>
                        <uui-button
                            look=${this.metric === 'requests' ? 'primary' : 'default'}
                            label="Requests"
                            @click=${() => this._handleMetricToggle('requests')}
                        >
                            Requests
                        </uui-button>
                    </uui-button-group>
                </div>
                <div class="chart-container">
                    <canvas id="chart-canvas"></canvas>
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
                min-height: 400px;
            }

            .chart-controls {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
            }

            .chart-container {
                position: relative;
                height: 350px;
                padding: 0 var(--uui-size-space-4);
            }

            .empty-state {
                display: flex;
                align-items: center;
                justify-content: center;
                min-height: 300px;
                text-align: center;
                color: var(--uui-color-text-alt);
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
