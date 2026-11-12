import { afterEach, describe, expect, it, vi } from "vitest";
import type {
    UsageBreakdownResponseModel,
    UsageCountsModel,
    UsageOverviewPeriodModel,
    UsageOverviewResponseModel,
} from "../../../api/types.gen.js";
import {
    getUsageChartMoment,
    mapBreakdownResult,
    mapUsageCachedInput,
    mapUsageChart,
    mapUsageSeries,
    mapUsageTiles,
    toUsageBreakdownQuery,
    usageChartKind,
    toUsageOverviewQuery,
} from "./usage-adapter.js";
import { resolveUsageTimeZone, usageLocalDate } from "./usage-time-zone.js";

const counts = (overrides: Partial<UsageCountsModel> = {}): UsageCountsModel => ({
    requests: 0,
    nestedRequests: 0,
    succeeded: 0,
    failed: 0,
    inputTokens: 0,
    cachedInputTokens: null,
    outputTokens: 0,
    totalTokens: 0,
    durationMs: 0,
    ...overrides,
});

const bucket = (start: string, overrides: Partial<UsageCountsModel> = {}) => ({ start, ...counts(overrides) });

const period = (overrides: Partial<UsageOverviewPeriodModel> = {}): UsageOverviewPeriodModel => ({
    from: "2026-11-05",
    to: "2026-11-05",
    totals: counts(),
    buckets: [],
    hourlyBuckets: [],
    ...overrides,
});

/** A whole local day of hourly buckets in Copenhagen, one request in the given hours. */
const hoursOf = (date: string, busy: Record<number, number> = {}) =>
    Array.from({ length: 24 }, (_, hour) =>
        bucket(`${date}T${String(hour).padStart(2, "0")}:00:00+01:00`, {
            requests: busy[hour] ?? 0,
            succeeded: busy[hour] ?? 0,
        }),
    );

const dates = (series: { points: { date: string }[] }) => series.points.map((p) => p.date);
const values = (series: { points: { value: number }[] }) => series.points.map((p) => p.value);

describe("usage tiles", () => {
    it("has no data for any tile without a response", () => {
        const tiles = mapUsageTiles(undefined);

        expect(Object.values(tiles).every((t) => t.noData && t.value === undefined)).toBe(true);
    });

    it("has no data, and no rate or average, for a period without requests", () => {
        const tiles = mapUsageTiles({ current: period(), previous: null });

        expect(tiles.requests).toEqual({ value: 0, previous: undefined, noData: true });
        expect(tiles.successRate).toEqual({ value: undefined, previous: undefined, noData: true });
        expect(tiles.averageDuration.value).toBeUndefined();
    });

    it("gives the period's cached input only when a provider reported it", () => {
        const reported = { current: period({ totals: counts({ requests: 4, cachedInputTokens: 30 }) }) };
        const zero = { current: period({ totals: counts({ requests: 4, cachedInputTokens: 0 }) }) };
        const none = { current: period({ totals: counts({ requests: 4 }) }) };

        expect(mapUsageCachedInput(reported)).toBe(30);
        expect(mapUsageCachedInput(zero)).toBe(0);
        expect(mapUsageCachedInput(none)).toBeUndefined();
        expect(mapUsageCachedInput(undefined)).toBeUndefined();
    });

    it("works out the success rate as a fraction and the average duration in seconds", () => {
        const response: UsageOverviewResponseModel = {
            current: period({
                totals: counts({
                    requests: 10,
                    succeeded: 9,
                    failed: 1,
                    durationMs: 25_000,
                    inputTokens: 100,
                    outputTokens: 40,
                    totalTokens: 140,
                }),
            }),
        };

        const tiles = mapUsageTiles(response);

        expect(tiles.successRate.value).toBeCloseTo(0.9);
        expect(tiles.averageDuration.value).toBe(2.5);
        expect(tiles.totalTokens.value).toBe(140);
        expect(tiles.requests.previous).toBeUndefined();
    });

    it("carries the compare period's values, leaving a rate with no requests behind it unknown", () => {
        const response: UsageOverviewResponseModel = {
            current: period({ totals: counts({ requests: 10, succeeded: 8, durationMs: 10_000 }) }),
            previous: period({ totals: counts({ requests: 5, succeeded: 5, durationMs: 5_000 }) }),
        };
        const empty: UsageOverviewResponseModel = { current: response.current, previous: period() };

        expect(mapUsageTiles(response).requests.previous).toBe(5);
        expect(mapUsageTiles(response).successRate.previous).toBe(1);
        expect(mapUsageTiles(empty).requests.previous).toBe(0);
        expect(mapUsageTiles(empty).successRate.previous).toBeUndefined();
    });
});

