using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Settings.Mapping;

/// <summary>
/// Defines mappings for Settings API models.
/// </summary>
public class SettingsMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Domain -> Response
        mapper.Define<AISettings, SettingsResponseModel>((_, _) => new SettingsResponseModel(), MapToResponse);

        // Request -> Domain
        mapper.Define<UpdateSettingsRequestModel, AISettings>((_, _) => new AISettings(), MapFromUpdateRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AISettings source, SettingsResponseModel target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
    }

    // Umbraco.Code.MapAll -DateCreated -CreatedByUserId -DateModified -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateSettingsRequestModel source, AISettings target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
    }
}
