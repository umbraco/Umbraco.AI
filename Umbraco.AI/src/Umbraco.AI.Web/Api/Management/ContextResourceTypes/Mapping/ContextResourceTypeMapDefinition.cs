using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.ContextResourceTypes.Models;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.ContextResourceTypes.Mapping;

/// <summary>
/// Map definitions for Context models.
/// </summary>
public class ContextResourceTypeMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Resource type mappings
        mapper.Define<IAIContextResourceType, ContextResourceTypeResponseModel>((_, _) => new ContextResourceTypeResponseModel
        {
            Id = string.Empty,
            Name = string.Empty
        }, MapResourceTypeToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapResourceTypeToResponse(IAIContextResourceType source, ContextResourceTypeResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.DataSchema = source.DataType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetDataSchema())
            : null;
    }
}
