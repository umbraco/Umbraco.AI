using Microsoft.AspNetCore.Http;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Provides runtime context scope management using HttpContext.Items for storage,
/// with an AsyncLocal fallback for background tasks without HttpContext.
/// </summary>
/// <remarks>
/// <para>
/// This implementation stores a stack of runtime contexts in HttpContext.Items, making it
/// available across async boundaries within the same HTTP request. When no HttpContext is
/// available (e.g., background tasks, startup reindex, content publish handlers), an
/// AsyncLocal-based stack is used instead, ensuring runtime context is still accessible
/// for middleware such as audit logging.
/// </para>
/// <para>
/// Nested scopes are supported: each call to <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/>
/// pushes a new context onto the stack, and disposing the scope pops it, restoring the previous context.
/// </para>
/// </remarks>
internal sealed class AIRuntimeContextScopeProvider : IAIRuntimeContextScopeProvider, IAIRuntimeContextAccessor
{
    private const string ContextKey = "Umbraco.AI.RuntimeContext";

    private static readonly AsyncLocal<Stack<AIRuntimeContext>?> AsyncLocalStack = new();

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

            // Fall back to AsyncLocal for background tasks
            if (httpContext is null && AsyncLocalStack.Value is { Count: > 0 } asyncStack)
            {
                return asyncStack.Peek();
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // If no HttpContext, use AsyncLocal-based scope for background tasks
        if (httpContext == null)
        {
            var asyncStack = AsyncLocalStack.Value;
            if (asyncStack is null)
            {
                asyncStack = new Stack<AIRuntimeContext>();
                AsyncLocalStack.Value = asyncStack;
            }

            var asyncParentContext = asyncStack.Count > 0 ? asyncStack.Peek() : null;
            var asyncContext = new AIRuntimeContext(items);
            asyncStack.Push(asyncContext);

            return new AsyncLocalScope(asyncContext, asyncStack, asyncParentContext);
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
    private sealed class StackScope : IAIRuntimeContextScope
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
    /// A scope backed by AsyncLocal storage for background tasks without HttpContext.
    /// </summary>
    private sealed class AsyncLocalScope : IAIRuntimeContextScope
    {
        private readonly Stack<AIRuntimeContext> _stack;
        private bool _disposed;

        public AsyncLocalScope(
            AIRuntimeContext context,
            Stack<AIRuntimeContext> stack,
            AIRuntimeContext? parentContext)
        {
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

            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), Context))
            {
                _stack.Pop();
            }

            // Clean up AsyncLocal if empty
            if (_stack.Count == 0)
            {
                AsyncLocalStack.Value = null;
            }
        }
    }
}
