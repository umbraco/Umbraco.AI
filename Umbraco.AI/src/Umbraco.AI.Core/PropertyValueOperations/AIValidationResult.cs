namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Outcome of a handler-level validation check.
/// </summary>
/// <param name="Error">The structured error if validation failed; <c>null</c> when valid.</param>
public sealed record AIValidationResult(AIPropertyValueOperationError? Error)
{
    /// <summary>Gets a value indicating whether validation passed.</summary>
    public bool IsValid => Error is null;

    /// <summary>Gets a singleton <see cref="AIValidationResult"/> indicating success.</summary>
    public static AIValidationResult Valid { get; } = new(Error: null);

    /// <summary>Builds a failed validation result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="AIValidationResult"/>.</returns>
    public static AIValidationResult Invalid(AIPropertyValueOperationError error) => new(Error: error);
}