describe("usage series", () => {
    it("maps hourly buckets to points dated by their local start, one per hour", () => {
        const hourly = period({ hourlyBuckets: hoursOf("2026-11-05", { 9: 3, 13: 5 }) });

        const [succeeded] = mapUsageSeries("requests", hourly);

        expect(succeeded.points).toHaveLength(24);
        expect(succeeded.points[0].date).toBe("2026-11-05T00:00:00+01:00");
        expect(succeeded.points[13]).toEqual({ date: "2026-11-05T13:00:00+01:00", value: 5 });
    });

    it("keeps the zero buckets so a quiet hour is a point and not a gap", () => {
        const [succeeded] = mapUsageSeries("requests", period({ hourlyBuckets: hoursOf("2026-11-05", { 9: 3 }) }));

        expect(values(succeeded).filter((v) => v === 0)).toHaveLength(23);
    });

    it("maps yesterday the same way, on its own date", () => {
        const [succeeded] = mapUsageSeries("requests", period({ hourlyBuckets: hoursOf("2026-11-04", { 1: 1 }) }));

        expect(dates(succeeded)[0]).toBe("2026-11-04T00:00:00+01:00");
    });

    it("keeps both days of a two-day hourly range, in order", () => {
        const hourly = period({ hourlyBuckets: [...hoursOf("2026-11-04"), ...hoursOf("2026-11-05")] });

        const [succeeded] = mapUsageSeries("requests", hourly);

        expect(succeeded.points).toHaveLength(48);
        expect(succeeded.points[24].date).toBe("2026-11-05T00:00:00+01:00");
    });

    it("keeps both of the repeated hour when the clocks go back, each at its own offset", () => {
        const fallBack = [
            bucket("2026-10-25T01:00:00+02:00"),
            bucket("2026-10-25T02:00:00+02:00"),
            bucket("2026-10-25T02:00:00+01:00"),
        ];

        const [succeeded] = mapUsageSeries("requests", period({ hourlyBuckets: fallBack }));

        expect(dates(succeeded).slice(1)).toEqual(["2026-10-25T02:00:00+02:00", "2026-10-25T02:00:00+01:00"]);
    });

    it.each([
        ["day", ["2026-11-02", "2026-11-03", "2026-11-04"]],
        ["week", ["2026-10-26", "2026-11-02"]],
        ["month", ["2026-10-01", "2026-11-01"]],
    ])("maps %s buckets to points dated by the bucket's start date", (_, starts) => {
        const daily = period({ buckets: starts.map((start, i) => bucket(start, { requests: i, succeeded: i })) });

        const [succeeded] = mapUsageSeries("requests", daily);

        expect(dates(succeeded)).toEqual(starts);
        expect(values(succeeded)).toEqual(starts.map((_, i) => i));
    });

    it("stacks requests as succeeded and failed, failed in the danger colour", () => {
        const series = mapUsageSeries(
            "requests",
            period({ buckets: [bucket("2026-11-05", { requests: 4, succeeded: 3, failed: 1 })] }),
        );

        expect(series.map((s) => [s.id, s.color, values(s)])).toEqual([
            ["succeeded", 1, [3]],
            ["failed", "danger", [1]],
        ]);
    });

    it("splits input into uncached and cached when a provider reported a cached figure", () => {
        const input = period({
            totals: counts({ cachedInputTokens: 30 }),
            buckets: [
                bucket("2026-11-05", { inputTokens: 100, cachedInputTokens: 30 }),
                bucket("2026-11-06", { inputTokens: 50, cachedInputTokens: null }),
            ],
        });

        const series = mapUsageSeries("inputTokens", input);

        expect(series.map((s) => [s.id, values(s)])).toEqual([
            ["input", [70, 50]],
            ["cachedInput", [30, 0]],
        ]);
    });

    it("draws a single input stack when nothing reported a cached figure", () => {
        const series = mapUsageSeries("inputTokens", period({ buckets: [bucket("2026-11-05", { inputTokens: 100 })] }));

        expect(series.map((s) => [s.id, values(s)])).toEqual([["input", [100]]]);
    });

    it("stacks total tokens as input and output", () => {
        const series = mapUsageSeries(
            "totalTokens",
            period({ buckets: [bucket("2026-11-05", { inputTokens: 10, outputTokens: 4 })] }),
        );

        expect(series.map((s) => [s.id, values(s)])).toEqual([
            ["input", [10]],
            ["output", [4]],
        ]);
    });

    it("asks the caller for each label", () => {
        const [succeeded] = mapUsageSeries("requests", period(), (id) => `label:${id}`);

        expect(succeeded.label).toBe("label:succeeded");
    });
});

