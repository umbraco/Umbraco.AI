import type {
    UauiCompare,
    UauiDateRange,
    UauiGroupBy,
    UauiSeries,
    UauiTableServerResult,
    UauiTableServerState,
} from "@umbraco/analytics-ui";
import type {
    GetUsageBreakdownData,
    GetUsageOverviewData,
    UsageBreakdownDimensionModel,
    UsageBreakdownResponseModel,
    UsageBreakdownRowModel,
    UsageBreakdownSortModel,
    UsageCountsModel,
    UsageGroupByModel,
    UsageOverviewPeriodModel,
    UsageOverviewResponseModel,
} from "../../../api/types.gen.js";
import { resolveUsageTimeZone, usageLocalDate } from "./usage-time-zone.js";

// Pure mapping between the usage endpoints and the `uaui-*` components: no Lit, no contexts, no fetch.

/** What one KPI tile shows. `previous` is only set when there is a comparison period with something to compare. */
export interface UaiUsageTile {
    value?: number;
    previous?: number;
    /** The period has no requests, so a rate or average is unknown rather than 0. */
    noData: boolean;
}

export interface UaiUsageTiles {
    requests: UaiUsageTile;
    inputTokens: UaiUsageTile;
    outputTokens: UaiUsageTile;
    totalTokens: UaiUsageTile;
    /** A fraction: 0.97 is 97%. */
    successRate: UaiUsageTile;
    /** Seconds. */
    averageDuration: UaiUsageTile;
}

const successRateOf = (counts: UsageCountsModel) => (counts.requests ? counts.succeeded / counts.requests : undefined);
const averageDurationOf = (counts: UsageCountsModel) =>
    counts.requests ? counts.durationMs / counts.requests / 1000 : undefined;

function tile(
    current: UsageCountsModel,
    previous: UsageCountsModel | null | undefined,
    pick: (counts: UsageCountsModel) => number | undefined,
): UaiUsageTile {
    const value = pick(current);

    return {
        value,
        // A rate or average with no requests behind it is unknown, so there is nothing to compare either.
        previous: value === undefined || !previous ? undefined : pick(previous),
        noData: current.requests === 0,
    };
}

/** The tiles of the page. Without a response (not loaded yet, or failed) every tile has no data. */
export function mapUsageTiles(response?: UsageOverviewResponseModel | null): UaiUsageTiles {
    const current = response?.current.totals;
    const previous = response?.previous?.totals;
    const of = (pick: (counts: UsageCountsModel) => number | undefined): UaiUsageTile =>
        current ? tile(current, previous, pick) : { noData: true };

    return {
        requests: of((c) => c.requests),
        inputTokens: of((c) => c.inputTokens),
        outputTokens: of((c) => c.outputTokens),
        totalTokens: of((c) => c.totalTokens),
        successRate: of(successRateOf),
        averageDuration: of(averageDurationOf),
    };
}

/**
 * The period's cached input tokens, for the second line of the Input tokens tile. Undefined when no provider
 * reported a figure, so there is no line (a reported 0 still has one).
 */
export function mapUsageCachedInput(response?: UsageOverviewResponseModel | null): number | undefined {
    return response?.current.totals.cachedInputTokens ?? undefined;
}

/** The metrics a tile can pick for the chart. */
export type UaiUsageMetric =
    | "requests"
    | "inputTokens"
    | "outputTokens"
    | "totalTokens"
    | "successRate"
    | "averageDuration";

/** The ids of the series a metric draws. The caller turns them into labels. */
export type UaiUsageSeriesId =
    | "succeeded"
    | "failed"
    | "input"
    | "cachedInput"
    | "output"
    | "successRate"
    | "averageDuration";

/**
 * How a metric is drawn: counts stack as bars, a rate is a line, and an average is plain bars (stacking would
 * add a "Total" of averages to the tooltip).
 */
export function usageChartKind(metric: UaiUsageMetric): "stacked-bar" | "bar" | "line" {
    switch (metric) {
        case "successRate":
            return "line";
        case "averageDuration":
            return "bar";
        default:
            return "stacked-bar";
    }
}

type Bucket = UsageOverviewPeriodModel["buckets"][number];

interface Stack {
    id: UaiUsageSeriesId;
    color: UauiSeries["color"];
    /** Undefined leaves the bucket out of the series: there is nothing to draw, which is not the same as 0. */
    of: (bucket: Bucket) => number | undefined;
}

// Cached input is null when no provider reported it. A period with no figure shows one plain input stack.
const cachedOf = (bucket: Bucket) => bucket.cachedInputTokens ?? 0;

const reportsCachedInput = (period?: UsageOverviewPeriodModel | null) => period?.totals.cachedInputTokens != null;

