using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;

/// <summary>
/// Response model for the usage overview: a range and, when asked for, its comparison range.
/// </summary>
public class UsageOverviewResponseModel
{
    /// <summary>
    /// Gets or sets the requested range.
    /// </summary>
    [Required]
    public UsageOverviewPeriodModel Current { get; set; } = new();

    /// <summary>
    /// Gets or sets the comparison range. Null when no comparison range was sent.
    /// </summary>
    public UsageOverviewPeriodModel? Previous { get; set; }
}

/// <summary>
/// The usage of one range: totals and a bucket for every day, week or month of it.
/// </summary>
public class UsageOverviewPeriodModel
{
    /// <summary>
    /// Gets or sets the first local day of the range.
    /// </summary>
    [Required]
    public DateOnly From { get; set; }

    /// <summary>
    /// Gets or sets the last local day of the range (inclusive).
    /// </summary>
    [Required]
    public DateOnly To { get; set; }

    /// <summary>
    /// Gets or sets the counts for the whole range.
    /// </summary>
    [Required]
    public UsageCountsModel Totals { get; set; } = new();

    /// <summary>
    /// Gets or sets every day, week or month bucket of the range in order, with buckets without usage as
    /// zeros. Empty when grouped by hour.
    /// </summary>
    [Required]
    public IEnumerable<UsageOverviewBucketModel> Buckets { get; set; } = [];

    /// <summary>
    /// Gets or sets every hour of the range in order, with hours without usage as zeros. Empty unless
    /// grouped by hour.
    /// </summary>
    [Required]
    public IEnumerable<UsageOverviewHourlyBucketModel> HourlyBuckets { get; set; } = [];
}

/// <summary>
/// The counts for one time series bucket.
/// </summary>
public class UsageOverviewBucketModel : UsageCountsModel
{
    /// <summary>
    /// Gets or sets the local day the bucket starts on: the day itself, the first day of its week or the
    /// first of its month. For the first week or month it can be before the range's own start.
    /// </summary>
    [Required]
    public DateOnly Start { get; set; }
}

/// <summary>
/// The counts for one hour.
/// </summary>
public class UsageOverviewHourlyBucketModel : UsageCountsModel
{
    /// <summary>
    /// Gets or sets the instant the hour starts, written in the requested time zone's offset at that
    /// instant, so the wall-clock hour and the instant are both unambiguous. A day that gains or loses an
    /// hour on a clock change has 25 or 23 of these.
    /// </summary>
    [Required]
    public DateTimeOffset Start { get; set; }
}
