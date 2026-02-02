using System.Collections;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// A collection of request context items with support for tracking handled items.
/// Provides methods to process items while automatically tracking which have been handled.
/// </summary>
public sealed class AIRequestContextItemCollection : IReadOnlyList<AIRequestContextItem>
{
    private readonly List<AIRequestContextItem> _items;
    private readonly HashSet<AIRequestContextItem> _handledItems = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AIRequestContextItemCollection"/> class.
    /// </summary>
    /// <param name="items">The request context items.</param>
    public AIRequestContextItemCollection(IEnumerable<AIRequestContextItem> items)
    {
        _items = items.ToList();
    }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public AIRequestContextItem this[int index] => _items[index];

    /// <inheritdoc />
    public IEnumerator<AIRequestContextItem> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the count of items that have been handled.
    /// </summary>
    public int HandledCount => _handledItems.Count;

    /// <summary>
    /// Gets whether a specific item has been handled.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item has been handled; otherwise false.</returns>
    public bool IsHandled(AIRequestContextItem item) => _handledItems.Contains(item);

    /// <summary>
    /// Handles the first unhandled item matching the predicate.
    /// The item is automatically marked as handled before the handler is invoked.
    /// </summary>
    /// <param name="predicate">Predicate to find a matching item.</param>
    /// <param name="handler">Action to process the matched item.</param>
    /// <returns>True if an item was found and handled; otherwise false.</returns>
    public bool Handle(
        Func<AIRequestContextItem, bool> predicate,
        Action<AIRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);

        var item = _items
            .Where(i => !_handledItems.Contains(i))
            .FirstOrDefault(predicate);

        if (item is null)
            return false;

        _handledItems.Add(item);
        handler(item);
        return true;
    }

    /// <summary>
    /// Handles all unhandled items matching the predicate.
    /// Each item is automatically marked as handled before the handler is invoked.
    /// </summary>
    /// <param name="predicate">Predicate to find matching items.</param>
    /// <param name="handler">Action to process each matched item.</param>
    public void HandleAll(
        Func<AIRequestContextItem, bool> predicate,
        Action<AIRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);

        var matching = _items
            .Where(i => !_handledItems.Contains(i))
            .Where(predicate)
            .ToList();

        foreach (var item in matching)
        {
            _handledItems.Add(item);
            handler(item);
        }
    }

    /// <summary>
    /// Handles all remaining unhandled items.
    /// Each item is automatically marked as handled before the handler is invoked.
    /// Typically used by fallback contributors that process any remaining items.
    /// </summary>
    /// <param name="handler">Action to process each unhandled item.</param>
    public void HandleUnhandled(Action<AIRequestContextItem> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var unhandled = _items
            .Where(i => !_handledItems.Contains(i))
            .ToList();

        foreach (var item in unhandled)
        {
            _handledItems.Add(item);
            handler(item);
        }
    }
}
