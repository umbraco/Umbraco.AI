using Umbraco.AI.Agent.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Tests.Integration.Fixtures;

/// <summary>
/// Minimal fake of <see cref="IEFCoreScopeProvider{TDbContext}"/> for <see cref="UmbracoAIAgentDbContext"/>, so
/// the real agent repository runs against a real SQLite database without the CMS scope machinery.
/// </summary>
internal sealed class TestEFCoreScopeProvider : IEFCoreScopeProvider<UmbracoAIAgentDbContext>
{
    private readonly Func<UmbracoAIAgentDbContext> _contextFactory;

    public TestEFCoreScopeProvider(Func<UmbracoAIAgentDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IEFCoreScope<UmbracoAIAgentDbContext> CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => new TestEFCoreScope(_contextFactory());

    public IEFCoreScope<UmbracoAIAgentDbContext> CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => CreateScope(repositoryCacheMode, scopeFileSystems);

    public void AttachScope(IEFCoreScope<UmbracoAIAgentDbContext> other)
    {
        // No-op for tests.
    }

    public IEFCoreScope<UmbracoAIAgentDbContext> DetachScope() =>
        throw new NotSupportedException("DetachScope is not supported in the test scope provider.");

    public IScopeContext? AmbientScopeContext => null;
}

internal sealed class TestEFCoreScope : IEFCoreScope<UmbracoAIAgentDbContext>
{
    private readonly UmbracoAIAgentDbContext _context;
    private bool _completed;
    private bool _disposed;

    public TestEFCoreScope(UmbracoAIAgentDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteWithContextAsync<T>(Func<UmbracoAIAgentDbContext, Task<T>> method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await method(_context);
    }

    public async Task ExecuteWithContextAsync<T>(Func<UmbracoAIAgentDbContext, Task> method)
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
