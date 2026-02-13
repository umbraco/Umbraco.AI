namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Represents the result of a value change operation.
/// </summary>
public sealed class AIValueChangeResult
{
    /// <summary>
    /// Whether the change was applied successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Human-readable error message if the change failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AIValueChangeResult Succeeded() => new() { Success = true };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static AIValueChangeResult Failed(string error) => new() { Success = false, Error = error };
}
