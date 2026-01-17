namespace Umbraco.Ai.Core.Contexts.Resolvers;

/// <summary>
/// Request model for context resolution, providing access to ChatOptions additional properties.
/// </summary>
/// <remarks>
/// This wraps the <c>ChatOptions.AdditionalProperties</c> dictionary and provides
/// helper methods for extracting typed values. Each resolver defines its own key
/// constants internally for reading from <see cref="Properties"/>.
/// </remarks>
public sealed class AiContextResolverRequest
{
    /// <summary>
    /// Additional properties from ChatOptions containing resolver-specific data.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Creates an empty request.
    /// </summary>
    public AiContextResolverRequest()
    {
    }

    /// <summary>
    /// Creates a request from the given properties dictionary.
    /// </summary>
    /// <param name="properties">The properties dictionary from ChatOptions.</param>
    public AiContextResolverRequest(IReadOnlyDictionary<string, object?>? properties)
    {
        Properties = properties ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets a property value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or default if not found or wrong type.</returns>
    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            return value switch
            {
                T typed => typed,
                _ => default
            };
        }

        return default;
    }

    /// <summary>
    /// Gets a Guid property, handling string parsing.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The Guid value, or null if not found or invalid.</returns>
    public Guid? GetGuidProperty(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
            _ => null
        };
    }

    /// <summary>
    /// Gets a collection of Guid properties.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The collection of Guids, or null if not found.</returns>
    public IEnumerable<Guid>? GetGuidCollectionProperty(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            IEnumerable<Guid> guids => guids,
            IEnumerable<string> strings => strings
                .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value),
            _ => null
        };
    }
}
