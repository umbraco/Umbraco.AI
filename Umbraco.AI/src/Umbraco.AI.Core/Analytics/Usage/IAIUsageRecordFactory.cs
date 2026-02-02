namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Factory for creating AIUsageRecord instances with proper dependency injection.
/// </summary>
internal interface IAIUsageRecordFactory
{
    /// <summary>
    /// Creates a new AIUsageRecord instance from the given context and result.
    /// </summary>
    /// <param name="context">The usage context containing operation metadata.</param>
    /// <param name="result">The operation result containing metrics and status.</param>
    /// <returns>A new AIUsageRecord instance ready to be persisted.</returns>
    AIUsageRecord Create(
        AIUsageRecordContext context,
        AIUsageRecordResult result);
}
