import {
    AnalyticsService,
    UsageBreakdownItemModel,
    UsageSummaryResponseModel,
    UsageTimeSeriesPointModel
} from "../../../api";
import { UaiAnalyticsQueryParams } from "../../types.js";

export class UaiAnalyticsUsageRepository {
    async getSummary(params: UaiAnalyticsQueryParams): Promise<UsageSummaryResponseModel> {
        const { data } = await AnalyticsService.getUsageSummary({
            query: params
        });
        return data!;
    }

    async getTimeSeries(params: UaiAnalyticsQueryParams): Promise<UsageTimeSeriesPointModel[]> {
        const { data } = await AnalyticsService.getUsageTimeSeries({
            query: params
        });
        return data!;
    }

    async getBreakdownByProvider(params: UaiAnalyticsQueryParams): Promise<UsageBreakdownItemModel[]> {
        const { data } = await AnalyticsService.getUsageBreakdownByProvider({
            query: params
        });
        return data!;
    }

    async getBreakdownByModel(params: UaiAnalyticsQueryParams): Promise<UsageBreakdownItemModel[]> {
        const { data } = await AnalyticsService.getUsageBreakdownByModel({
            query: params
        });
        return data!;
    }

    async getBreakdownByProfile(params: UaiAnalyticsQueryParams): Promise<UsageBreakdownItemModel[]> {
        const { data } = await AnalyticsService.getUsageBreakdownByProfile({
            query: params
        });
        return data!;
    }

    async getBreakdownByUser(params: UaiAnalyticsQueryParams): Promise<UsageBreakdownItemModel[]> {
        const { data } = await AnalyticsService.getUsageBreakdownByUser({
            query: params
        });
        return data!;
    }
}

export const usageRepository = new UaiAnalyticsUsageRepository();
