// A new value import from @umbraco/analytics-ui must also be added to the Copilot Workspace
// test stand-in (Umbraco.AI.Agent.Copilot.Workspace/.../Client/test/analytics-ui.stub.ts).
import {
    UAUI_TABLE_CELL,
    UauiTableServerController,
    uauiFormatDateRange,
    uauiGroupByOptions,
} from "@umbraco/analytics-ui";
import type {
    UauiCompare,
    UauiDateRange,
    UauiGroupBy,
    UauiTableColumn,
    UauiTableServerState,
    UauiToggleGroupChangeDetail,
} from "@umbraco/analytics-ui";
import { css, customElement, html, ref, state } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UsageOverviewResponseModel } from "../../../../api/types.gen.js";
import {
    getUsageChartMoment,
    mapBreakdownResult,
    mapUsageCachedInput,
    mapUsageChart,
    mapUsageTiles,
    toUsageBreakdownQuery,
    toUsageOverviewQuery,
    usageChartKind,
} from "../../adapter/usage-adapter.js";
import type {
    UaiUsageBreakdownDimension,
    UaiUsageBreakdownResult,
    UaiUsageBreakdownRow,
    UaiUsageChartData,
    UaiUsageChartMoment,
    UaiUsageMetric,
    UaiUsageSeriesId,
    UaiUsageTile,
} from "../../adapter/usage-adapter.js";
import { UAI_ANALYTICS_WORKSPACE_CONTEXT } from "../../../workspace/analytics-root/analytics-workspace.context-token.js";
import { defaultUsageDateRange, isSameUsageDateRange } from "../../usage-date-range.js";
import type { UaiUsageDateRange } from "../../usage-date-range.js";
import { UaiUsageRepository } from "../../repository/usage.repository.js";

const COMPACT: Intl.NumberFormatOptions = { notation: "compact", maximumFractionDigits: 1 };
const PERCENT: Intl.NumberFormatOptions = { style: "percent", maximumFractionDigits: 1 };
const SECONDS: Intl.NumberFormatOptions = { style: "unit", unit: "second", maximumFractionDigits: 2 };
const MILLISECONDS: Intl.NumberFormatOptions = { style: "unit", unit: "millisecond", maximumFractionDigits: 0 };

const DIMENSION_LABEL_KEYS: Record<UaiUsageBreakdownDimension, string> = {
    provider: "uaiUsage_dimensionProvider",
    model: "uaiUsage_dimensionModel",
    profile: "uaiUsage_dimensionProfile",
    user: "general_user",
};

const SERIES_LABEL_KEYS: Record<UaiUsageSeriesId, string> = {
    succeeded: "uaiUsage_seriesSucceeded",
    failed: "uaiUsage_seriesFailed",
    input: "uaiUsage_seriesInput",
    cachedInput: "uaiUsage_cachedInput",
    output: "uaiUsage_seriesOutput",
    successRate: "uaiUsage_successRate",
    averageDuration: "uaiUsage_averageDuration",
};

const METRIC_LABEL_KEYS: Record<UaiUsageMetric, string> = {
    requests: "uaiUsage_requests",
    inputTokens: "uaiUsage_inputTokens",
    outputTokens: "uaiUsage_outputTokens",
    totalTokens: "uaiUsage_totalTokens",
    successRate: "uaiUsage_successRate",
    averageDuration: "uaiUsage_averageDuration",
};

const METRIC_FORMATS: Record<UaiUsageMetric, Intl.NumberFormatOptions> = {
    requests: COMPACT,
    inputTokens: COMPACT,
    outputTokens: COMPACT,
    totalTokens: COMPACT,
    successRate: PERCENT,
    averageDuration: SECONDS,
};

// The server names usage with no recorded model, provider or profile `unknown` and usage with no user `anonymous`.
const PLACEHOLDER_KEY_TERMS: Record<string, string> = {
    unknown: "general_unknown",
    anonymous: "uaiUsage_anonymous",
};

@customElement("uai-usage-dashboard")
export class UaiUsageDashboardElement extends UmbLitElement {
    #repository = new UaiUsageRepository(this);
    #overviewAbort?: AbortController;

