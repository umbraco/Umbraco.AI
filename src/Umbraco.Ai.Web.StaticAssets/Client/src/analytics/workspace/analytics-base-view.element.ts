import { css, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiAnalyticsQueryParams } from '../repository/index.js';

export type DateRangeType = 'last24h' | 'last7d' | 'last30d';

/**
 * Base component for analytics views that handles common functionality:
 * - Loading and error states
 * - Date range selection
 * - Data loading lifecycle
 */
export abstract class UaiAnalyticsBaseViewElement extends UmbLitElement {

    @property({ type: String })
    headline = 'Analytics';

    @state()
    protected _loading = true;

    @state()
    protected _error?: string;

    @state()
    protected _dateRange: DateRangeType = 'last7d';

    constructor() {
        super();
        this._loadData();
    }

    /**
     * Converts the selected date range into query parameters
     */
    protected _getDateRange(): UaiAnalyticsQueryParams {
        const to = new Date().toISOString();
        const from = new Date();

        switch (this._dateRange) {
            case 'last24h':
                from.setHours(from.getHours() - 24);
                break;
            case 'last7d':
                from.setDate(from.getDate() - 7);
                break;
            case 'last30d':
                from.setDate(from.getDate() - 30);
                break;
        }

        return {
            from: from.toISOString(),
            to
        };
    }

    /**
     * Handles date range changes and triggers data reload
     */
    protected _handleDateRangeChange(event: CustomEvent) {
        const select = event.target as any;
        this._dateRange = select.value as DateRangeType;
        this._loadData();
    }

    /**
     * Gets date range options for the uui-select component
     */
    protected _getDateRangeOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        return [
            { name: 'Last 24 Hours', value: 'last24h', selected: this._dateRange === 'last24h' },
            { name: 'Last 7 Days', value: 'last7d', selected: this._dateRange === 'last7d' },
            { name: 'Last 30 Days', value: 'last30d', selected: this._dateRange === 'last30d' }
        ];
    }

    /**
     * Loads data with loading and error state management
     */
    protected async _loadData() {
        this._loading = true;
        this._error = undefined;

        try {
            const params = this._getDateRange();
            await this.loadData(params);
        } catch (error) {
            console.error('Failed to load analytics data:', error);
            this._error = this.getErrorMessage(error);
        } finally {
            this._loading = false;
        }
    }

    /**
     * Abstract method for child components to implement their data loading logic
     */
    protected abstract loadData(params: UaiAnalyticsQueryParams): Promise<void>;

    /**
     * Allows child components to customize error messages
     */
    protected getErrorMessage(_error: unknown): string {
        return 'Failed to load analytics data. Please try again.';
    }

    /**
     * Renders the date range selector header
     */
    protected _renderHeader() {
        return html`
            <div class="analytics-header">
                <h3>${this.headline}</h3>
                <uui-select
                    label="Time Range"
                    .value=${this._dateRange}
                    .options=${this._getDateRangeOptions()}
                    @change=${this._handleDateRangeChange}>
                </uui-select>
            </div>
        `;
    }

    /**
     * Renders loading state
     */
    protected _renderLoading() {
        return html`
            <div class="analytics-container">
                <uui-loader-bar></uui-loader-bar>
                <div class="loading-message">Loading analytics data...</div>
            </div>
        `;
    }

    /**
     * Renders error state
     */
    protected _renderError() {
        return html`
            <div class="analytics-container">
                <uui-box>
                    <div class="error-message">
                        <uui-icon name="icon-alert"></uui-icon>
                        <p>${this._error}</p>
                    </div>
                </uui-box>
            </div>
        `;
    }

    /**
     * Abstract method for child components to render their content
     */
    protected abstract renderContent(): unknown;

    override render() {
        if (this._loading) {
            return this._renderLoading();
        }

        if (this._error) {
            return this._renderError();
        }

        return html`
            <div class="analytics-container">
                ${this._renderHeader()}
                ${this.renderContent()}
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-space-6);
            }

            .analytics-container {
                height: 100%;
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            .analytics-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: var(--uui-size-space-3);
            }

            .analytics-header h2 {
                margin: 0;
                font-size: var(--uui-type-h3-size);
                font-weight: 600;
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
            
            h3 {
                margin: 0;
            }
        `,
    ];
}
