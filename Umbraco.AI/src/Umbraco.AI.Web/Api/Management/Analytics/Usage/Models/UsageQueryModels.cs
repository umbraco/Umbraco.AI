namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// What usage rows are broken down by.
/// </summary>
public enum UsageBreakdownDimensionModel
{
    /// <summary>By provider.</summary>
    Provider,

    /// <summary>By model.</summary>
    Model,

    /// <summary>By profile.</summary>
    Profile,

    /// <summary>By user.</summary>
    User
}

/// <summary>
/// What usage rows are sorted by.
/// </summary>
public enum UsageBreakdownSortModel
{
    /// <summary>By name (the key when a row has none).</summary>
    Name,

    /// <summary>By number of requests.</summary>
    Requests,

    /// <summary>By total tokens.</summary>
    TotalTokens,

    /// <summary>By the share of requests that succeeded.</summary>
    SuccessRate,

    /// <summary>By average request duration.</summary>
    AverageDuration
}

/// <summary>
/// The direction of a sort.
/// </summary>
public enum UsageSortDirectionModel
{
    /// <summary>Lowest (or first) first.</summary>
    Ascending,

    /// <summary>Highest (or last) first.</summary>
    Descending
}

/// <summary>
/// The size of a time series bucket.
/// </summary>
public enum UsageGroupByModel
{
    /// <summary>One bucket per hour, in <c>hourlyBuckets</c>. For ranges of up to 2 local days.</summary>
    Hour,

    /// <summary>One bucket per local day.</summary>
    Day,

    /// <summary>One bucket per local week.</summary>
    Week,

    /// <summary>One bucket per local calendar month.</summary>
    Month
}
