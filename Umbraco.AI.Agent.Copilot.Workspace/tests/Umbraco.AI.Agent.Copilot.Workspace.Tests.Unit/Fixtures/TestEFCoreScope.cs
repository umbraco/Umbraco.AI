using Umbraco.AI.Agent.Conversations.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Fixtures;

/// <summary>
/// Minimal fake of <see cref="IEFCoreScope{TDbContext}"/> for <see cref="UmbracoAIConversationsDbContext"/>,
/// paired with <see cref="TestEFCoreScopeProvider"/>. Shared across Conversations-repository tests that
/// need a real EF-Core-backed scope.
/// </summary>
internal sealed class TestEFCoreScope : IEFCoreScope<UmbracoAIConversationsDbContext>
{
    private readonly UmbracoAIConversationsDbContext _context;
    private bool _completed;
    private bool _disposed;

    public TestEFCoreScope(UmbracoAIConversationsDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteWithContextAsync<T>(Func<UmbracoAIConversationsDbContext, Task<T>> method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await method(_context);
    }

    public async Task ExecuteWithContextAsync<T>(Func<UmbracoAIConversationsDbContext, Task> method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await method(_context);
    }

    public IScopeContext? ScopeContext { get; set; }

    public IScopedNotificationPublisher Notifications =>
        throw new NotSupportedException("Notifications are not supported in test scope.");

    public Guid InstanceId { get; } = Guid.NewGuid();

    public int CreatedThreadId => Environment.CurrentManagedThreadId;

    public int Depth => 0;

    public ILockingMechanism Locks =>
        throw new NotSupportedException("Locking is not supported in test scope.");

    public RepositoryCacheMode RepositoryCacheMode => RepositoryCacheMode.Default;

    public IsolatedCaches IsolatedCaches =>
        throw new NotSupportedException("IsolatedCaches is not supported in test scope.");

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
