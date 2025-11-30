using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Umbraco.Ai.Web.Api.Management.Common.Json;

namespace Umbraco.Ai.Web.Api.Management.Common.Models;

/// <summary>
/// Represents a value that can be either a GUID ID or a string alias.
/// Useful for API endpoints that accept either identifier format.
/// </summary>
[JsonConverter(typeof(IdOrAliasJsonConverter))]
public class IdOrAlias : IParsable<IdOrAlias>
{
    /// <summary>
    /// Gets the GUID ID if this instance represents an ID.
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Gets the alias string if this instance represents an alias.
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// Creates a new IdOrAlias from a GUID ID.
    /// </summary>
    public IdOrAlias(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Creates a new IdOrAlias from a string alias.
    /// </summary>
    public IdOrAlias(string alias)
    {
        Alias = alias;
    }

    /// <summary>
    /// Gets whether this instance represents an ID.
    /// </summary>
    [JsonIgnore]
    public bool IsId => Id.HasValue;

    /// <summary>
    /// Gets whether this instance represents an alias.
    /// </summary>
    [JsonIgnore]
    public bool IsAlias => !string.IsNullOrEmpty(Alias);

    /// <inheritdoc />
    public override string? ToString() => IsId ? Id!.Value.ToString() : Alias;

    /// <summary>
    /// Implicitly converts a GUID to an IdOrAlias.
    /// </summary>
    public static implicit operator IdOrAlias(Guid id) => new(id);

    /// <summary>
    /// Implicitly converts a string to an IdOrAlias.
    /// </summary>
    public static implicit operator IdOrAlias(string alias) => new(alias);

    /// <inheritdoc />
    public static IdOrAlias Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out IdOrAlias? result))
        {
            throw new ArgumentException("Could not parse supplied value.", nameof(s));
        }

        return result;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out IdOrAlias result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = null;
            return false;
        }

        if (Guid.TryParse(s, out Guid id))
        {
            result = new IdOrAlias(id);
        }
        else
        {
            result = new IdOrAlias(s);
        }

        return true;
    }
}
