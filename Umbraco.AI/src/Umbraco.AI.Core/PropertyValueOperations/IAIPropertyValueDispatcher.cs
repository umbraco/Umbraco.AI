namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Walks a property value path, applies the requested operation through the matching handler(s),
/// and returns the mutated root value.
/// </summary>
/// <remarks>
/// The dispatcher is stateless and does not read or write data. Inputs come from the caller via
/// <see cref="AIPropertyValueDispatchRequest"/>; outputs are returned via
/// <see cref="AIPropertyValueDispatchResult"/>. The same dispatcher is consumed by frontend tools
/// (over HTTP) and by future server-side tools (in-process).
/// </remarks>
public interface IAIPropertyValueDispatcher
{
    /// <summary>
    /// Performs a single property value operation.
    /// </summary>
    /// <param name="request">The dispatch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dispatch result (success or failure).</returns>
    Task<AIPropertyValueDispatchResult> DispatchAsync(
        AIPropertyValueDispatchRequest request,
        CancellationToken cancellationToken = default);
}
