using Microsoft.Extensions.AI;
using Umbraco.AI.Web.Api.Management.Embedding.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Embedding.Mapping;

/// <summary>
/// Map definitions for Embedding models.
/// </summary>
public class EmbeddingMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<Embedding<float>, EmbeddingItemModel>((_, _) => new EmbeddingItemModel { Vector = [] }, Map);
    }

    // Umbraco.Code.MapAll -Index
    private static void Map(Embedding<float> source, EmbeddingItemModel target, MapperContext context)
    {
        // Note: Index is set externally when mapping collections since Embedding<T> doesn't track its index
        target.Vector = source.Vector.ToArray();
    }
}
