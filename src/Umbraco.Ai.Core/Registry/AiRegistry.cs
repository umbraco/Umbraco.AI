using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Registry;

/// <summary>
/// Central registry for AI providers. Providers are resolved via the <see cref="AiProviderCollection"/>.
/// </summary>
internal sealed class AiRegistry : IAiRegistry
{
    private readonly AiProviderCollection _providers;

    public AiRegistry(AiProviderCollection providers)
    {
        _providers = providers;
    }

    public IEnumerable<IAiProvider> Providers => _providers;

    public IEnumerable<IAiProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAiCapability
        => _providers.GetWithCapability<TCapability>();

    public IAiProvider? GetProvider(string providerId)
        => _providers.GetById(providerId);

    public TCapability? GetCapability<TCapability>(string providerId)
        where TCapability : class, IAiCapability
        => GetProvider(providerId)?.GetCapability<TCapability>();
}
