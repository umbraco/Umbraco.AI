using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Connection.Mapping;

/// <summary>
/// Map definitions for model fact models.
/// </summary>
public class ModelFactsMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AIModelFact, ModelFactResponseModel>((_, _) => new ModelFactResponseModel(), Map);
        mapper.Define<ModelFactsItem, ModelFactsItemResponseModel>((_, _) => new ModelFactsItemResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(AIModelFact source, ModelFactResponseModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Label = source.Label;
        target.ShortLabel = source.ShortLabel;
        target.Value = source.Value;
        target.SortValue = source.SortValue;
        target.Detail = source.Detail;
        target.Tone = source.Tone;
        target.Url = source.Url;
    }

    // Umbraco.Code.MapAll
    private static void Map(ModelFactsItem source, ModelFactsItemResponseModel target, MapperContext context)
    {
        target.Model = context.Map<ModelRefModel>(source.Model)!;
        target.Facts = context.MapEnumerable<AIModelFact, ModelFactResponseModel>(source.Facts);
    }
}

/// <summary>
/// A model together with its facts; the mapping source for <see cref="ModelFactsItemResponseModel"/>.
/// </summary>
/// <param name="Model">The model the facts describe.</param>
/// <param name="Facts">The model's facts.</param>
internal sealed record ModelFactsItem(AIModelRef Model, IReadOnlyList<AIModelFact> Facts);