describe("usage rate and average metrics", () => {
    const days = period({
        buckets: [
            bucket("2026-11-02", { requests: 4, succeeded: 3, failed: 1, durationMs: 8_000 }),
            bucket("2026-11-03"),
            bucket("2026-11-04", { requests: 2, succeeded: 2, durationMs: 1_000 }),
        ],
    });

    it("draws the success rate as a line of fractions", () => {
        const [series, ...rest] = mapUsageSeries("successRate", days);

        expect(rest).toEqual([]);
        expect(usageChartKind("successRate")).toBe("line");
        expect(series.id).toBe("successRate");
        expect(values(series)).toEqual([0.75, 1]);
    });

    it("leaves a bucket with no requests out of the success rate rather than drawing 0", () => {
        const [series] = mapUsageSeries("successRate", days);

        expect(dates(series)).toEqual(["2026-11-02", "2026-11-04"]);
    });

    it("draws the average duration as bars of seconds per request, with no point for a bucket without requests", () => {
        const [series] = mapUsageSeries("averageDuration", days);

        expect(usageChartKind("averageDuration")).toBe("bar");
        expect(dates(series)).toEqual(["2026-11-02", "2026-11-04"]);
        expect(values(series)).toEqual([2, 0.5]);
    });

    it("applies the same rules to the compare period, keeping its own dates", () => {
        const response: UsageOverviewResponseModel = {
            current: days,
            previous: period({
                buckets: [
                    bucket("2026-10-30", { requests: 2, succeeded: 1, failed: 1, durationMs: 4_000 }),
                    bucket("2026-10-31"),
                    bucket("2026-11-01"),
                ],
            }),
        };

        const rate = mapUsageChart("successRate", response);
        const duration = mapUsageChart("averageDuration", response);

        expect(values(rate.compareSeries[0])).toEqual([0.5]);
        expect(dates(rate.compareSeries[0])).toEqual(["2026-10-30"]);
        expect(values(duration.compareSeries[0])).toEqual([2]);
        expect(rate.compareSeries[0].id).toBe(rate.series[0].id);
    });

    it("maps hourly buckets, leaving the quiet hours out", () => {
        const hourly = period({ hourlyBuckets: hoursOf("2026-11-05", { 9: 4, 13: 2 }) });

        const [rate] = mapUsageSeries("successRate", hourly);
        const [duration] = mapUsageSeries("averageDuration", hourly);

        expect(dates(rate)).toEqual(["2026-11-05T09:00:00+01:00", "2026-11-05T13:00:00+01:00"]);
        expect(values(rate)).toEqual([1, 1]);
        expect(values(duration)).toEqual([0, 0]);
    });

    it("localizes the new series through the label callback", () => {
        const [series] = mapUsageSeries("successRate", days, (id) => `label:${id}`);

        expect(series.label).toBe("label:successRate");
    });
});

