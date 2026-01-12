using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiTraceService"/> to support TraceIdentifier lookups.
/// </summary>
internal static class AiTraceServiceExtensions
{
    /// <summary>
    /// Tries to get a trace ID by local ID or OpenTelemetry TraceId.
    /// If already a local ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The trace service.</param>
    /// <param name="identifier">The local ID or OpenTelemetry TraceId to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trace ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetTraceIdAsync(
        this IAiTraceService service,
        TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        // If it's already a local ID, return it directly (no DB lookup needed)
        if (identifier is { IsLocalId: true, LocalId: not null })
        {
            return identifier.LocalId.Value;
        }

        // For OpenTelemetry TraceId, we need to look up the trace
        if (identifier.OTelTraceId != null)
        {
            var trace = await service.GetTraceByTraceIdAsync(identifier.OTelTraceId, cancellationToken);
            return trace?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a trace ID by local ID or OpenTelemetry TraceId, throwing if not found.
    /// </summary>
    /// <param name="service">The trace service.</param>
    /// <param name="identifier">The local ID or OpenTelemetry TraceId to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trace ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the trace is not found.</exception>
    public static async Task<Guid> GetTraceIdAsync(
        this IAiTraceService service,
        TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        var traceId = await TryGetTraceIdAsync(service, identifier, cancellationToken);
        if (traceId is null)
        {
            var identifierValue = identifier.IsLocalId
                ? identifier.LocalId!.Value.ToString()
                : identifier.OTelTraceId;

            throw new InvalidOperationException(
                $"Unable to find a trace with the identifier '{identifierValue}'");
        }

        return traceId.Value;
    }
}
