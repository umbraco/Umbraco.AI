using Microsoft.AspNetCore.Http;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextAccessor"/> using HttpContext.Items.
/// </summary>
/// <remarks>
/// Uses HttpContext.Items instead of AsyncLocal because AsyncLocal doesn't survive
/// the async boundaries created by MEAI's FunctionInvokingChatClient during tool execution.
/// HttpContext.Items is preserved across all async calls within an HTTP request.
/// </remarks>
internal sealed class AIContextAccessor(IHttpContextAccessor httpContextAccessor) : IAIContextAccessor
{
    private const string ContextKey = "Umbraco.AI.ResolvedContext";

    /// <inheritdoc />
    public AIResolvedContext? Context =>
        httpContextAccessor.HttpContext?.Items[ContextKey] as AIResolvedContext;

    /// <inheritdoc />
    public IDisposable SetContext(AIResolvedContext context)
    {
        if (httpContextAccessor.HttpContext != null)
        {
            httpContextAccessor.HttpContext.Items[ContextKey] = context;
        }
        return new ContextScope(httpContextAccessor);
    }

    private sealed class ContextScope(IHttpContextAccessor httpContextAccessor) : IDisposable
    {
        public void Dispose()
        {
            httpContextAccessor.HttpContext?.Items.Remove(ContextKey);
        }
    }
}
