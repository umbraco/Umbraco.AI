import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { usageRepository } from "../../repository";
import type { UsageSummaryResponseModel, UsageTimeSeriesPointModel, UsageBreakdownItemModel } from "../../../../api";
import type { DateRangeType } from "../../../components";
import type { UaiAnalyticsQueryParams } from "../../../types.js";

@customElement("uai-usage-dashboard")
export class UaiUsageDashboardElement extends UmbLitElement {
    @state()
    private _summary?: UsageSummaryResponseModel;

    @state()
    private _timeSeries?: UsageTimeSeriesPointModel[];

    @state()
    private _providerBreakdown?: UsageBreakdownItemModel[];

    @state()
    private _modelBreakdown?: UsageBreakdownItemModel[];

    @state()
    private _profileBreakdown?: UsageBreakdownItemModel[];

    @state()
    private _userBreakdown?: UsageBreakdownItemModel[];

    @state()
    private _loading = true;

    @state()
    private _error?: string;

    @state()
    private _dateRange: DateRangeType = "last24h";

    constructor() {
        super();
        this._loadData();
    }

    /**
     * Converts the selected date range into query parameters
     */
    private _getDateRange(): UaiAnalyticsQueryParams {
        const to = new Date().toISOString();
        const from = new Date();

        switch (this._dateRange) {
            case "last24h":
                from.setHours(from.getHours() - 24);
                break;
            case "last7d":
                from.setDate(from.getDate() - 7);
                break;
            case "last30d":
                from.setDate(from.getDate() - 30);
                break;
        }

        return {
            from: from.toISOString(),
            to,
        };
    }

    /**
     * Handles date range changes and triggers data reload
     */
    private _handleDateRangeChange(event: CustomEvent) {
        const detail = event.detail as { dateRange: DateRangeType };
        this._dateRange = detail.dateRange;
        this._loadData();
    }

    /**
     * Loads data with loading and error state management
     */
    private async _loadData() {
        this._loading = true;
        this._error = undefined;

        try {
            const params = this._getDateRange();
            const [summary, timeSeries, providerBreakdown, modelBreakdown, profileBreakdown, userBreakdown] =
                await Promise.all([
                    usageRepository.getSummary(params),
                    usageRepository.getTimeSeries(params),
                    usageRepository.getBreakdownByProvider(params),
                    usageRepository.getBreakdownByModel(params),
                    usageRepository.getBreakdownByProfile(params),
                    usageRepository.getBreakdownByUser(params),
                ]);

            this._summary = summary;
            this._timeSeries = timeSeries;
            this._providerBreakdown = providerBreakdown;
            this._modelBreakdown = modelBreakdown;
            this._profileBreakdown = profileBreakdown;
            this._userBreakdown = userBreakdown;
        } catch (error) {
            console.error("Failed to load usage analytics data:", error);
            this._error = "Failed to load usage analytics data. Please try again.";
        } finally {
            this._loading = false;
        }
    }

    /**
     * Renders loading state
     */
    private _renderLoading() {
        return html`
            <uui-loader-bar></uui-loader-bar>
            <div class="loading-message">Loading analytics data...</div>
        `;
    }

    /**
     * Renders error state
     */
    private _renderError() {
        return html`
            <uui-box>
                <div class="error-message">
                    <uui-icon name="icon-alert"></uui-icon>
                    <p>${this._error}</p>
                </div>
            </uui-box>
        `;
    }

    /**
     * Renders the dashboard content
     */
    private _renderContent() {
        const dateRange = this._getDateRange();
        return html`<div class="usage-dashboard-content">
            <uai-usage-summary-cards .summary=${this._summary}></uai-usage-summary-cards>

            <uai-usage-time-series-chart
                .data=${this._timeSeries}
                .dateRangeType=${this._dateRange}
                .fromDate=${dateRange.from}
                .toDate=${dateRange.to}
            >
            </uai-usage-time-series-chart>

            <div class="breakdowns">
                <uai-analytics-breakdown-table headline="By Provider" .data=${this._providerBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table headline="By Model" .data=${this._modelBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table headline="By Profile" .data=${this._profileBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table headline="By User" .data=${this._userBreakdown}>
                </uai-analytics-breakdown-table>
            </div>
        </div> `;
    }

    override render() {
        return html`
            <uai-analytics-dashboard-layout headline="Usage" @change=${this._handleDateRangeChange}>
                ${this._loading ? this._renderLoading() : this._error ? this._renderError() : this._renderContent()}
            </uai-analytics-dashboard-layout>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-2);
            }

            .loading-message {
                text-align: center;
                padding: var(--uui-size-space-6);
                color: var(--uui-color-text-alt);
            }

            .error-message {
                display: flex;
                flex-direction: column;
                align-items: center;
                padding: var(--uui-size-space-6);
                color: var(--uui-color-danger);
            }

            .error-message uui-icon {
                font-size: 3rem;
                margin-bottom: var(--uui-size-space-3);
            }

            .usage-dashboard-content {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            .breakdowns {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
                gap: var(--uui-size-space-5);
            }
        `,
    ];
}

export default UaiUsageDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-usage-dashboard": UaiUsageDashboardElement;
    }
}
