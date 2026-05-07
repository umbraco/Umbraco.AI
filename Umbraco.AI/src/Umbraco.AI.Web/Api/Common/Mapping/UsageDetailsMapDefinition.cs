using Microsoft.Extensions.AI;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Common.Mapping;

/// <summary>
/// Maps Microsoft.Extensions.AI <see cref="UsageDetails"/> to <see cref="UsageModel"/> for API responses.
/// </summary>
public class UsageDetailsMapDefinition : IMapDefinition
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
