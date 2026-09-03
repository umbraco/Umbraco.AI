namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Contributes context to an AI runtime context.
/// Contributors can add ambient context (user info, settings) and/or
/// process specific request context items.
/// </summary>
/// <remarks>
/// <para>
/// Contributors are executed in registration order. Use
/// <see cref="AIRequestContextItemCollection.Handle"/> or
/// <see cref="AIRequestContextItemCollection.HandleAll"/> to process
/// specific request context items. Use
/// <see cref="AIRequestContextItemCollection.HandleUnhandled"/> for
/// fallback processing of remaining items.
/// </para>
/// </remarks>
public interface IAIRuntimeContextContributor
{
    /// <summary>
    /// Contributes to the runtime context.
    /// </summary>
    /// <param name="context">The runtime context to contribute to.</param>
    void Contribute(AIRuntimeContext context);

    /// <summary>
    /// Contributes to the runtime context asynchronously. The default implementation calls
    /// <see cref="Contribute"/>, so existing implementers work unchanged. Override this when a
    /// contributor needs to do async work (e.g. resolving a file) before it can format its
    /// content.
    /// </summary>
    /// <param name="context">The runtime context to contribute to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ContributeAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
    {
        Contribute(context);
        return Task.CompletedTask;
    }
}
