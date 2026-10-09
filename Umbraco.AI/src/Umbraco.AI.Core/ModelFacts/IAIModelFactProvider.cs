using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Supplies facts (context window, pricing, deprecation notices, and so on) about AI models.
/// </summary>
/// <remarks>
/// Register implementations through <c>builder.AIModelFactProviders().Append&lt;T&gt;()</c>. Core runs every
/// registered provider concurrently, each under the time limit configured by
/// <see cref="AIModelFactOptions.ProviderTimeout"/>. A provider that throws or times out is logged and skipped;
/// it never fails the request.
/// </remarks>
public interface IAIModelFactProvider
{
    /// <summary>
    /// Gets how long core may cache this provider's facts for a single model.
    /// </summary>
    /// <remarks>
    /// Models the provider leaves out of its result are cached as "no facts" for the same duration.
    /// <see cref="TimeSpan.Zero"/> or a negative value means nothing is cached and the provider is asked again
    /// on every request.
    /// </remarks>
    TimeSpan CacheDuration { get; }

    /// <summary>
    /// Gets the facts for the given models.
    /// </summary>
    /// <param name="context">The connection, provider, and capability the models belong to.</param>
    /// <param name="models">The models to get facts for. Core only passes models that are not already cached.</param>
    /// <param name="cancellationToken">Cancelled when the provider time limit is reached or the request is aborted.</param>
    /// <returns>
    /// Facts keyed by model id. Models with no facts are left out of the dictionary. Ids that are not in
    /// <paramref name="models"/> are dropped.
    /// </returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> models,
        CancellationToken cancellationToken);
}
