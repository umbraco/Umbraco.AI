using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Mutable runtime context that accumulates state during an AI request.
/// Contributors populate from request items, and tools can add multimodal content.
/// </summary>
public sealed class AiRuntimeContext
{
    /// <summary>
    /// The raw context items from the request with support for handling tracking.
    /// </summary>
    public AiRequestContextItemCollection RequestContextItems { get; }

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
    /// <param name="requestContextItems">The raw context items from the request.</param>
    public AiRuntimeContext(IEnumerable<AiRequestContextItem> requestContextItems)
    {
        ArgumentNullException.ThrowIfNull(requestContextItems);
        RequestContextItems = new AiRequestContextItemCollection(requestContextItems);
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
