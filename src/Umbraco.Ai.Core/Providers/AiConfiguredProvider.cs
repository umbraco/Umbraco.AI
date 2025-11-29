using Umbraco.Ai.Core.Connections;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Wraps a provider with resolved settings, exposing configured capabilities.
/// </summary>
internal sealed class AiConfiguredProvider(IAiProvider provider, object resolvedSettings) : IAiConfiguredProvider
{
    private readonly IReadOnlyList<IAiConfiguredCapability> _capabilities = WrapCapabilities(provider.GetCapabilities(), resolvedSettings);

    /// <inheritdoc />
    public IAiProvider Provider { get; } = provider;

    /// <inheritdoc />
    public IReadOnlyList<IAiConfiguredCapability> GetCapabilities() => _capabilities;

    /// <inheritdoc />
    public TCapability? GetCapability<TCapability>() where TCapability : class, IAiConfiguredCapability
        => _capabilities.OfType<TCapability>().FirstOrDefault();

    /// <inheritdoc />
    public bool HasCapability<TCapability>() where TCapability : class, IAiConfiguredCapability
        => _capabilities.OfType<TCapability>().Any();

    private static IReadOnlyList<IAiConfiguredCapability> WrapCapabilities(
        IReadOnlyCollection<IAiCapability> capabilities,
        object settings)
    {
        var configured = new List<IAiConfiguredCapability>();
        foreach (var cap in capabilities)
        {
            IAiConfiguredCapability? wrapped = cap switch
            {
                IAiChatCapability chat => new AiConfiguredChatCapability(chat, settings),
                IAiEmbeddingCapability embedding => new AiConfiguredEmbeddingCapability(embedding, settings),
                _ => null
            };
            if (wrapped is not null)
            {
                configured.Add(wrapped);
            }
        }
        return configured;
    }
}
