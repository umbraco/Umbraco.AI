using Umbraco.Ai.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.Ai.Tests.Common.Fixtures;

/// <summary>
/// Test implementation of <see cref="IEFCoreScopeProvider{TDbContext}"/> for unit testing.
/// </summary>
public class TestEfCoreScopeProvider : IEFCoreScopeProvider<UmbracoAiDbContext>
{
    private readonly Func<UmbracoAiDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new test scope provider.
    /// </summary>
    /// <param name="contextFactory">Factory to create DbContext instances.</param>
    public TestEfCoreScopeProvider(Func<UmbracoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public IEfCoreScope<UmbracoAiDbContext> CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null)
    {
        return new TestEfCoreScope(_contextFactory());
    }

    /// <inheritdoc />
    public IEfCoreScope<UmbracoAiDbContext> CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null)
    {
        return CreateScope(repositoryCacheMode, scopeFileSystems);
    }

    /// <inheritdoc />
    public void AttachScope(IEfCoreScope<UmbracoAiDbContext> other)
    {
        // No-op for tests
    }

    /// <inheritdoc />
    public IEfCoreScope<UmbracoAiDbContext> DetachScope()
    {
        throw new NotSupportedException("DetachScope is not supported in test scope provider.");
    }

    /// <inheritdoc />
    public IScopeContext? AmbientScopeContext => null;
}

/// <summary>
/// Test implementation of <see cref="IEfCoreScope{TDbContext}"/> for unit testing.
/// </summary>
public class TestEfCoreScope : IEfCoreScope<UmbracoAiDbContext>
{
    private readonly UmbracoAiDbContext _context;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new test scope with the given context.
    /// </summary>
    public TestEfCoreScope(UmbracoAiDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithContextAsync<T>(Func<UmbracoAiDbContext, Task<T>> method)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TestEfCoreScope));
        }

        return await method(_context);
    }

    /// <inheritdoc />
    public async Task ExecuteWithContextAsync<T>(Func<UmbracoAiDbContext, Task> method)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TestEfCoreScope));
        }

        await method(_context);
    }

    /// <inheritdoc />
    public IScopeContext? ScopeContext { get; set; }

    /// <inheritdoc />
    public IScopedNotificationPublisher Notifications =>
        throw new NotSupportedException("Notifications are not supported in test scope.");

    /// <inheritdoc />
    public Guid InstanceId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public int CreatedThreadId => Environment.CurrentManagedThreadId;

    /// <inheritdoc />
    public int Depth => 0;

    /// <inheritdoc />
    public ILockingMechanism Locks =>
        throw new NotSupportedException("Locking is not supported in test scope.");

    /// <inheritdoc />
    public RepositoryCacheMode RepositoryCacheMode => RepositoryCacheMode.Default;

    /// <inheritdoc />
    public IsolatedCaches IsolatedCaches =>
        throw new NotSupportedException("IsolatedCaches is not supported in test scope.");

    /// <inheritdoc />
    public bool Complete()
    {
        _completed = true;
        return true;
    }

    /// <inheritdoc />
    public void ReadLock(params int[] lockIds) { }

    /// <inheritdoc />
    public void WriteLock(params int[] lockIds) { }

    /// <inheritdoc />
    public void WriteLock(TimeSpan timeout, int lockId) { }

    /// <inheritdoc />
    public void ReadLock(TimeSpan timeout, int lockId) { }

    /// <inheritdoc />
    public void EagerWriteLock(params int[] lockIds) { }

    /// <inheritdoc />
    public void EagerWriteLock(TimeSpan timeout, int lockId) { }

    /// <inheritdoc />
    public void EagerReadLock(TimeSpan timeout, int lockId) { }

    /// <inheritdoc />
    public void EagerReadLock(params int[] lockIds) { }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Only save changes if Complete() was called
        if (_completed)
        {
            _context.SaveChanges();
        }

        _context.Dispose();
    }
}
