using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Connection.Mapping;

/// <summary>
/// Map definitions for Connection models.
/// </summary>
public class ConnectionMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AiConnection, ConnectionResponseModel>((_, _) => new ConnectionResponseModel(), Map);
        mapper.Define<AiConnection, ConnectionItemResponseModel>((_, _) => new ConnectionItemResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(AiConnection source, ConnectionResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ProviderId = source.ProviderId;
        target.Settings = source.Settings;
        target.IsActive = source.IsActive;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
    }

    // Umbraco.Code.MapAll
    private static void Map(AiConnection source, ConnectionItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.ProviderId = source.ProviderId;
        target.IsActive = source.IsActive;
    }
}