describe("usage chart", () => {
    it("has no series without a response", () => {
        expect(mapUsageChart("requests", undefined)).toEqual({ series: [], compareSeries: [] });
    });

    it("has empty points, not missing series, for an empty response", () => {
        const chart = mapUsageChart("requests", { current: period(), previous: null });

        expect(chart.series.map((s) => s.points)).toEqual([[], []]);
        expect(chart.compareSeries).toEqual([]);
    });

    it("keeps each period's own dates, matched by series id", () => {
        const response: UsageOverviewResponseModel = {
            current: period({ buckets: [bucket("2026-11-05", { requests: 2, succeeded: 2 }), bucket("2026-11-06")] }),
            previous: period({ buckets: [bucket("2026-11-03", { requests: 1, succeeded: 1 }), bucket("2026-11-04")] }),
        };

        const { series, compareSeries } = mapUsageChart("requests", response);

        expect(compareSeries.map((s) => s.id)).toEqual(series.map((s) => s.id));
        expect(dates(series[0])).toEqual(["2026-11-05", "2026-11-06"]);
        expect(dates(compareSeries[0])).toEqual(["2026-11-03", "2026-11-04"]);
        expect(compareSeries[0].points).toHaveLength(series[0].points.length);
    });

    it("splits input both sides when only one period reports a cached figure", () => {
        const response: UsageOverviewResponseModel = {
            current: period({ buckets: [bucket("2026-11-05", { inputTokens: 100 })] }),
            previous: period({
                totals: counts({ cachedInputTokens: 10 }),
                buckets: [bucket("2026-11-04", { inputTokens: 40, cachedInputTokens: 10 })],
            }),
        };

        const { series, compareSeries } = mapUsageChart("inputTokens", response);

        expect(series.map((s) => [s.id, values(s)])).toEqual([
            ["input", [100]],
            ["cachedInput", [0]],
        ]);
        expect(compareSeries.map((s) => [s.id, values(s)])).toEqual([
            ["input", [30]],
            ["cachedInput", [10]],
        ]);
    });

    it("compares today by hour against yesterday by hour", () => {
        const response: UsageOverviewResponseModel = {
            current: period({ hourlyBuckets: hoursOf("2026-11-05", { 9: 2 }) }),
            previous: period({ hourlyBuckets: hoursOf("2026-11-04", { 9: 1 }) }),
        };

        const { series, compareSeries } = mapUsageChart("requests", response);

        expect(series[0].points).toHaveLength(24);
        expect(compareSeries[0].points[9]).toEqual({ date: "2026-11-04T09:00:00+01:00", value: 1 });
    });
});

describe("usage chart moment", () => {
    // 13:23 UTC is 14:23 in Copenhagen in November.
    const clock = new Date("2026-11-05T13:23:00Z");

    afterEach(() => vi.restoreAllMocks());

    const browserZone = (timeZone: string) =>
        vi.spyOn(Intl.DateTimeFormat.prototype, "resolvedOptions").mockReturnValue({ timeZone } as never);

    it("marks now and the unfinished part with the clock's instant, which the chart reads in local time", () => {
        expect(
            getUsageChartMoment({ from: "2026-11-04", to: "2026-11-05" }, "hour", clock, "Europe/Copenhagen"),
        ).toEqual({
            now: "2026-11-05T13:23:00.000Z",
            incompleteFrom: "2026-11-05T13:23:00.000Z",
        });
    });

    it("only marks the unfinished part above hour level", () => {
        expect(
            getUsageChartMoment({ from: "2026-11-01", to: "2026-11-05" }, "day", clock, "Europe/Copenhagen"),
        ).toEqual({
            now: undefined,
            incompleteFrom: "2026-11-05T13:23:00.000Z",
        });
    });

    it("marks nothing when the range is over", () => {
        expect(
            getUsageChartMoment({ from: "2026-11-03", to: "2026-11-04" }, "hour", clock, "Europe/Copenhagen"),
        ).toEqual({});
    });

    it("reads today in the given zone, not the clock's date in UTC", () => {
        const lateEvening = new Date("2026-11-05T23:30:00Z");
        const tomorrow = { from: "2026-11-06", to: "2026-11-06" };

        expect(getUsageChartMoment(tomorrow, "hour", lateEvening, "Europe/Copenhagen").now).toBe(
            "2026-11-05T23:30:00.000Z",
        );
        expect(getUsageChartMoment(tomorrow, "hour", lateEvening, "UTC")).toEqual({});
    });

    it("reads today in the browser's zone when the zone is missing", () => {
        browserZone("Asia/Tokyo");
        const lateEvening = new Date("2026-11-05T23:30:00Z");

        // 23:30 UTC is already 6 November in Tokyo.
        expect(getUsageChartMoment({ from: "2026-11-06", to: "2026-11-06" }, "hour", lateEvening, undefined).now).toBe(
            "2026-11-05T23:30:00.000Z",
        );
        expect(getUsageChartMoment({ from: "2026-11-05", to: "2026-11-05" }, "hour", lateEvening, null)).toEqual({});
    });
});

