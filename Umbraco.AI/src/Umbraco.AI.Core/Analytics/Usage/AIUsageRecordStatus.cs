namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// How an AI operation recorded in usage analytics ended. Stored by name, so values must not be renamed.
/// </summary>
public enum AIUsageRecordStatus
{
    /// <summary>The operation completed.</summary>
    Succeeded,

    /// <summary>The operation threw, or ended on a provider error.</summary>
    Failed,

    /// <summary>A guardrail stopped the operation. Counted as a failure in totals.</summary>
    Blocked,
}
