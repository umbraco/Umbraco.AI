using Umbraco.Ai.Core.Context.ResourceTypes;
using Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Mapping;

/// <summary>
/// Map definitions for Context models.
/// </summary>
public class ContextResourceTypeMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Resource type mappings
        mapper.Define<IAiContextResourceType, ContextResourceTypeResponseModel>((_, _) => new ContextResourceTypeResponseModel
        {
            Id = string.Empty,
            Name = string.Empty
        }, MapResourceTypeToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapResourceTypeToResponse(IAiContextResourceType source, ContextResourceTypeResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
    }
}