describe("usage time zone", () => {
    afterEach(() => vi.restoreAllMocks());

    it("keeps a real zone", () => {
        expect(resolveUsageTimeZone("Europe/Copenhagen")).toBe("Europe/Copenhagen");
    });

    it("falls back to the browser's zone for a missing or unknown zone", () => {
        vi.spyOn(Intl.DateTimeFormat.prototype, "resolvedOptions").mockReturnValue({
            timeZone: "Asia/Tokyo",
        } as never);

        expect(resolveUsageTimeZone(undefined)).toBe("Asia/Tokyo");
        expect(resolveUsageTimeZone("")).toBe("Asia/Tokyo");
        expect(resolveUsageTimeZone("Not/AZone")).toBe("Asia/Tokyo");
    });

    it("falls back to UTC only when the browser's zone can't be resolved", () => {
        vi.spyOn(Intl.DateTimeFormat.prototype, "resolvedOptions").mockReturnValue({ timeZone: undefined } as never);

        expect(resolveUsageTimeZone(undefined)).toBe("UTC");
    });

    it("works out the local date of an instant in a zone", () => {
        const instant = new Date("2026-11-05T23:05:00Z");

        expect(usageLocalDate(instant, "Europe/Copenhagen")).toBe("2026-11-06");
        expect(usageLocalDate(instant, "America/New_York")).toBe("2026-11-05");
    });
});

