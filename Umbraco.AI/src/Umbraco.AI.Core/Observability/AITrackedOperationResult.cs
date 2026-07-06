using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Internal outcome wrapper returned by a tracked operation, carrying everything the tracker needs
/// to record analytics and audit entries. Analytics usage and audit-response usage are separate
/// because they diverge for image generation (analytics records usage; audit response does not).
/// </summary>
internal sealed class AITrackedOperationResult<TResult>
{
    public required TResult Result { get; init; }

    /// <summary>Usage recorded to analytics (nullable; STT has none).</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The fully-formed audit-complete payload (Data + optional Usage); null skips nothing but writes null data.</summary>
    public AIAuditResponse? AuditResponse { get; init; }
}
