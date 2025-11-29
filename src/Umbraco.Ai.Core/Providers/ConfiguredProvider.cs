namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Wraps a provider with resolved settings, exposing configured capabilities.
/// </summary>
internal sealed class ConfiguredProvider : IConfiguredProvider
{
    private readonly IReadOnlyList<IConfiguredCapability> _capabilities;

    public ConfiguredProvider(IAiProvider provider, object resolvedSettings)
    {
        Provider = provider;
        _capabilities = WrapCapabilities(provider.GetCapabilities(), resolvedSettings);
    }

    /// <inheritdoc />
    public IAiProvider Provider { get; }

    /// <inheritdoc />
    public IReadOnlyList<IConfiguredCapability> GetCapabilities() => _capabilities;

    /// <inheritdoc />
    public TCapability? GetCapability<TCapability>() where TCapability : class, IConfiguredCapability
        => _capabilities.OfType<TCapability>().FirstOrDefault();

    /// <inheritdoc />
    public bool HasCapability<TCapability>() where TCapability : class, IConfiguredCapability
        => _capabilities.OfType<TCapability>().Any();

    private static IReadOnlyList<IConfiguredCapability> WrapCapabilities(
        IReadOnlyCollection<IAiCapability> capabilities,
        object settings)
    {
        var configured = new List<IConfiguredCapability>();
        foreach (var cap in capabilities)
        {
            IConfiguredCapability? wrapped = cap switch
            {
                IAiChatCapability chat => new ConfiguredChatCapability(chat, settings),
                IAiEmbeddingCapability embedding => new ConfiguredEmbeddingCapability(embedding, settings),
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
