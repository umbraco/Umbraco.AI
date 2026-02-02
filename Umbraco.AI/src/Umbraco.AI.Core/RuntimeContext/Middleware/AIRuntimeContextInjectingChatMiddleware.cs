using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;

namespace Umbraco.AI.Core.RuntimeContext.Middleware;

/// <summary>
/// Middleware that injects multimodal content from the runtime context into chat messages.
/// </summary>
/// <remarks>
/// <para>
/// This middleware should be positioned before (innermost) the FunctionInvokingChatMiddleware
/// so that images added by tools are injected before the next LLM call.
/// </para>
/// <para>
/// When tools add images via <see cref="AIRuntimeContext.AddImage"/>, the context becomes
/// "dirty". On the next chat request, this middleware detects the dirty state, injects
/// the multimodal content, and marks the context clean.
/// </para>
/// </remarks>
public sealed class AIRuntimeContextInjectingChatMiddleware : IAiChatMiddleware
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIRuntimeContextInjectingChatMiddleware"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    public AIRuntimeContextInjectingChatMiddleware(IAiRuntimeContextAccessor runtimeContextAccessor)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIRuntimeContextInjectingChatClient(client, _runtimeContextAccessor);
    }
}
