using Microsoft.Extensions.AI;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Common.Mapping;

/// <summary>
/// Map definitions for common models shared across API features.
/// </summary>
public class CommonMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper) 
    {
        mapper.Define<UsageDetails, UsageModel>((_, _) => new UsageModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(UsageDetails source, UsageModel target, MapperContext context)
    {
        target.InputTokens = source.InputTokenCount;
        target.OutputTokens = source.OutputTokenCount;
        target.TotalTokens = source.TotalTokenCount;
    }
}
