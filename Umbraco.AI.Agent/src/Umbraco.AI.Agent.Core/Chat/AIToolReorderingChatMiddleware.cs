using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

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
/// By inserting this middleware before <c>AIFunctionInvokingChatMiddleware</c>, tool calls
/// are reordered so that server-side tools appear first and execute before any frontend
/// tool triggers termination.
/// </para>
/// <para>
/// Frontend tool names are read from <see cref="ChatOptions.AdditionalProperties"/>
/// using the key <see cref="Constants.ContextKeys.FrontendToolNames"/>.
/// </para>
/// </remarks>
public sealed class AIToolReorderingChatMiddleware(IAIRuntimeContextAccessor runtimeContextAccessor) : IAIChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIToolReorderingChatClient(client, runtimeContextAccessor);
    }
}
