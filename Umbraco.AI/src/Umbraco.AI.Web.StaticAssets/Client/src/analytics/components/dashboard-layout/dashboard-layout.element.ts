import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

export type DateRangeType = "last24h" | "last7d" | "last30d";

@customElement("uai-analytics-dashboard-layout")
export class UaiAnalyticsDashboardLayout extends UmbLitElement {
    @property({ type: String })
    headline = "Analytics";

    @state()
    protected _dateRange: DateRangeType = "last24h";

    constructor() {
        super();
    }

    /**
     * Handles date range changes and triggers data reload
     */
    protected _handleDateRangeChange(event: CustomEvent) {
        const select = event.target as any;
        this._dateRange = select.value as DateRangeType;
        this.dispatchEvent(
            new CustomEvent("change", { detail: { dateRange: this._dateRange }, bubbles: true, composed: true }),
        );
    }

    /**
     * Gets date range options for the uui-select component
     */
    protected _getDateRangeOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        return [
            { name: "Last 24 Hours", value: "last24h", selected: this._dateRange === "last24h" },
            { name: "Last 7 Days", value: "last7d", selected: this._dateRange === "last7d" },
            { name: "Last 30 Days", value: "last30d", selected: this._dateRange === "last30d" },
        ];
    }

    /**
     * Renders the date range selector header
     */
    protected _renderHeader() {
        return html`
            <div class="analytics-dashboard-header">
                <h3>${this.headline}</h3>
                <uui-select
                    label="Time Range"
                    .value=${this._dateRange}
                    .options=${this._getDateRangeOptions()}
                    @change=${this._handleDateRangeChange}
                >
                </uui-select>
            </div>
        `;
    }

    override render() {
        return html`
            <div class="analytics-dashboard-layout">
                ${this._renderHeader()}
                <div class="analytics-dashboard-content">
                    <slot></slot>
                </div>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .analytics-dashboard-layout {
                height: 100%;
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            .analytics-dashboard-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: var(--uui-size-space-3);
            }

            .analytics-dashboard-header h3 {
                margin: 0;
            }
        `,
    ];
}

export default UaiAnalyticsDashboardLayout;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-dashboard-layout": UaiAnalyticsDashboardLayout;
    }
}
