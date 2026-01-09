namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAiContextAccessor"/> using AsyncLocal.
/// </summary>
internal sealed class AiContextAccessor : IAiContextAccessor
{
    private static readonly AsyncLocal<AiResolvedContext?> _context = new();

    /// <inheritdoc />
    public AiResolvedContext? Context => _context.Value;

    /// <inheritdoc />
    public IDisposable SetContext(AiResolvedContext context)
    {
        _context.Value = context;
        return new ContextScope();
    }

    private sealed class ContextScope : IDisposable
    {
        public void Dispose() => _context.Value = null;
    }
}
