using Umbraco.Extensions;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Factory for creating AI capability instances.
/// </summary>
public sealed class AiCapabilityFactory : IAiCapabilityFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiCapabilityFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public AiCapabilityFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates an AI capability instance for the specified provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public TCapability Create<TCapability>(IAiProvider provider)
        where TCapability : class, IAiCapability
    {
        return _serviceProvider.CreateInstance<TCapability>(provider);
    }
}