    @state() private _range: UauiDateRange = defaultUsageDateRange().range;
    @state() private _compare?: UauiCompare;
    @state() private _groupBy: UauiGroupBy = "day";
    @state() private _groupByOptions: UauiGroupBy[] = uauiGroupByOptions(this._range);
    @state() private _metric: UaiUsageMetric = "requests";
    @state() private _dimension: UaiUsageBreakdownDimension = "model";

    @state() private _overview?: UsageOverviewResponseModel;
    @state() private _moment: UaiUsageChartMoment = {};
    @state() private _loading = true;
    @state() private _error = false;

    @state() private _chart: UaiUsageChartData = { series: [], compareSeries: [] };

    // Built in willUpdate, and again when cached input is reported or not, or the labels' translation changes.
    #columns: UauiTableColumn[] = [];
    #dimensionOptions: { value: string; label: string }[] = [];
    #labelsKey?: string;

    // The table's sort, page and search live in the controller; it loads on connect and on the table's own events.
    #breakdown = new UauiTableServerController<UaiUsageBreakdownResult>(this, {
        sort: { key: "requests", direction: "desc" },
        pageSize: 10,
        load: (state, signal) => this.#loadBreakdown(state, signal),
    });

    constructor() {
        super();
        // The header's picker sets the range on the workspace context. The first value is the default this
        // element already holds, so only a different range reloads.
        this.consumeContext(UAI_ANALYTICS_WORKSPACE_CONTEXT, (context) => {
            this.observe(context?.dateRange, (dateRange) => dateRange && this.#onDateRange(dateRange));
        });
    }

    override connectedCallback() {
        super.connectedCallback();
        void this.#loadOverview();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#overviewAbort?.abort();
    }

    protected override willUpdate(changed: PropertyValues) {
        if (changed.has("_overview") || changed.has("_metric")) {
            // With nothing in either period the chart shows its own empty state rather than flat zero bars.
            const empty =
                !!this._overview &&
                this._overview.current.totals.requests === 0 &&
                (this._overview.previous?.totals.requests ?? 0) === 0;
            this._chart = empty
                ? { series: [], compareSeries: [] }
                : mapUsageChart(this._metric, this._overview, (id) => this.localize.term(SERIES_LABEL_KEYS[id]));
        }

        // The localize controller updates the host when the language or its translations change; one label
        // stands in for them, so the columns and options are rebuilt only then.
        const hasCachedInput = this.#breakdown.result?.reportsCachedInput ?? false;
        const labelsKey = `${hasCachedInput}|${this.localize.term("uaiUsage_requests")}`;
        if (labelsKey !== this.#labelsKey) {
            this.#labelsKey = labelsKey;
            this.#columns = this.#buildColumns(hasCachedInput);
            this.#dimensionOptions = (Object.keys(DIMENSION_LABEL_KEYS) as UaiUsageBreakdownDimension[]).map(
                (dimension) => ({ value: dimension, label: this.localize.term(DIMENSION_LABEL_KEYS[dimension]) }),
            );
        }
    }

    async #loadOverview() {
        this.#overviewAbort?.abort();
        const abort = (this.#overviewAbort = new AbortController());
        const clock = new Date();

        this._loading = true;
        this._error = false;

        const { data, error } = await this.#repository.getOverview(
            toUsageOverviewQuery({ range: this._range, compare: this._compare, groupBy: this._groupBy }),
            abort.signal,
        );

        // A newer request, or leaving the page, makes this answer stale.
        if (abort.signal.aborted) return;

        this._overview = data;
        this._error = !!error;
        this._moment = getUsageChartMoment(this._range, this._groupBy, clock);
        this._loading = false;
    }

    async #loadBreakdown(state: Readonly<UauiTableServerState>, signal: AbortSignal) {
        const { data, error } = await this.#repository.getBreakdown(
            toUsageBreakdownQuery({ dimension: this._dimension, range: this._range, compare: this._compare, state }),
            signal,
        );
        signal.throwIfAborted();
        if (error || !data) throw error;

        const result: UaiUsageBreakdownResult = mapBreakdownResult(data);
        const rows: UaiUsageBreakdownRow[] = result.rows;
        const compareRows: UaiUsageBreakdownRow[] = result.compareRows;
        return {
            ...result,
            rows: rows.map((row) => this.#localizeRow(row)),
            compareRows: compareRows.map((row) => this.#localizeRow(row)),
        };
    }

    #localizeRow<T extends Record<string, unknown>>(row: T): T {
        const term = typeof row.key === "string" ? PLACEHOLDER_KEY_TERMS[row.key] : undefined;
        return term && row.name === row.key ? { ...row, name: this.localize.term(term) } : row;
    }

    // The range changed: both parts reload and the table goes back to page 1.
    #onDateRange(dateRange: UaiUsageDateRange) {
        if (isSameUsageDateRange(dateRange, { range: this._range, compare: this._compare })) return;
        this._range = dateRange.range;
        this._compare = dateRange.compare;
        // Hour is only offered for short ranges; when the chosen option is gone, take the first one.
        this._groupByOptions = uauiGroupByOptions(this._range);
        if (!this._groupByOptions.includes(this._groupBy)) this._groupBy = this._groupByOptions[0];
        void this.#loadOverview();
        void this.#breakdown.reload({ resetPage: true });
    }

    #onGroupBy(event: CustomEvent<{ value: UauiGroupBy }>) {
        this._groupBy = event.detail.value;
        void this.#loadOverview();
    }

    #onDimension(event: CustomEvent<UauiToggleGroupChangeDetail>) {
        this._dimension = event.detail.value as UaiUsageBreakdownDimension;
        void this.#breakdown.reload({ resetPage: true });
    }

    // A tile was pressed: the chart plots that metric. Nothing is fetched, the buckets already hold every metric.
    #selectMetric(metric: UaiUsageMetric) {
        this._metric = metric;
    }

    // Before an overview has loaded the tiles show their skeleton or error, never "no data".
    #noData(tile: UaiUsageTile) {
        return !!this._overview && tile.noData;
    }

    // The Input tokens tile adds the period's cached input as a second line. The library keeps the line while
    // the tile reloads and shows none without a value, so it is only set when a provider reported a figure.
    #renderMetricTile(metric: UaiUsageMetric, icon: string, tile: UaiUsageTile) {
        const detail = metric === "inputTokens" ? mapUsageCachedInput(this._overview) : undefined;
        return html`<uaui-kpi-tile
            label=${this.localize.term(METRIC_LABEL_KEYS[metric])}
            icon=${icon}
            .value=${tile.value}
            .valueFormat=${METRIC_FORMATS[metric]}
            .previous=${tile.previous}
            ?no-data=${this.#noData(tile)}
            ?loading=${this._loading}
            ?error=${this._error}
            .detail=${detail === undefined ? undefined : this.localize.term("uaiUsage_cachedInputDetail")}
            .detailValue=${detail}
            .deltaUnit=${metric === "successRate" ? "points" : "percent"}
            .lowerIsBetter=${metric === "averageDuration"}
            selectable
            .deselectable=${false}
            .selected=${this._metric === metric}
            @selected=${() => this.#selectMetric(metric)}
        ></uaui-kpi-tile>`;
    }

    #renderTiles() {
        const tiles = mapUsageTiles(this._overview);
        return html`<div class="tiles-container">
            <div class="tiles">
                ${this.#renderMetricTile("requests", "icon-chart", tiles.requests)}
                ${this.#renderMetricTile("inputTokens", "icon-inbox", tiles.inputTokens)}
                ${this.#renderMetricTile("outputTokens", "icon-navigation-right", tiles.outputTokens)}
                ${this.#renderMetricTile("totalTokens", "icon-coins", tiles.totalTokens)}
                ${this.#renderMetricTile("successRate", "icon-check", tiles.successRate)}
                ${this.#renderMetricTile("averageDuration", "icon-time", tiles.averageDuration)}
            </div>
        </div>`;
    }

    #buildColumns(withCachedInput: boolean): UauiTableColumn[] {
        const cachedInput: UauiTableColumn[] = withCachedInput
            ? [
                  {
                      key: "cachedInputTokens",
                      label: this.localize.term("uaiUsage_cachedInput"),
                      elementName: UAUI_TABLE_CELL.number,
                      align: "end",
                      valueFormat: COMPACT,
                  },
              ]
            : [];

        return [
            {
                key: "name",
                label: this.localize.term("general_name"),
                elementName: UAUI_TABLE_CELL.text,
                sortable: true,
                width: "28%",
            },
            {
                key: "requests",
                label: this.localize.term("uaiUsage_requests"),
                elementName: UAUI_TABLE_CELL.share,
                sortable: true,
                sortAs: "number",
                width: "24%",
            },
            {
                key: "totalTokens",
                label: this.localize.term("uaiUsage_tokens"),
                elementName: UAUI_TABLE_CELL.share,
                sortable: true,
                sortAs: "number",
                width: "24%",
            },
            ...cachedInput,
            {
                key: "successRate",
                label: this.localize.term("uaiUsage_successRate"),
                elementName: UAUI_TABLE_CELL.percent,
                align: "end",
                sortable: true,
                sortAs: "number",
            },
            {
                key: "averageDurationMs",
                label: this.localize.term("uaiUsage_averageDuration"),
                elementName: UAUI_TABLE_CELL.number,
                align: "end",
                sortable: true,
                sortAs: "number",
                valueFormat: MILLISECONDS,
                // A faster average is the improvement, so a fall shows as good.
                meta: { lowerIsBetter: true },
            },
        ];
    }

    override render() {
        const metricLabel = this.localize.term(METRIC_LABEL_KEYS[this._metric]);
        const breakdownLabel = this.localize.term("uaiUsage_breakdown");

        return html`
            ${this.#renderTiles()}

            <uui-box
                headline=${this.localize.term("uaiUsage_overTime", metricLabel)}
                headline-variant="h3"
                style="--uui-box-default-padding: 0"
            >
                <uaui-group-by
                    slot="header-actions"
                    .options=${this._groupByOptions}
                    .value=${this._groupBy}
                    @change=${this.#onGroupBy}
                ></uaui-group-by>
                <uaui-time-series-chart
                    .kind=${usageChartKind(this._metric)}
                    .valueFormat=${METRIC_FORMATS[this._metric]}
                    .lowerIsBetter=${this._metric === "averageDuration"}
                    .series=${this._chart.series}
                    .compareSeries=${this._chart.compareSeries}
                    .range=${this._range}
                    .compare=${this._compare}
                    .compareLabel=${this._compare ? uauiFormatDateRange(this.localize, this._compare) : undefined}
                    .groupBy=${this._groupBy}
                    .now=${this._moment.now}
                    .incompleteFrom=${this._moment.incompleteFrom}
                    ?loading=${this._loading}
                    ?error=${this._error}
                    retryable
                    @retry=${() => void this.#loadOverview()}
                ></uaui-time-series-chart>
            </uui-box>

            <uui-box headline=${breakdownLabel} headline-variant="h3" style="--uui-box-default-padding: 0">
                <uaui-toggle-group
                    slot="header-actions"
                    label=${this.localize.term("uaiUsage_breakdownBy")}
                    .value=${this._dimension}
                    .options=${this.#dimensionOptions}
                    @change=${this.#onDimension}
                ></uaui-toggle-group>
                <uaui-data-table
                    ${ref(this.#breakdown.attach)}
                    label=${breakdownLabel}
                    row-key="key"
                    .columns=${this.#columns}
                >
                    <div slot="toolbar"><uaui-search-field></uaui-search-field></div>
                </uaui-data-table>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-4);
                padding: var(--uui-size-layout-1);
            }
            /* Three across, then two, then one, by the width of the view. */
            .tiles-container {
                container-type: inline-size;
            }
            .tiles {
                display: grid;
                grid-template-columns: repeat(3, minmax(0, 1fr));
                gap: var(--uui-size-space-4);
            }
            @container (max-width: 700px) {
                .tiles {
                    grid-template-columns: repeat(2, minmax(0, 1fr));
                }
            }
            @container (max-width: 480px) {
                .tiles {
                    grid-template-columns: minmax(0, 1fr);
                }
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
