namespace Umbraco.AI.Core.Utilities;

/// <summary>
/// General-purpose collection helpers.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Returns the source unchanged when it has items, or <c>null</c> when it is <c>null</c> or empty.
    /// </summary>
    /// <remarks>
    /// Useful when flowing a stored list into a slot that treats <c>null</c> as "not set" and any
    /// non-null list (including an empty one) as an explicit override.
    /// </remarks>
    public static IReadOnlyList<T>? NullIfEmpty<T>(this IReadOnlyList<T>? source)
        => source is { Count: > 0 } ? source : null;
}
