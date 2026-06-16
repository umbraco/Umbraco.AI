namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Configuration options for Umbraco.AI usage telemetry — the anonymous, aggregate usage
/// information reported to Umbraco HQ via the CMS telemetry pipeline.
/// </summary>
/// <remarks>
/// Not to be confused with <see cref="AITelemetry"/>, which holds constants for OpenTelemetry
/// tracing/metrics emitted to the host application's own observability infrastructure.
/// Usage telemetry only ships when the CMS telemetry level is set to <c>Detailed</c>
/// AND <see cref="Enabled"/> is <c>true</c>.
/// </remarks>
public sealed class AIUsageTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether Umbraco.AI contributes usage telemetry to the CMS telemetry report.
    /// When false, no Umbraco.AI data is included regardless of the CMS telemetry level.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
