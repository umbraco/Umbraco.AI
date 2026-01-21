using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Mutable runtime context that accumulates state during an AI request.
/// Contributors populate from request items, and tools can add multimodal content.
/// </summary>
public sealed class AiRuntimeContext
{
    /// <summary>
    /// The raw context items from the request.
    /// </summary>
    public IReadOnlyList<AiRuntimeContextItem> Items { get; }

    /// <summary>
    /// System message parts to inject (aggregated from contributors).
    /// </summary>
    public List<string> SystemMessageParts { get; } = [];

    /// <summary>
    /// Template variables (aggregated from contributors).
    /// </summary>
    public Dictionary<string, object?> Variables { get; } = [];

    /// <summary>
    /// Typed data bag - contributors store extracted data by key.
    /// Use <see cref="AiRuntimeContextKeys"/> constants for well-known keys.
    /// </summary>
    public Dictionary<string, object?> Data { get; } = [];

    /// <summary>
    /// Multimodal content for injection (images, etc.).
    /// </summary>
    public List<AIContent> MultimodalContents { get; } = [];

    /// <summary>
    /// Indicates whether multimodal content has been added since last injection.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Creates a new runtime context from a collection of context items.
    /// </summary>
    /// <param name="items">The raw context items from the request.</param>
    public AiRuntimeContext(IEnumerable<AiRuntimeContextItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.ToList();
    }

    /// <summary>
    /// Adds data to the runtime context for injection into the next LLM call.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="mediaType">The MIME type (e.g., "image/png").</param>
    /// <param name="description">Optional description for the AI to reference.</param>
    public void AddData(byte[] data, string mediaType, string? description = null)
    {
        MultimodalContents.Add(new DataContent(data, mediaType));
        if (!string.IsNullOrEmpty(description))
        {
            MultimodalContents.Add(new TextContent($" [Data: {description}]"));
        }
        IsDirty = true;
    }

    /// <summary>
    /// Adds arbitrary content to the runtime context for injection.
    /// </summary>
    /// <param name="content">The content to add.</param>
    public void AddContent(AIContent content)
    {
        MultimodalContents.Add(content);
        IsDirty = true;
    }

    /// <summary>
    /// Marks the context as clean after multimodal content has been injected.
    /// </summary>
    internal void Clean() => IsDirty = false;

    /// <summary>
    /// Gets typed data from the bag.
    /// </summary>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The data if found and of the correct type; otherwise null.</returns>
    public T? GetData<T>(string key) where T : class
        => Data.TryGetValue(key, out var value) ? value as T : null;
    
    /// <summary>
    /// Gets typed data from the bag.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="result">The output result.</param>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <returns></returns>
    public bool TryGetData<T>(string key, out T result) where T : class
    {
        if (Data.TryGetValue(key, out var value) && value is T typed)
        {
            result = typed;
            return true;
        }

        result = null!;
        return false;
    }

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
    public T? GetValue<T>(string key)
        => Data.TryGetValue(key, out var value) && value is T typed ? typed : default;

    /// <summary>
    /// Gets a value type from the data bag.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="result"></param>
    /// <returns>The value if found; otherwise default.</returns>
    public bool TryGetValue<T>(string key, out T result)
    {
        if (Data.TryGetValue(key, out var value) && value is T typed)
        {
            result = typed;
            return true;
        }

        result = default!;
        return false;
    }

    /// <summary>
    /// Sets a value type in the data bag.
    /// </summary>
    /// <param name="key">The key to store under.</param>
    /// <param name="value">The value to store.</param>
    public void SetValue(string key, object? value) 
        => Data[key] = value;
}
