namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Factory for creating AI capability instances.
/// </summary>
public interface IAiCapabilityFactory
{
    /// <summary>
    /// Creates an AI capability instance for the specified provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    TCapability Create<TCapability>(IAiProvider provider)
        where TCapability : class, IAiCapability;
}