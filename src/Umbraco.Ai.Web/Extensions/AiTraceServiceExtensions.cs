using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiAuditService"/> to support TraceIdentifier lookups.
/// </summary>
internal static class AiTraceServiceExtensions
{
    /// <summary>
    /// Tries to get a audit ID by local ID or OpenTelemetry TraceId.
    /// If already a local ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The audit service.</param>
    /// <param name="identifier">The local ID or OpenTelemetry TraceId to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetTraceIdAsync(
        this IAiAuditService service,
        TraceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        // If it's already a local ID, return it directly (no DB lookup needed)
        if (identifier is { IsLocalId: true, LocalId: not null })
        {
            return identifier.LocalId.Value;
        }

        // For OpenTelemetry TraceId, we need to look up the audit
        if (identifier.OTelTraceId != null)
        {
            var trace = await service.GetAuditByTraceIdAsync(identifier.OTelTraceId, cancellationToken);
            return trace?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a audit ID by local ID or OpenTelemetry TraceId, throwing if not found.
    /// </summary>
    /// <param name="service">The audit service.</param>
    /// <param name="identifier">The local ID or OpenTelemetry TraceId to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the audit is not found.</exception>
    public static async Task<Guid> GetTraceIdAsync(
        this IAiAuditService service,
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
                $"Unable to find a audit with the identifier '{identifierValue}'");
        }

        return traceId.Value;
    }
}
