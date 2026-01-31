namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Contributes context to an AI runtime context.
/// Contributors can add ambient context (user info, settings) and/or
/// process specific request context items.
/// </summary>
/// <remarks>
/// <para>
/// Contributors are executed in registration order. Use
/// <see cref="AiRuntimeContext.HandleRequestContextItem"/> or
/// <see cref="AiRuntimeContext.HandleRequestContextItems"/> to process
/// specific request context items. Use
/// <see cref="AiRuntimeContext.HandleUnhandledRequestContextItems"/> for
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
