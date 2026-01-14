import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UaiAnalyticsBaseViewElement } from '../analytics-base-view.element.js';
import { analyticsRepository, type UaiAnalyticsQueryParams } from '../../repository/index.js';
import type {
    UsageSummaryResponseModel,
    UsageTimeSeriesPointModel,
    UsageBreakdownItemModel
} from '../../../api/types.gen.js';
import './analytics-summary-cards.element.js';
import './analytics-time-series.element.js';
import './analytics-breakdown-table.element.js';
import { uaiWithProfile } from "../../../profile/directives/with-profile.directive.ts";

@customElement("uai-analytics-dashboard")
export class UaiAnalyticsDashboardElement extends UaiAnalyticsBaseViewElement {

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

    constructor() {
        super();
        this.headline = 'Usage';
    }

    protected async loadData(params: UaiAnalyticsQueryParams): Promise<void> {
        const [summary, timeSeries, providerBreakdown, modelBreakdown, profileBreakdown, userBreakdown] = await Promise.all([
            analyticsRepository.getSummary(params),
            analyticsRepository.getTimeSeries(params),
            analyticsRepository.getBreakdownByProvider(params),
            analyticsRepository.getBreakdownByModel(params),
            analyticsRepository.getBreakdownByProfile(params),
            analyticsRepository.getBreakdownByUser(params)
        ]);

        this._summary = summary;
        this._timeSeries = timeSeries;
        this._providerBreakdown = providerBreakdown;
        this._modelBreakdown = modelBreakdown;
        this._profileBreakdown = profileBreakdown;
        this._userBreakdown = userBreakdown;
    }

    protected renderContent() {
        return html`
            <uai-analytics-summary-cards .summary=${this._summary}></uai-analytics-summary-cards>

            <uai-analytics-time-series .data=${this._timeSeries}></uai-analytics-time-series>

            <div class="breakdowns">
                <uai-analytics-breakdown-table
                    headline="By Provider"
                    .data=${this._providerBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table
                    headline="By Model"
                    .data=${this._modelBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table
                    headline="By Profile"
                    .data=${this._profileBreakdown}>
                </uai-analytics-breakdown-table>

                <uai-analytics-breakdown-table
                    headline="By User"
                    .data=${this._userBreakdown}>
                </uai-analytics-breakdown-table>
            </div>
        `;
    }

    static override styles = [
        ...UaiAnalyticsBaseViewElement.styles,
        css`
            .breakdowns {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
                gap: var(--uui-size-space-4);
            }
        `,
    ];
}

export default UaiAnalyticsDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-dashboard": UaiAnalyticsDashboardElement;
    }
}
