using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Umbraco.AI.Web.Api.Common.Json;

namespace Umbraco.AI.Web.Api.Common.Models;

/// <summary>
/// Represents a value that can be either a local database GUID or an OpenTelemetry TraceId string.
/// Useful for API endpoints that accept either identifier format.
/// </summary>
[JsonConverter(typeof(TraceIdentifierJsonConverter))]
public class TraceIdentifier : IParsable<TraceIdentifier>
{
    /// <summary>
    /// Gets the local database GUID if this instance represents a local ID.
    /// </summary>
    public Guid? LocalId { get; }

    /// <summary>
    /// Gets the OpenTelemetry TraceId string if this instance represents an OTel TraceId.
    /// </summary>
    public string? OTelTraceId { get; }

    /// <summary>
    /// Creates a new TraceIdentifier from a local database GUID.
    /// </summary>
    public TraceIdentifier(Guid localId)
    {
        LocalId = localId;
    }

    /// <summary>
    /// Creates a new TraceIdentifier from an OpenTelemetry TraceId string.
    /// </summary>
    public TraceIdentifier(string otelTraceId)
    {
        OTelTraceId = otelTraceId;
    }

    /// <summary>
    /// Gets whether this instance represents a local database ID.
    /// </summary>
    [JsonIgnore]
    public bool IsLocalId => LocalId.HasValue;

    /// <summary>
    /// Gets whether this instance represents an OpenTelemetry TraceId.
    /// </summary>
    [JsonIgnore]
    public bool IsOTelTraceId => !string.IsNullOrEmpty(OTelTraceId);

    /// <inheritdoc />
    public override string? ToString() => IsLocalId ? LocalId!.Value.ToString() : OTelTraceId;

    /// <summary>
    /// Implicitly converts a GUID to a TraceIdentifier.
    /// </summary>
    public static implicit operator TraceIdentifier(Guid localId) => new(localId);

    /// <summary>
    /// Implicitly converts a string to a TraceIdentifier.
    /// </summary>
    public static implicit operator TraceIdentifier(string otelTraceId) => new(otelTraceId);

    /// <inheritdoc />
    public static TraceIdentifier Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out TraceIdentifier? result))
        {
            throw new ArgumentException("Could not parse supplied value.", nameof(s));
        }

        return result;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TraceIdentifier result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = null;
            return false;
        }

        if (Guid.TryParse(s, out Guid id))
        {
            result = new TraceIdentifier(id);
        }
        else
        {
            result = new TraceIdentifier(s);
        }

        return true;
    }
}
