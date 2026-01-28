using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Umbraco.Ai.Core.Chat.Middleware;

/// <summary>
/// Middleware that adds automatic function/tool invocation support to chat clients.
/// </summary>
/// <remarks>
/// <para>
/// This middleware wraps the chat client with <see cref="FunctionInvokingChatClient"/>
/// which automatically invokes tools when the model requests them and feeds results
/// back to the model.
/// </para>
/// <para>
/// When no tools are configured in <see cref="ChatOptions.Tools"/>, this middleware
/// is effectively a no-op passthrough.
/// </para>
/// </remarks>
public sealed class AiFunctionInvokingChatMiddleware : IAiChatMiddleware
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiFunctionInvokingChatMiddleware"/> class.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for function invocation logging.</param>
    public AiFunctionInvokingChatMiddleware(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return client.AsBuilder()
            .UseFunctionInvocation(_loggerFactory)
            .Build();
    }
}
