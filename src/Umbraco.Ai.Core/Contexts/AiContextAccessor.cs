using Microsoft.AspNetCore.Http;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextAccessor"/> using HttpContext.Items.
/// </summary>
/// <remarks>
/// Uses HttpContext.Items instead of AsyncLocal because AsyncLocal doesn't survive
/// the async boundaries created by MEAI's FunctionInvokingChatClient during tool execution.
/// HttpContext.Items is preserved across all async calls within an HTTP request.
/// </remarks>
internal sealed class AiContextAccessor(IHttpContextAccessor httpContextAccessor) : IAiContextAccessor
{
    private const string ContextKey = "Umbraco.Ai.ResolvedContext";

    /// <inheritdoc />
    public AiResolvedContext? Context =>
        httpContextAccessor.HttpContext?.Items[ContextKey] as AiResolvedContext;

    /// <inheritdoc />
    public IDisposable SetContext(AiResolvedContext context)
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
