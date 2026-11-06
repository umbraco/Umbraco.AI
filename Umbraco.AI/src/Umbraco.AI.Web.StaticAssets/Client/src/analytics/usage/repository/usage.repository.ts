import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AnalyticsService } from "../../../api/sdk.gen.js";
import type {
    GetUsageBreakdownData,
    GetUsageOverviewData,
    UsageBreakdownResponseModel,
    UsageOverviewResponseModel,
} from "../../../api/types.gen.js";

/** Loads the usage dashboard's two endpoints. A failure comes back as `error`, never thrown. */
export class UaiUsageRepository {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getOverview(
        query: GetUsageOverviewData["query"],
        signal?: AbortSignal,
    ): Promise<{ data?: UsageOverviewResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, AnalyticsService.getUsageOverview({ query, signal }));
        return error || !data ? { error: error ?? new Error("No usage overview returned") } : { data };
    }

    async getBreakdown(
        query: GetUsageBreakdownData["query"],
        signal?: AbortSignal,
    ): Promise<{ data?: UsageBreakdownResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, AnalyticsService.getUsageBreakdown({ query, signal }));
        return error || !data ? { error: error ?? new Error("No usage breakdown returned") } : { data };
    }
}
