namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// Mutable context object that processors populate.
/// Passed through the processor pipeline, each processor adds what it extracts.
/// </summary>
public sealed class AiRequestContext
{
    /// <summary>
    /// The raw context items from the request.
    /// </summary>
    public IReadOnlyList<AiRequestContextItem> Items { get; }

    /// <summary>
    /// System message parts to inject (aggregated from processors).
    /// </summary>
    public List<string> SystemMessageParts { get; } = [];

    /// <summary>
    /// Template variables (aggregated from processors).
    /// </summary>
    public Dictionary<string, object?> Variables { get; } = [];

    /// <summary>
    /// Typed data bag - processors store extracted data by key.
    /// Use <see cref="AiRequestContextKeys"/> constants for well-known keys.
    /// </summary>
    public Dictionary<string, object?> Data { get; } = [];

    /// <summary>
    /// Creates a new request context from a collection of context items.
    /// </summary>
    /// <param name="items">The raw context items from the request.</param>
    public AiRequestContext(IEnumerable<AiRequestContextItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.ToList();
    }

    /// <summary>
    /// Gets typed data from the bag.
    /// </summary>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The data if found and of the correct type; otherwise null.</returns>
    public T? GetData<T>(string key) where T : class
        => Data.TryGetValue(key, out var value) ? value as T : null;

    /// <summary>
    /// Sets typed data in the bag.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="key">The key to store under.</param>
    /// <param name="value">The value to store.</param>
    public void SetData<T>(string key, T value) where T : class
        => Data[key] = value;

    /// <summary>
    /// Gets a value type from the data bag.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value if found; otherwise default.</returns>
    public T? GetValue<T>(string key) where T : struct
        => Data.TryGetValue(key, out var value) && value is T typed ? typed : null;

    /// <summary>
    /// Sets a value type in the data bag.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key to store under.</param>
    /// <param name="value">The value to store.</param>
    public void SetValue<T>(string key, T value) where T : struct
        => Data[key] = value;
}
