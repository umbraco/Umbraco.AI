using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Registry;

/// <summary>
/// Central registry for AI providers. Providers are injected via DI after assembly scanning.
/// </summary>
internal sealed class AiRegistry : IAiRegistry
{
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;

    public AiRegistry(IEnumerable<IAiProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<IAiProvider> Providers => _providers.Values;
    
    public IEnumerable<IAiProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAiCapability
        => _providers.Values.Where(x => x.HasCapability<TCapability>());
    public IAiProvider? GetProvider(string providerId)
        => _providers.GetValueOrDefault(providerId);

    public TCapability? GetCapability<TCapability>(string providerId)
        where TCapability : class, IAiCapability
        =>  GetProvider(providerId)?.GetCapability<TCapability>();
}
