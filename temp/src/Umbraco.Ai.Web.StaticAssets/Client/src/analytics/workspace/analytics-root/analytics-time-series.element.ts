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
            type: 'line',
            data: this._prepareChartData(),
            options: {
                responsive: true,
                maintainAspectRatio: false,
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
                            font: {
                                family: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
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
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45
                        }
                    },
                    y: {
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

        // Extract labels (x-axis: timestamps)
        const labels = sortedData.map(point =>
            this._formatTimestamp(new Date(point.timestamp))
        );

        // Create datasets based on selected metric
        const datasets = this.metric === 'tokens'
            ? [
                {
                    label: 'Input Tokens',
                    data: sortedData.map(p => p.inputTokens),
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: 'Output Tokens',
                    data: sortedData.map(p => p.outputTokens),
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.1)',
                    fill: true,
                    tension: 0.4
                }
              ]
            : [
                {
                    label: 'Total Requests',
                    data: sortedData.map(p => p.requestCount),
                    borderColor: 'rgb(54, 162, 235)',
                    backgroundColor: 'rgba(54, 162, 235, 0.1)',
                    fill: true,
                    tension: 0.4
                }
              ];

        return { labels, datasets };
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
                padding: var(--uui-size-space-4);
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
