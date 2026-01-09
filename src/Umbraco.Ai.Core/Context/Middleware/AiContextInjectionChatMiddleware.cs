using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;

namespace Umbraco.Ai.Core.Context.Middleware;

/// <summary>
/// Chat middleware that injects AI context into chat requests.
/// </summary>
/// <remarks>
/// This middleware is registered by default and resolves context from all registered
/// resolvers (via ChatOptions.AdditionalProperties). It injects "Always" mode resources into
/// the system prompt and makes the resolved context available via <see cref="IAiContextAccessor"/>
/// for OnDemand tools.
/// </remarks>
public class AiContextInjectionChatMiddleware : IAiChatMiddleware
{
    private readonly IAiContextResolutionService _contextResolutionService;
    private readonly IAiContextFormatter _contextFormatter;
    private readonly IAiContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextInjectionChatMiddleware"/> class.
    /// </summary>
    /// <param name="contextResolutionService">The context resolution service.</param>
    /// <param name="contextFormatter">The context formatter.</param>
    /// <param name="contextAccessor">The context accessor.</param>
    public AiContextInjectionChatMiddleware(
        IAiContextResolutionService contextResolutionService,
        IAiContextFormatter contextFormatter,
        IAiContextAccessor contextAccessor)
    {
        _contextResolutionService = contextResolutionService;
        _contextFormatter = contextFormatter;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new ContextInjectingChatClient(
            client,
            _contextResolutionService,
            _contextFormatter,
            _contextAccessor);
    }
}
