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
internal sealed class AIRuntimeContextScopeProvider : IAIRuntimeContextScopeProvider, IAIRuntimeContextAccessor
{
    private const string ContextKey = "Umbraco.AI.RuntimeContext";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Fallback storage for detached scopes (no HttpContext available).
    /// Uses AsyncLocal so each async flow gets its own stack.
    /// </summary>
    private static readonly AsyncLocal<Stack<AIRuntimeContext>?> _detachedContextStack = new();

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

            // Fallback to detached (AsyncLocal) storage
            if (_detachedContextStack.Value is { Count: > 0 } detachedStack)
            {
                return detachedStack.Peek();
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

        // If no HttpContext, use AsyncLocal-backed detached scope
        if (httpContext == null)
        {
            var detachedStack = _detachedContextStack.Value;
            if (detachedStack is null)
            {
                detachedStack = new Stack<AIRuntimeContext>();
                _detachedContextStack.Value = detachedStack;
            }

            var detachedParent = detachedStack.Count > 0 ? detachedStack.Peek() : null;
            var detachedContext = new AIRuntimeContext(items);
            detachedStack.Push(detachedContext);

            return new DetachedScope(detachedContext, detachedStack, detachedParent);
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
    /// A detached scope for scenarios without HttpContext.
    /// Uses AsyncLocal-backed stack for context accessibility via the accessor.
    /// </summary>
    private sealed class DetachedScope : IAIRuntimeContextScope
    {
        private readonly Stack<AIRuntimeContext> _stack;
        private bool _disposed;

        public DetachedScope(AIRuntimeContext context, Stack<AIRuntimeContext> stack, AIRuntimeContext? parentContext)
        {
            Context = context;
            ParentContext = parentContext;
            _stack = stack;
        }

        public AIRuntimeContext Context { get; }

        public AIRuntimeContext? ParentContext { get; }

        public int Depth => _stack.Count;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Pop this context from the detached stack
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), Context))
            {
                _stack.Pop();
            }

            // Clean up AsyncLocal if stack is empty
            if (_stack.Count == 0)
            {
                _detachedContextStack.Value = null;
            }
        }
    }
}