function stacksOf(metric: UaiUsageMetric, splitCachedInput: boolean): Stack[] {
    switch (metric) {
        case "requests":
            return [
                { id: "succeeded", color: 1, of: (b) => b.succeeded },
                { id: "failed", color: "danger", of: (b) => b.failed },
            ];
        case "inputTokens":
            return splitCachedInput
                ? [
                      { id: "input", color: 1, of: (b) => b.inputTokens - cachedOf(b) },
                      { id: "cachedInput", color: 3, of: cachedOf },
                  ]
                : [{ id: "input", color: 1, of: (b) => b.inputTokens }];
        case "outputTokens":
            return [{ id: "output", color: 2, of: (b) => b.outputTokens }];
        case "totalTokens":
            return [
                { id: "input", color: 1, of: (b) => b.inputTokens },
                { id: "output", color: 2, of: (b) => b.outputTokens },
            ];
        case "successRate":
            // A fraction, worked out as the tile does. A bucket with no requests has no rate: its point is left
            // out, which the chart reads as an empty slot (the range keeps every period on the axis and the line
            // breaks) rather than drawing 0.
            return [{ id: "successRate", color: 1, of: successRateOf }];
        case "averageDuration":
            // Seconds per request, as the tile. A bucket with no requests is left out, as for the success rate.
            return [{ id: "averageDuration", color: 1, of: averageDurationOf }];
    }
}

/**
 * One period's buckets as stacked series. An hourly response carries `hourlyBuckets` and any other `buckets`;
 * the server has already zero-filled them, so this only maps. A point's `date` is the bucket start as sent: an
 * ISO date, or for an hour a date-time with its local offset, which the chart reads as that instant.
 * `splitCachedInput` defaults to whether this period reports cached input; `mapUsageChart` sets it for both
 * periods so a compare line is like for like.
 */
export function mapUsageSeries(
    metric: UaiUsageMetric,
    period: UsageOverviewPeriodModel,
    labelOf: (id: UaiUsageSeriesId) => string = (id) => id,
    splitCachedInput: boolean = reportsCachedInput(period),
): UauiSeries[] {
    const buckets: Bucket[] = period.hourlyBuckets.length > 0 ? period.hourlyBuckets : period.buckets;

    return stacksOf(metric, splitCachedInput).map((stack) => ({
        id: stack.id,
        label: labelOf(stack.id),
        color: stack.color,
        points: buckets.flatMap((bucket) => {
            const value = stack.of(bucket);
            return value === undefined ? [] : [{ date: bucket.start, value }];
        }),
    }));
}

export interface UaiUsageChartData {
    series: UauiSeries[];
    /** Matched to `series` by id. The chart lines the periods up by bucket offset, so each keeps its own dates. */
    compareSeries: UauiSeries[];
}

/** The chart's current and compare series for a metric. No response gives no series. */
export function mapUsageChart(
    metric: UaiUsageMetric,
    response?: UsageOverviewResponseModel | null,
    labelOf?: (id: UaiUsageSeriesId) => string,
): UaiUsageChartData {
    if (!response) {
        return { series: [], compareSeries: [] };
    }

    // If either period reports cached input, both split it (a null counts as 0 cached), so the compare matches.
    const split = reportsCachedInput(response.current) || reportsCachedInput(response.previous);

    return {
        series: mapUsageSeries(metric, response.current, labelOf, split),
        compareSeries: response.previous ? mapUsageSeries(metric, response.previous, labelOf, split) : [],
    };
}

export interface UaiUsageChartMoment {
    /** The now line. Only at hour level, where it sits inside the day; at day or above the hatch already marks it. */
    now?: string;
    /** From here on the data is unfinished. Set whenever the range holds today. */
    incompleteFrom?: string;
}

/**
 * The chart's `now` and `incompleteFrom` for a range: the clock's instant, which the chart turns into the
 * browser's local time. `timeZone` only decides whether the range holds today, so it must be the browser's
 * zone (the default), because the chart buckets hours in the browser's zone.
 */
export function getUsageChartMoment(
    range: Pick<UauiDateRange, "from" | "to">,
    groupBy: UauiGroupBy,
    clock: Date,
    timeZone?: string | null,
): UaiUsageChartMoment {
    const today = usageLocalDate(clock, timeZone);

    if (today < range.from || today > range.to) {
        return {};
    }

    const moment = clock.toISOString();
    return { now: groupBy === "hour" ? moment : undefined, incompleteFrom: moment };
}

/** The keys of the breakdown table's columns, which is what the table raises in `sort-change`. */
export type UaiUsageBreakdownColumn = "name" | "requests" | "totalTokens" | "successRate" | "averageDurationMs";

