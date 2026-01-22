using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// Middleware that reorders tool calls to ensure server-side tools execute before frontend tools.
/// </summary>
/// <remarks>
/// <para>
/// This middleware solves the problem of mixed frontend and server-side tool execution.
/// When the model calls multiple tools, if a frontend tool (which sets <c>Terminate = true</c>)
/// is processed first by <c>FunctionInvokingChatClient</c>, server-side tools never execute.
/// </para>
/// <para>
/// By inserting this middleware before <c>AiFunctionInvokingChatMiddleware</c>, tool calls
/// are reordered so that server-side tools appear first and execute before any frontend
/// tool triggers termination.
/// </para>
/// <para>
/// Frontend tool names are read from <see cref="ChatOptions.AdditionalProperties"/>
/// using the key <see cref="Constants.ContextKeys.FrontendToolNames"/>.
/// </para>
/// </remarks>
public sealed class AiToolReorderingChatMiddleware(IAiRuntimeContextAccessor runtimeContextAccessor) : IAiChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AiToolReorderingChatClient(client, runtimeContextAccessor);
    }
}
