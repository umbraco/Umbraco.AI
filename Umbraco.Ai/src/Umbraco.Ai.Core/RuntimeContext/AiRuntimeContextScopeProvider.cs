using Microsoft.AspNetCore.Http;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Provides runtime context scope management using HttpContext.Items for storage.
/// </summary>
/// <remarks>
/// This implementation stores the runtime context in HttpContext.Items, making it
/// available across async boundaries within the same HTTP request.
/// </remarks>
internal sealed class AiRuntimeContextScopeProvider : IAiRuntimeContextScopeProvider, IAiRuntimeContextAccessor
{
    private const string ContextKey = "Umbraco.Ai.RuntimeContext";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiRuntimeContextScopeProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public AiRuntimeContextScopeProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public AiRuntimeContext? Context => _httpContextAccessor.HttpContext?.Items[ContextKey] as AiRuntimeContext;

    /// <inheritdoc />
    public IAiRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAiRuntimeContextScope CreateScope(IEnumerable<AiRequestContextItem> items)
    {
        // If scope already exists, return no-op wrapper (one scope per request)
        if (_httpContextAccessor.HttpContext?.Items[ContextKey] is AiRuntimeContext existing)
        {
            return new NoOpScope(existing);
        }

        var context = new AiRuntimeContext(items);
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items[ContextKey] = context;
        }

        return new OwningScope(_httpContextAccessor, context);
    }

    /// <summary>
    /// A scope that owns the context and removes it on dispose.
    /// </summary>
    private sealed class OwningScope : IAiRuntimeContextScope
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OwningScope(IHttpContextAccessor httpContextAccessor, AiRuntimeContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            Context = context;
        }

        public AiRuntimeContext Context { get; }

        public void Dispose()
        {
            _httpContextAccessor.HttpContext?.Items.Remove(ContextKey);
        }
    }

    /// <summary>
    /// A no-op scope for nested usage - doesn't remove the context on dispose.
    /// </summary>
    private sealed class NoOpScope : IAiRuntimeContextScope
    {
        public NoOpScope(AiRuntimeContext context)
        {
            Context = context;
        }

        public AiRuntimeContext Context { get; }

        public void Dispose()
        {
            // No-op: don't remove the context, it belongs to the outer scope
        }
    }
}
