namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Contributes context to an AI runtime context.
/// Contributors can add ambient context (user info, settings) and/or
/// process specific request context items.
/// </summary>
/// <remarks>
/// <para>
/// Contributors are executed in registration order. Use
/// <see cref="AiRequestContextItemCollection.Handle"/> or
/// <see cref="AiRequestContextItemCollection.HandleAll"/> to process
/// specific request context items. Use
/// <see cref="AiRequestContextItemCollection.HandleUnhandled"/> for
/// fallback processing of remaining items.
/// </para>
/// </remarks>
public interface IAiRuntimeContextContributor
{
    /// <summary>
    /// Contributes to the runtime context.
    /// </summary>
    /// <param name="context">The runtime context to contribute to.</param>
    void Contribute(AiRuntimeContext context);
}
