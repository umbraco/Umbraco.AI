namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Wraps a provider with resolved settings, exposing configured capabilities.
/// </summary>
internal sealed class AIConfiguredProvider(IAIProvider provider, object resolvedSettings) : IAIConfiguredProvider
{
    private readonly IReadOnlyList<IAIConfiguredCapability> _capabilities = WrapCapabilities(provider.GetCapabilities(), resolvedSettings);

    /// <inheritdoc />
    public IAIProvider Provider { get; } = provider;

    /// <inheritdoc />
    public IReadOnlyList<IAIConfiguredCapability> GetCapabilities() => _capabilities;

    /// <inheritdoc />
    public TCapability? GetCapability<TCapability>() where TCapability : class, IAIConfiguredCapability
        => _capabilities.OfType<TCapability>().FirstOrDefault();

    /// <inheritdoc />
    public bool HasCapability<TCapability>() where TCapability : class, IAIConfiguredCapability
        => _capabilities.OfType<TCapability>().Any();

    private static IReadOnlyList<IAIConfiguredCapability> WrapCapabilities(
        IReadOnlyCollection<IAICapability> capabilities,
        object settings)
    {
        var configured = new List<IAIConfiguredCapability>();
        foreach (var cap in capabilities)
        {
            IAIConfiguredCapability? wrapped = cap switch
            {
                IAIChatCapability chat => new AIConfiguredChatCapability(chat, settings),
                IAIEmbeddingCapability embedding => new AIConfiguredEmbeddingCapability(embedding, settings),
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
