using Umbraco.AI.Agent.Conversations.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Fixtures;

/// <summary>
/// Minimal fake of <see cref="IEFCoreScopeProvider{TDbContext}"/> for <see cref="UmbracoAIConversationsDbContext"/>,
/// mirroring Umbraco.AI.Tests.Common's <c>TestEFCoreScopeProvider</c> (which is bound to the
/// unrelated <c>UmbracoAIDbContext</c> and so can't be reused here as-is). Shared across
/// Conversations-repository tests that need a real EF-Core-backed scope (e.g. <c>ConversationSharesIndicatorTests</c>).
/// </summary>
internal sealed class TestEFCoreScopeProvider : IEFCoreScopeProvider<UmbracoAIConversationsDbContext>
{
    private readonly Func<UmbracoAIConversationsDbContext> _contextFactory;

    public TestEFCoreScopeProvider(Func<UmbracoAIConversationsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IEfCoreScope<UmbracoAIConversationsDbContext> CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => new TestEFCoreScope(_contextFactory());

    public IEfCoreScope<UmbracoAIConversationsDbContext> CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) => CreateScope(repositoryCacheMode, scopeFileSystems);

    public void AttachScope(IEfCoreScope<UmbracoAIConversationsDbContext> other)
    {
        // No-op for tests.
    }

    public IEfCoreScope<UmbracoAIConversationsDbContext> DetachScope() =>
        throw new NotSupportedException("DetachScope is not supported in test scope provider.");

    public IScopeContext? AmbientScopeContext => null;
}
