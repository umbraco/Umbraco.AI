using System.Globalization;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// Built-in <see cref="IAIModelFactProvider"/> that turns the standard model metadata (context window and
/// pricing) into facts. Reads only data already on the descriptor, so it is never cached.
/// </summary>
internal sealed class AIMetadataModelFactProvider : IAIModelFactProvider
{
    /// <inheritdoc />
    public TimeSpan CacheDuration => TimeSpan.Zero;

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
        AIModelFactContext context,
        IReadOnlyList<AIModelDescriptor> models,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IReadOnlyList<AIModelFact>>();

        foreach (var model in models)
        {
            var facts = BuildFacts(model);
            if (facts.Count > 0)
            {
                result[model.Model.ModelId] = facts;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>>(result);
    }

    private static List<AIModelFact> BuildFacts(AIModelDescriptor model)
    {
        var facts = new List<AIModelFact>();

        if (model.GetContextWindow() is { } contextWindow)
        {
            facts.Add(new AIModelFact
            {
                Key = "core.contextWindow",
                Label = "#uaiModelFacts_contextWindow",
                ShortLabel = "#uaiModelFacts_contextWindowShort",
                Value = contextWindow.ToString("N0", CultureInfo.InvariantCulture),
                SortValue = contextWindow,
            });
        }

        if (model.GetPricing() is { } pricing)
        {
            facts.Add(new AIModelFact
            {
                Key = "core.price",
                Label = "#uaiModelFacts_price",
                ShortLabel = "#uaiModelFacts_priceShort",
                Value = FormatPrice(pricing),
                SortValue = (double)pricing.InputPerMillionTokens,
                Detail = "#uaiModelFacts_priceDetail",
            });
        }

        return facts;
    }

    private static string FormatPrice(AIModelPricing pricing)
    {
        var prefix = string.Equals(pricing.Currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? "$"
            : pricing.Currency + " ";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}{pricing.InputPerMillionTokens:F2} / {prefix}{pricing.OutputPerMillionTokens:F2}");
    }
}
