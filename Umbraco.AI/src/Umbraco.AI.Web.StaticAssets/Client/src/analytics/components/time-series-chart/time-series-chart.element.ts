import { css, html, customElement, property, query, state, type PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { Chart, ChartConfiguration, registerables } from 'chart.js';

/**
 * Generic time series data point
 */
export interface AnalyticsTimeSeriesDataPoint {
    timestamp: string;
    [key: string]: string | number;
}

/**
 * Configuration for a dataset in the chart
 */
export interface AnalyticsTimeSeriesDataset {
    label: string;
    dataKey: string;
    backgroundColor: string;
    borderColor: string;
}

/**
 * Tab configuration for grouped dataset views
 */
export interface AnalyticsTimeSeriesTab {
    id: string;
    label: string;
    datasets: AnalyticsTimeSeriesDataset[];
}

@customElement("uai-analytics-time-series-chart")
export class UaiAnalyticsTimeSeriesChartElement extends UmbLitElement {

    @property({ type: Array })
    data?: AnalyticsTimeSeriesDataPoint[];

    @property({ type: Array })
    tabs?: AnalyticsTimeSeriesTab[];

    @property({ type: Array })
    datasets?: AnalyticsTimeSeriesDataset[];

    @property({ type: String })
    headline = 'Time Series';

    @property({ type: Object })
    chartOptions?: Partial<ChartConfiguration['options']>;

    @property({ type: Function })
    valueFormatter?: (value: number) => string;

    @property({ type: Function })
    timestampFormatter?: (date: Date) => string;

    @property({ type: String })
    dateRangeType?: 'last24h' | 'last7d' | 'last30d';

    @property({ type: String })
    fromDate?: string;

    @property({ type: String })
    toDate?: string;

    @state()
    private _activeTab?: string;

    @state()
    private _chart?: Chart;

    @query('#chart-canvas')
    private _canvasElement?: HTMLCanvasElement;

    constructor() {
        super();
        Chart.register(...registerables);
    }

    override firstUpdated() {
        // Initialize active tab if tabs are provided
        if (this.tabs && this.tabs.length > 0 && !this._activeTab) {
            this._activeTab = this.tabs[0].id;
        }
        this._initializeChart();
    }

    override updated(changedProperties: PropertyValues) {
        super.updated(changedProperties);

        if (changedProperties.has('data') ||
            changedProperties.has('_activeTab') ||
            changedProperties.has('datasets') ||
            changedProperties.has('tabs')) {
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
        const defaultOptions: ChartConfiguration['options'] = {
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
                            const value = this.valueFormatter
                                ? this.valueFormatter(context.parsed.y!)
                                : this._defaultValueFormatter(context.parsed.y!);
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
                        callback: (_value, index, ticks) => {
                            const skipInterval = Math.ceil(ticks.length / 12);
                            if (index % skipInterval === 0) {
                                return (this._chart?.data.labels?.[index] as string) || '';
                            }
                            return '';
                        }
                    }
                },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    ticks: {
                        callback: (value) => this.valueFormatter
                            ? this.valueFormatter(Number(value))
                            : this._defaultValueFormatter(Number(value))
                    }
                }
            }
        };

        // Merge with custom options if provided
        const mergedOptions = this.chartOptions
            ? this._deepMerge(defaultOptions, this.chartOptions as Record<string, any>)
            : defaultOptions;

        return {
            type: 'bar',
            data: this._prepareChartData(),
            options: mergedOptions
        };
    }

    private _prepareChartData() {
        if (!this.data || this.data.length === 0) {
            return { labels: [], datasets: [] };
        }

        // Determine which datasets to use
        const activeDatasets = this._getActiveDatasets();
        if (activeDatasets.length === 0) {
            return { labels: [], datasets: [] };
        }

        // Sort by timestamp
        const sortedData = [...this.data].sort(
            (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
        );

        // Determine granularity (hourly vs daily)
        const isHourly = this._isHourly(sortedData);

        // Generate complete time series with gaps filled
        const filledData = this._fillTimeSeriesGaps(sortedData, isHourly);

        // Extract labels (x-axis: timestamps)
        const labels = filledData.map(point => {
            const date = new Date(point.timestamp);
            return this.timestampFormatter
                ? this.timestampFormatter(date)
                : this._defaultTimestampFormatter(date, isHourly);
        });

        // Create datasets based on configuration
        const datasets = activeDatasets.map(config => ({
            label: config.label,
            data: filledData.map(point => (point[config.dataKey] as number) || 0),
            backgroundColor: config.backgroundColor,
            borderColor: config.borderColor,
            borderWidth: 1
        }));

        return { labels, datasets };
    }

    private _getActiveDatasets(): AnalyticsTimeSeriesDataset[] {
        if (this.tabs && this.tabs.length > 0) {
            const activeTab = this.tabs.find(t => t.id === this._activeTab);
            return activeTab?.datasets || [];
        }
        return this.datasets || [];
    }

    private _isHourly(sortedData: AnalyticsTimeSeriesDataPoint[]): boolean {
        // First, check if dateRangeType is provided (most reliable)
        if (this.dateRangeType === 'last24h' || this.dateRangeType === 'last7d') {
            return true; // Both 24h and 7d use hourly buckets
        }
        if (this.dateRangeType === 'last30d') {
            return false; // 30d uses daily buckets
        }

        // Second, calculate from fromDate/toDate if available
        if (this.fromDate && this.toDate) {
            const from = new Date(this.fromDate);
            const to = new Date(this.toDate);
            const daySpan = (to.getTime() - from.getTime()) / (1000 * 60 * 60 * 24);
            return daySpan <= 7; // Match backend logic: <= 7 days = hourly
        }

        // Fallback: infer from data points if we have at least 2
        if (sortedData.length < 2) {
            return true; // Default to hourly for single data point
        }
        const diff = new Date(sortedData[1].timestamp).getTime() -
                     new Date(sortedData[0].timestamp).getTime();
        return diff < 2 * 60 * 60 * 1000; // Less than 2 hours = hourly
    }

    private _fillTimeSeriesGaps(sortedData: AnalyticsTimeSeriesDataPoint[], isHourly: boolean): AnalyticsTimeSeriesDataPoint[] {
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

        const result: AnalyticsTimeSeriesDataPoint[] = [];
        const dataMap = new Map<number, AnalyticsTimeSeriesDataPoint>();

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
                const zeroPoint: AnalyticsTimeSeriesDataPoint = {
                    timestamp: currentDate.toISOString()
                };

                // Initialize all data keys with 0
                const activeDatasets = this._getActiveDatasets();
                activeDatasets.forEach(dataset => {
                    zeroPoint[dataset.dataKey] = 0;
                });

                result.push(zeroPoint);
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

    private _defaultTimestampFormatter(date: Date, isHourly: boolean): string {
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

    private _defaultValueFormatter(value: number, decimals: number = 1): string {
        const abs = Math.abs(value);

        if (abs >= 1_000_000) {
            return `${(value / 1_000_000).toFixed(decimals)}M`;
        }
        if (abs >= 1_000) {
            return `${(value / 1_000).toFixed(decimals)}k`;
        }
        return value.toString();
    }

    private _deepMerge<T extends Record<string, any>>(target: T, source: Partial<T>): T {
        const result = { ...target };

        for (const key in source) {
            if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
                result[key] = this._deepMerge(
                    (result[key] || {}) as Record<string, any>,
                    source[key] as Record<string, any>
                ) as T[Extract<keyof T, string>];
            } else {
                result[key] = source[key] as T[Extract<keyof T, string>];
            }
        }

        return result;
    }

    private _handleTabChange(tabId: string) {
        this._activeTab = tabId;
    }

    private _renderTabControls() {
        if (!this.tabs || this.tabs.length === 0) return html``;

        return html`
            <div class="chart-controls" slot="header-actions">
                <uui-button-group>
                    ${this.tabs.map(tab => html`
                        <uui-button
                            look=${this._activeTab === tab.id ? 'primary' : 'default'}
                            label=${tab.label}
                            @click=${() => this._handleTabChange(tab.id)}
                        >
                            ${tab.label}
                        </uui-button>
                    `)}
                </uui-button-group>
            </div>
        `;
    }

    override render() {
        if (!this.data || this.data.length === 0) {
            return html`
                <uui-box headline=${this.headline} class="time-series-box">
                    <div class="empty-state">
                        <p>No data available for the selected time range.</p>
                    </div>
                </uui-box>
            `;
        }

        return html`
            <uui-box headline=${this.headline} class="time-series-box">
                ${this._renderTabControls()}
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

export default UaiAnalyticsTimeSeriesChartElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-time-series-chart": UaiAnalyticsTimeSeriesChartElement;
    }
}