describe("usage breakdown", () => {
    const response: UsageBreakdownResponseModel = {
        total: 7,
        totals: counts({ requests: 100, totalTokens: 5000 }),
        previousTotals: counts({ requests: 80 }),
        items: [
            {
                key: "gpt-4o",
                name: null,
                current: counts({ requests: 10, succeeded: 9, totalTokens: 400, durationMs: 20_000 }),
                previous: counts({ requests: 8, succeeded: 8, totalTokens: 300, durationMs: 8_000 }),
            },
            { key: "p1", name: "Content assistant", current: counts(), previous: counts() },
        ],
    };

    it("maps rows with the name falling back to the key, a success fraction and average milliseconds", () => {
        const { rows } = mapBreakdownResult(response);

        expect(rows[0]).toEqual({
            key: "gpt-4o",
            name: "gpt-4o",
            requests: 10,
            totalTokens: 400,
            successRate: 0.9,
            averageDurationMs: 2000,
        });
        expect(rows[1].name).toBe("Content assistant");
    });

    it("carries cached input tokens on a row and its compare row, and none when not reported", () => {
        const withCache: UsageBreakdownResponseModel = {
            ...response,
            items: [
                {
                    ...response.items[0],
                    current: counts({ requests: 10, cachedInputTokens: 120 }),
                    previous: counts({ requests: 8, cachedInputTokens: 0 }),
                },
                { ...response.items[1], current: counts({ requests: 1 }), previous: null },
            ],
        };

        const { rows, compareRows } = mapBreakdownResult(withCache);

        expect(rows[0].cachedInputTokens).toBe(120);
        expect(compareRows[0].cachedInputTokens).toBe(0);
        expect(rows[1].cachedInputTokens).toBeUndefined();
    });

    it("says whether cached input was reported from the totals, not from the page's rows", () => {
        const notReported = mapBreakdownResult({ ...response, totals: counts({ requests: 5 }) });
        const reported = mapBreakdownResult({
            ...response,
            totals: counts({ requests: 5, cachedInputTokens: 0 }),
            items: [],
        });

        expect(notReported.reportsCachedInput).toBe(false);
        expect(reported.reportsCachedInput).toBe(true);
    });

    it("gives a row with no requests a zero rate and average", () => {
        const { rows } = mapBreakdownResult(response);

        expect(rows[1]).toMatchObject({ successRate: 0, averageDurationMs: 0 });
    });

    it("maps the compare period's values onto the same keys", () => {
        const { compareRows } = mapBreakdownResult(response);

        expect(compareRows[0]).toEqual({
            key: "gpt-4o",
            name: "gpt-4o",
            requests: 8,
            totalTokens: 300,
            successRate: 1,
            averageDurationMs: 1000,
        });
        expect(compareRows.map((r) => r.key)).toEqual(["gpt-4o", "p1"]);
    });

    it("has no compare rows when no period was compared", () => {
        const { compareRows } = mapBreakdownResult({
            ...response,
            items: response.items.map((i) => ({ ...i, previous: null })),
        });

        expect(compareRows).toEqual([]);
    });

    it("reports the match count and the totals the shares are a part of", () => {
        const result = mapBreakdownResult(response);

        expect(result.totalRows).toBe(7);
        expect(result.stats).toEqual({ requests: { total: 100 }, totalTokens: { total: 5000 } });
    });

    it("maps an empty page", () => {
        const result = mapBreakdownResult({ total: 0, items: [], totals: counts() });

        expect(result.rows).toEqual([]);
        expect(result.totalRows).toBe(0);
    });
});

describe("usage requests", () => {
    afterEach(() => vi.restoreAllMocks());

    it("builds the overview query with the compare range, group by and the browser zone for an unknown one", () => {
        vi.spyOn(Intl.DateTimeFormat.prototype, "resolvedOptions").mockReturnValue({ timeZone: "Asia/Tokyo" } as never);

        expect(
            toUsageOverviewQuery({
                range: { from: "2026-11-05", to: "2026-11-05" },
                compare: { from: "2026-11-04", to: "2026-11-04" },
                groupBy: "hour",
                timeZone: "Not/AZone",
            }),
        ).toEqual({
            from: "2026-11-05",
            to: "2026-11-05",
            compareFrom: "2026-11-04",
            compareTo: "2026-11-04",
            timeZone: "Asia/Tokyo",
            groupBy: "Hour",
            weekStart: undefined,
        });
    });

    it("builds the breakdown query from the table's state", () => {
        const query = toUsageBreakdownQuery({
            range: { from: "2026-11-01", to: "2026-11-05" },
            dimension: "profile",
            timeZone: "Europe/Copenhagen",
            state: { sort: { key: "averageDurationMs", direction: "asc" }, page: 3, pageSize: 5, query: "chat" },
        });

        expect(query).toMatchObject({
            dimension: "Profile",
            sort: "AverageDuration",
            sortDirection: "Ascending",
            query: "chat",
            skip: 10,
            take: 5,
            compareFrom: undefined,
            timeZone: "Europe/Copenhagen",
        });
    });

    it("leaves the sort to the endpoint when the table has none, and drops an empty search", () => {
        const query = toUsageBreakdownQuery({
            range: { from: "2026-11-01", to: "2026-11-05" },
            dimension: "user",
            state: { page: 1, pageSize: 10, query: "" },
        });

        expect(query).toMatchObject({
            dimension: "User",
            sort: undefined,
            sortDirection: undefined,
            query: undefined,
            skip: 0,
            take: 10,
        });
    });
});
