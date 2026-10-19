namespace Umbraco.AI.Core.Observability;

/// <summary>
/// An ambient scope that owns an <see cref="AIUsageCollector"/> and makes it the current collector
/// for the logical call flow. Uses AsyncLocal to flow across async boundaries; nested scopes
/// restore their parent's collector on disposal.
/// </summary>
internal sealed class AIUsageCollectionScope : IDisposable
{
    private static readonly AsyncLocal<AIUsageCollector?> _current = new();

    private readonly AIUsageCollector? _previousCollector;

    /// <summary>
    /// Gets the collector of the innermost active scope, if any.
    /// </summary>
    public static AIUsageCollector? Current => _current.Value;

    /// <summary>
    /// Gets the collector owned by this scope.
    /// </summary>
    public AIUsageCollector Collector { get; }

    private AIUsageCollectionScope()
    {
        Collector = new AIUsageCollector();
        _previousCollector = _current.Value;
        _current.Value = Collector;
    }

    /// <summary>
    /// Begins a new collection scope with its own collector.
    /// </summary>
    /// <returns>A disposable scope that restores the previous collector on disposal.</returns>
    public static AIUsageCollectionScope Begin() => new();

    /// <summary>
    /// Restores the previous collector.
    /// </summary>
    public void Dispose()
    {
        _current.Value = _previousCollector;
    }
}
