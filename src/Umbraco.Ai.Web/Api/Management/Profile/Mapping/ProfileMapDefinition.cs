using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Profile.Mapping;

/// <summary>
/// Map definitions for Profile models.
/// </summary>
public class ProfileMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AiProfile, ProfileResponseModel>((_, _) => new ProfileResponseModel(), Map);
        mapper.Define<AiProfile, ProfileItemResponseModel>((_, _) => new ProfileItemResponseModel(), Map);
        mapper.Define<AiModelRef, ModelRefModel>((_, _) => new ModelRefModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(AiProfile source, ProfileResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ConnectionId = source.ConnectionId;
        target.Capability = source.Capability.ToString();
        target.Model = context.Map<ModelRefModel>(source.Model);
        target.SystemPromptTemplate = source.SystemPromptTemplate;
        target.Temperature = source.Temperature;
        target.MaxTokens = source.MaxTokens;
        target.Tags = source.Tags;
    }

    // Umbraco.Code.MapAll
    private static void Map(AiProfile source, ProfileItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Capability = source.Capability.ToString();
        target.Model = context.Map<ModelRefModel>(source.Model);
    }

    // Umbraco.Code.MapAll
    private static void Map(AiModelRef source, ModelRefModel target, MapperContext context)
    {
        target.ProviderId = source.ProviderId;
        target.ModelId = source.ModelId;
    }
}
