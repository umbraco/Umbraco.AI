using Umbraco.AI.Prompt.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Prompt.Tests.Unit.Fixtures;

/// <summary>
/// Minimal fake of <see cref="IEFCoreScopeProvider{TDbContext}"/> for <see cref="UmbracoAIPromptDbContext"/>, so
/// the real prompt repository runs against a real SQLite database without the CMS scope machinery.
/// </summary>
internal sealed class TestEFCoreScopeProvider : IEFCoreScopeProvider<UmbracoAIPromptDbContext>
{
    private readonly Func<UmbracoAIPromptDbContext> _contextFactory;

    public TestEFCoreScopeProvider(Func<UmbracoAIPromptDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IEfCoreScope<UmbracoAIPromptDbContext> CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => new TestEFCoreScope(_contextFactory());

    public IEfCoreScope<UmbracoAIPromptDbContext> CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => CreateScope(repositoryCacheMode, scopeFileSystems);

    public void AttachScope(IEfCoreScope<UmbracoAIPromptDbContext> other)
    {
        // No-op for tests.
    }

    public IEfCoreScope<UmbracoAIPromptDbContext> DetachScope() =>
        throw new NotSupportedException("DetachScope is not supported in the test scope provider.");

    public IScopeContext? AmbientScopeContext => null;
}

internal sealed class TestEFCoreScope : IEfCoreScope<UmbracoAIPromptDbContext>
{
    private readonly UmbracoAIPromptDbContext _context;
    private bool _completed;
    private bool _disposed;

    public TestEFCoreScope(UmbracoAIPromptDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteWithContextAsync<T>(Func<UmbracoAIPromptDbContext, Task<T>> method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await method(_context);
    }

    public async Task ExecuteWithContextAsync<T>(Func<UmbracoAIPromptDbContext, Task> method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await method(_context);
    }

    public IScopeContext? ScopeContext { get; set; }

    public IScopedNotificationPublisher Notifications =>
        throw new NotSupportedException("Notifications are not supported in the test scope.");

    public Guid InstanceId { get; } = Guid.NewGuid();

    public int CreatedThreadId => Environment.CurrentManagedThreadId;

    public int Depth => 0;

    public ILockingMechanism Locks =>
        throw new NotSupportedException("Locking is not supported in the test scope.");

    public RepositoryCacheMode RepositoryCacheMode => RepositoryCacheMode.Default;

    public IsolatedCaches IsolatedCaches =>
        throw new NotSupportedException("IsolatedCaches is not supported in the test scope.");

    public bool Complete()
    {
        _completed = true;
        return true;
    }

    public void ReadLock(params int[] lockIds) { }

    public void WriteLock(params int[] lockIds) { }

    public void WriteLock(TimeSpan timeout, int lockId) { }

    public void ReadLock(TimeSpan timeout, int lockId) { }

    public void EagerWriteLock(params int[] lockIds) { }

    public void EagerWriteLock(TimeSpan timeout, int lockId) { }

    public void EagerReadLock(TimeSpan timeout, int lockId) { }

    public void EagerReadLock(params int[] lockIds) { }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_completed)
        {
            _context.SaveChanges();
        }

        _context.Dispose();
    }
}
