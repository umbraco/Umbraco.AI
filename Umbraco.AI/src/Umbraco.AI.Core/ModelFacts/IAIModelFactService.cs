using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Aggregates the facts that every registered <see cref="IAIModelFactProvider"/> supplies about models.
/// </summary>
public interface IAIModelFactService
{
    /// <summary>
    /// Gets the facts for the given models, combined from all registered providers.
    /// </summary>
    /// <param name="context">The connection, provider, and capability the models belong to.</param>
    /// <param name="models">The models to get facts for.</param>
    /// <param name="cancellationToken">Cancels the request. Cancelling it is the only way this method throws.</param>
    /// <returns>
    /// Facts keyed by model id. Models with no facts are omitted. Per model, <see cref="AIModelFactTone.Warning"/>
    /// facts come first, then facts follow provider registration order, then each provider's own order.
    /// </returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> models,
        CancellationToken cancellationToken = default);
}
