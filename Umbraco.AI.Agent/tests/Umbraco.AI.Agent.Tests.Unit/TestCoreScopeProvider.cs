using Moq;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Agent.Tests.Unit;

/// <summary>
/// An <see cref="ICoreScopeProvider"/> for tests that build an <c>UmbracoMapper</c>. The mapper completes
/// the scope it opens, so a bare <c>Mock.Of&lt;ICoreScopeProvider&gt;()</c> (which hands back no scope) is not enough.
/// </summary>
internal static class TestCoreScopeProvider
{
    /// <summary>
    /// Creates a scope provider whose scopes accept being completed and disposed.
    /// </summary>
    public static ICoreScopeProvider Create()
    {
        var provider = new Mock<ICoreScopeProvider>();
        provider
            .Setup(x => x.CreateCoreScope(
                It.IsAny<System.Data.IsolationLevel>(),
                It.IsAny<RepositoryCacheMode>(),
                It.IsAny<IEventDispatcher?>(),
                It.IsAny<IScopedNotificationPublisher?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(() => Mock.Of<ICoreScope>());
        return provider.Object;
    }
}
