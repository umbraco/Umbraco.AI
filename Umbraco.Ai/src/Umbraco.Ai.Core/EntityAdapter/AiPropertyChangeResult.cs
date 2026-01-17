namespace Umbraco.Ai.Core.EntityAdapter;

/// <summary>
/// Represents the result of a property change operation.
/// </summary>
public sealed class AiPropertyChangeResult
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
    public static AiPropertyChangeResult Succeeded() => new() { Success = true };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static AiPropertyChangeResult Failed(string error) => new() { Success = false, Error = error };
}
