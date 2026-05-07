using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Outcome of a property value dispatch.
/// </summary>
/// <remarks>
/// Either <see cref="NewRootValue"/> is populated (on success) or <see cref="Error"/> is populated
/// (on failure). The caller is responsible for persisting <see cref="NewRootValue"/> — the
/// dispatcher never writes data.
/// </remarks>
/// <param name="NewRootValue">
/// The mutated root property value. <c>null</c> when the operation cleared the value entirely or
/// when the dispatch failed.
/// </param>
/// <param name="BlockKey">
/// Populated for <see cref="AIPropertyOperation.AddItem"/>: the freshly-minted block key the LLM
/// can use to reference the new item in subsequent calls.
/// </param>
/// <param name="Error">Structured error payload when the dispatch failed; <c>null</c> on success.</param>
public sealed record AIPropertyValueDispatchResult(
    JsonNode? NewRootValue,
    Guid? BlockKey,
    AIPropertyValueOperationError? Error)
{
    /// <summary>Gets a value indicating whether the dispatch succeeded.</summary>
    public bool Success => Error is null;

    /// <summary>Builds a successful result.</summary>
    /// <param name="newRootValue">The mutated root value.</param>
    /// <param name="blockKey">The new block key for AddItem (or <c>null</c> for other operations).</param>
    /// <returns>A successful <see cref="AIPropertyValueDispatchResult"/>.</returns>
    public static AIPropertyValueDispatchResult Ok(JsonNode? newRootValue, Guid? blockKey = null)
        => new(newRootValue, blockKey, null);

    /// <summary>Builds a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="AIPropertyValueDispatchResult"/>.</returns>
    public static AIPropertyValueDispatchResult Fail(AIPropertyValueOperationError error)
        => new(null, null, error);
}
