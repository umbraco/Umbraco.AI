using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Web.Api.Management.ToolScope.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.ToolScope.Mapping;

/// <summary>
/// UmbracoMapper definitions for tool scope models.
/// </summary>
internal class ToolScopeMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<IAIToolScope, ToolScopeItemResponseModel>(
            (_, _) => new ToolScopeItemResponseModel(),
            MapToItemResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapToItemResponse(IAIToolScope source, ToolScopeItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Icon = source.Icon;
        target.IsDestructive = source.IsDestructive;
        target.Domain = source.Domain;
    }
}
