using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Settings.Mapping;

/// <summary>
/// Defines mappings for Settings API models.
/// </summary>
public class SettingsMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Domain -> Response
        mapper.Define<AiSettings, SettingsResponseModel>((_, _) => new SettingsResponseModel(), MapToResponse);

        // Request -> Domain
        mapper.Define<UpdateSettingsRequestModel, AiSettings>((_, _) => new AiSettings(), MapFromUpdateRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AiSettings source, SettingsResponseModel target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
    }

    // Umbraco.Code.MapAll
    private static void MapFromUpdateRequest(UpdateSettingsRequestModel source, AiSettings target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
    }
}
