export interface UaiAnalyticsQueryParams {
    from: string;
    to: string;
    granularity?: 'Hourly' | 'Daily';
}