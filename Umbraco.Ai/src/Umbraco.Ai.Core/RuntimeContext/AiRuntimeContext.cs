using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Mutable runtime context that accumulates state during an AI request.
/// Contributors populate from request items, and tools can add multimodal content.
/// </summary>
public sealed class AiRuntimeContext
{
    private readonly HashSet<AiRequestContextItem> _handledRequestContextItems = [];

    /// <summary>
    /// The raw context items from the request.
    /// </summary>
    public IReadOnlyList<AiRequestContextItem> RequestContextItems { get; }

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
    /// <param name="items">The raw context items from the request.</param>
    public AiRuntimeContext(IEnumerable<AiRequestContextItem> requestContextItems)
    {
        ArgumentNullException.ThrowIfNull(requestContextItems);
        RequestContextItems = requestContextItems.ToList();
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

    /// <summary>
    /// Handles the first unhandled request context item matching the predicate.
    /// The item is automatically marked as handled before the handler is invoked.
    /// </summary>
    /// <param name="predicate">Predicate to find a matching item.</param>
    /// <param name="handler">Action to process the matched item.</param>
    /// <returns>True if an item was found and handled; otherwise false.</returns>
    public bool HandleRequestContextItem(
        Func<AiRequestContextItem, bool> predicate,
        Action<AiRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);

        var item = RequestContextItems
            .Where(i => !_handledRequestContextItems.Contains(i))
            .FirstOrDefault(predicate);

        if (item is null)
            return false;

        _handledRequestContextItems.Add(item);
        handler(item);
        return true;
    }

    /// <summary>
    /// Handles all unhandled request context items matching the predicate.
    /// Each item is automatically marked as handled before the handler is invoked.
    /// </summary>
    /// <param name="predicate">Predicate to find matching items.</param>
    /// <param name="handler">Action to process each matched item.</param>
    public void HandleRequestContextItems(
        Func<AiRequestContextItem, bool> predicate,
        Action<AiRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);

        var matching = RequestContextItems
            .Where(i => !_handledRequestContextItems.Contains(i))
            .Where(predicate)
            .ToList();

        foreach (var item in matching)
        {
            _handledRequestContextItems.Add(item);
            handler(item);
        }
    }

    /// <summary>
    /// Handles all remaining unhandled request context items.
    /// Each item is automatically marked as handled before the handler is invoked.
    /// Typically used by fallback contributors that process any remaining items.
    /// </summary>
    /// <param name="handler">Action to process each unhandled item.</param>
    public void HandleUnhandledRequestContextItems(Action<AiRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var unhandled = RequestContextItems
            .Where(i => !_handledRequestContextItems.Contains(i))
            .ToList();

        foreach (var item in unhandled)
        {
            _handledRequestContextItems.Add(item);
            handler(item);
        }
    }

    /// <summary>
    /// Gets the count of request context items that have been handled.
    /// </summary>
    public int HandledRequestContextItemCount => _handledRequestContextItems.Count;

    /// <summary>
    /// Gets whether a specific request context item has been handled.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item has been handled; otherwise false.</returns>
    public bool IsRequestContextItemHandled(AiRequestContextItem item)
        => _handledRequestContextItems.Contains(item);
}
