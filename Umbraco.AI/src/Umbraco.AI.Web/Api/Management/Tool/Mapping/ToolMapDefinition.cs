using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Web.Api.Management.Tool.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Tool.Mapping;

/// <summary>
/// UmbracoMapper definitions for tool models.
/// </summary>
public class ToolMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<IAIToolScope, ToolScopeItemResponseModel>(
            (_, _) => new ToolScopeItemResponseModel(),
            MapToolScopeToItemResponse);

        mapper.Define<IAITool, ToolItemResponseModel>(
            (_, _) => new ToolItemResponseModel(),
            MapToolToItemResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapToolScopeToItemResponse(IAIToolScope source, ToolScopeItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Icon = source.Icon;
        target.IsDestructive = source.IsDestructive;
        target.Domain = source.Domain;
    }

    // Umbraco.Code.MapAll
    private static void MapToolToItemResponse(IAITool source, ToolItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ScopeId = source.ScopeId;
        target.IsDestructive = source.IsDestructive;
        target.Tags = source.Tags;
    }
}
