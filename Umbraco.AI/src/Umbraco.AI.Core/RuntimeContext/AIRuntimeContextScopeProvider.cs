using Microsoft.AspNetCore.Http;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Provides runtime context scope management using HttpContext.Items for storage.
/// </summary>
/// <remarks>
/// <para>
/// This implementation stores a stack of runtime contexts in HttpContext.Items, making it
/// available across async boundaries within the same HTTP request.
/// </para>
/// <para>
/// Nested scopes are supported: each call to <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/>
/// pushes a new context onto the stack, and disposing the scope pops it, restoring the previous context.
/// </para>
/// </remarks>
internal sealed class AIRuntimeContextScopeProvider : IAiRuntimeContextScopeProvider, IAiRuntimeContextAccessor
{
    private const string ContextKey = "Umbraco.Ai.RuntimeContext";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIRuntimeContextScopeProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public AIRuntimeContextScopeProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public AIRuntimeContext? Context
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items[ContextKey] is Stack<AIRuntimeContext> stack && stack.Count > 0)
            {
                return stack.Peek();
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IAiRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAiRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // If no HttpContext, return a detached scope
        if (httpContext == null)
        {
            return new DetachedScope(new AIRuntimeContext(items));
        }

        // Get or create stack
        if (httpContext.Items[ContextKey] is not Stack<AIRuntimeContext> stack)
        {
            stack = new Stack<AIRuntimeContext>();
            httpContext.Items[ContextKey] = stack;
        }

        var parentContext = stack.Count > 0 ? stack.Peek() : null;
        var context = new AIRuntimeContext(items);
        stack.Push(context);

        return new StackScope(this, context, stack, parentContext);
    }

    /// <summary>
    /// A scope that owns a context on the stack and pops it on dispose.
    /// </summary>
    private sealed class StackScope : IAiRuntimeContextScope
    {
        private readonly AIRuntimeContextScopeProvider _provider;
        private readonly Stack<AIRuntimeContext> _stack;
        private bool _disposed;

        public StackScope(
            AIRuntimeContextScopeProvider provider,
            AIRuntimeContext context,
            Stack<AIRuntimeContext> stack,
            AIRuntimeContext? parentContext)
        {
            _provider = provider;
            _stack = stack;
            Context = context;
            ParentContext = parentContext;
            Depth = stack.Count;
        }

        public AIRuntimeContext Context { get; }

        public AIRuntimeContext? ParentContext { get; }

        public int Depth { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Only pop if this context is still at the top of the stack.
            // This handles out-of-order disposal gracefully.
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), Context))
            {
                _stack.Pop();
            }

            // Clean up the stack from HttpContext.Items if empty
            if (_stack.Count == 0)
            {
                _provider._httpContextAccessor.HttpContext?.Items.Remove(ContextKey);
            }
        }
    }

    /// <summary>
    /// A detached scope for scenarios without HttpContext.
    /// </summary>
    private sealed class DetachedScope : IAiRuntimeContextScope
    {
        public DetachedScope(AIRuntimeContext context)
        {
            Context = context;
        }

        public AIRuntimeContext Context { get; }

        public AIRuntimeContext? ParentContext => null;

        public int Depth => 1;

        public void Dispose()
        {
            // No-op: detached scopes don't manage any shared state
        }
    }
}
