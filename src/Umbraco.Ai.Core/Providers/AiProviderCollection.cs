using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// A collection of AI providers.
/// </summary>
public sealed class AiProviderCollection : BuilderCollectionBase<IAiProvider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiProviderCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the providers.</param>
    public AiProviderCollection(Func<IEnumerable<IAiProvider>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a provider by its unique identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The provider, or <c>null</c> if not found.</returns>
    public IAiProvider? GetById(string providerId)
        => this.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all providers that support a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The capability type.</typeparam>
    /// <returns>Providers that support the capability.</returns>
    public IEnumerable<IAiProvider> GetWithCapability<TCapability>()
        where TCapability : class, IAiCapability
        => this.Where(p => p.HasCapability<TCapability>());
}
