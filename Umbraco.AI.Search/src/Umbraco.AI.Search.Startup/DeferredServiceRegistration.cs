using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Search.Startup;

/// <summary>
/// A proxy <see cref="ICollectionBuilder"/> that defers service registrations until
/// <see cref="Umbraco.Cms.Core.DependencyInjection.IUmbracoBuilder.Build"/> is called,
/// which runs after all composers have executed.
/// </summary>
/// <remarks>
/// This is useful when registrations must happen last — for example, to conditionally
/// call <c>AddSearchCore()</c> only if no other composer has already registered it.
/// </remarks>
internal sealed class DeferredServiceRegistration : ICollectionBuilder
{
    private readonly List<Action<IServiceCollection>> _actions = [];

    /// <summary>
    /// Queues a registration action to run during <see cref="Umbraco.Cms.Core.DependencyInjection.IUmbracoBuilder.Build"/>.
    /// </summary>
    public DeferredServiceRegistration Add(Action<IServiceCollection> action)
    {
        _actions.Add(action);
        return this;
    }

    /// <inheritdoc />
    public void RegisterWith(IServiceCollection services)
    {
        foreach (var action in _actions)
        {
            action(services);
        }
    }
}