/** One breakdown table row. */
export interface UaiUsageBreakdownRow extends Record<string, unknown> {
    /** The dimension's key: a provider alias, model id, profile id or user key. Use it as the table's `row-key`. */
    key: string;
    /** The latest name seen, else the key. The `unknown` and `anonymous` keys come through as names for the caller to localize. */
    name: string;
    requests: number;
    totalTokens: number;
    /** Undefined when no provider reported cached input for the row. */
    cachedInputTokens: number | undefined;
    /** A fraction: 0.97 is 97%. */
    successRate: number;
    /** Milliseconds per request. */
    averageDurationMs: number;
}

function rowOf(row: UsageBreakdownRowModel, counts: UsageCountsModel): UaiUsageBreakdownRow {
    return {
        key: row.key,
        name: row.name || row.key,
        requests: counts.requests,
        totalTokens: counts.totalTokens,
        cachedInputTokens: counts.cachedInputTokens ?? undefined,
        successRate: successRateOf(counts) ?? 0,
        averageDurationMs: counts.requests ? counts.durationMs / counts.requests : 0,
    };
}

/** What the breakdown table loads: a page of rows, and whether any usage in the range reported cached input. */
export type UaiUsageBreakdownResult = UauiTableServerResult & {
    rows: UaiUsageBreakdownRow[];
    compareRows: UaiUsageBreakdownRow[];
    /** From the totals across every row, so it does not change as the editor pages or searches. */
    reportsCachedInput: boolean;
};

/**
 * A page of breakdown rows for `UauiTableServerController`. The compare rows are the same keys in the
 * comparison period. `stats` carries each share column's total across every row before the search; the
 * largest value is left for the table to work out from the page.
 */
export function mapBreakdownResult(response: UsageBreakdownResponseModel): UaiUsageBreakdownResult {
    return {
        reportsCachedInput: response.totals.cachedInputTokens != null,
        rows: response.items.map((item) => rowOf(item, item.current)),
        compareRows: response.items.flatMap((item) => (item.previous ? [rowOf(item, item.previous)] : [])),
        totalRows: response.total,
        stats: {
            requests: { total: response.totals.requests },
            totalTokens: { total: response.totals.totalTokens },
        },
    };
}

const groupByModels: Record<UauiGroupBy, UsageGroupByModel> = {
    hour: "Hour",
    day: "Day",
    week: "Week",
    month: "Month",
};

/** The dimension toggle's values. */
export type UaiUsageBreakdownDimension = "provider" | "model" | "profile" | "user";

const dimensionModels: Record<UaiUsageBreakdownDimension, UsageBreakdownDimensionModel> = {
    provider: "Provider",
    model: "Model",
    profile: "Profile",
    user: "User",
};

const sortModels: Record<UaiUsageBreakdownColumn, UsageBreakdownSortModel> = {
    name: "Name",
    requests: "Requests",
    totalTokens: "TotalTokens",
    successRate: "SuccessRate",
    averageDurationMs: "AverageDuration",
};

interface RangeOptions {
    range: Pick<UauiDateRange, "from" | "to">;
    compare?: Pick<UauiCompare, "from" | "to">;
    /** The IANA zone the range's days are local to. Must be the browser's, as the chart buckets in it. Missing or unknown means the browser's. */
    timeZone?: string | null;
}

/** The query of `GET usage-overview`. `weekStart` is an ISO weekday, 1 (Monday) to 7 (Sunday). */
export function toUsageOverviewQuery(
    options: RangeOptions & { groupBy: UauiGroupBy; weekStart?: number },
): GetUsageOverviewData["query"] {
    return {
        from: options.range.from,
        to: options.range.to,
        compareFrom: options.compare?.from,
        compareTo: options.compare?.to,
        timeZone: resolveUsageTimeZone(options.timeZone),
        groupBy: groupByModels[options.groupBy],
        weekStart: options.weekStart,
    };
}

/** The query of `GET usage-breakdown` for the table's state. A sort on an unknown column is left to the endpoint's default. */
export function toUsageBreakdownQuery(
    options: RangeOptions & { dimension: UaiUsageBreakdownDimension; state: Readonly<UauiTableServerState> },
): GetUsageBreakdownData["query"] {
    const { state } = options;
    const sort = state.sort ? sortModels[state.sort.key as UaiUsageBreakdownColumn] : undefined;

    return {
        dimension: dimensionModels[options.dimension],
        from: options.range.from,
        to: options.range.to,
        compareFrom: options.compare?.from,
        compareTo: options.compare?.to,
        timeZone: resolveUsageTimeZone(options.timeZone),
        sort,
        sortDirection: sort ? (state.sort?.direction === "asc" ? "Ascending" : "Descending") : undefined,
        query: state.query || undefined,
        skip: (state.page - 1) * state.pageSize,
        take: state.pageSize,
    };
}
