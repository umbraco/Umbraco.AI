using Umbraco.Extensions;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Factory for creating AI capability instances.
/// </summary>
public sealed class AICapabilityFactory : IAICapabilityFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AICapabilityFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AICapabilityFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates an AI capability instance for the specified provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public TCapability Create<TCapability>(IAIProvider provider)
        where TCapability : class, IAICapability
    {
        return _serviceProvider.CreateInstance<TCapability>(provider);
    }
}