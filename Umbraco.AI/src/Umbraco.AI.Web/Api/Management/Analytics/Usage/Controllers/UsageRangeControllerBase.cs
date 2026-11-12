using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Analytics.Controllers;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Controllers;

/// <summary>
/// Base for the usage endpoints that take a local-day range, an optional comparison range and a time zone.
/// </summary>
public abstract class UsageRangeControllerBase : AnalyticsControllerBase
{
    /// <summary>
    /// The longest range, in days, an endpoint accepts, so one request can't scan years of rows.
    /// </summary>
    protected const int MaxRangeDays = 5 * 366;

    /// <summary>
    /// Checks the ranges and the time zone, returning a 400 problem to send back when they can't be used.
    /// </summary>
    /// <param name="from">First day of the range.</param>
    /// <param name="to">Last day of the range (inclusive).</param>
    /// <param name="compareFrom">First day of the comparison range.</param>
    /// <param name="compareTo">Last day of the comparison range (inclusive).</param>
    /// <param name="timeZoneId">An IANA time zone name, or null for UTC.</param>
    /// <param name="timeZone">The resolved time zone when the result is null.</param>
    /// <returns>The problem to return, or null when the request is valid.</returns>
    protected ActionResult? ValidateRanges(
        DateOnly from,
        DateOnly to,
        DateOnly? compareFrom,
        DateOnly? compareTo,
        string? timeZoneId,
        out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;

        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out timeZone!))
        {
            timeZone = TimeZoneInfo.Utc;
            return BadRequest(CreateProblemDetails(
                "Invalid Time Zone",
                $"'{timeZoneId}' is not a known IANA time zone name, e.g. 'Europe/Copenhagen'."));
        }

        if (to < from)
        {
            return BadRequest(CreateProblemDetails("Invalid Date Range", "'to' must be on or after 'from'."));
        }

        if (compareFrom.HasValue != compareTo.HasValue)
        {
            return BadRequest(CreateProblemDetails(
                "Invalid Date Range",
                "Send both 'compareFrom' and 'compareTo', or neither."));
        }

        if (compareFrom.HasValue && compareTo!.Value < compareFrom.Value)
        {
            return BadRequest(CreateProblemDetails("Invalid Date Range", "'compareTo' must be on or after 'compareFrom'."));
        }

        if (DaysIn(from, to) > MaxRangeDays || (compareFrom.HasValue && DaysIn(compareFrom.Value, compareTo!.Value) > MaxRangeDays))
        {
            return BadRequest(CreateProblemDetails(
                "Invalid Date Range",
                $"A range can be at most {MaxRangeDays} days."));
        }

        return null;
    }

    /// <summary>
    /// The number of days in a range, both ends included.
    /// </summary>
    protected static int DaysIn(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber + 1;
}
