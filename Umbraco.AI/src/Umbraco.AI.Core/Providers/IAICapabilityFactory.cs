namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Factory for creating AI capability instances.
/// </summary>
public interface IAICapabilityFactory
{
    /// <summary>
    /// Creates an AI capability instance for the specified provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    TCapability Create<TCapability>(IAIProvider provider)
        where TCapability : class, IAICapability;
}