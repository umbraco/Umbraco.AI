using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Common.Mapping;

/// <summary>
/// Map definitions for shared models.
/// </summary>
public class CommonMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AiModelRef, ModelRefModel>((_, _) => new ModelRefModel(), Map);
        mapper.Define<AiModelDescriptor, ModelDescriptorResponseModel>((_, _) => new ModelDescriptorResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(AiModelRef source, ModelRefModel target, MapperContext context)
    {
        target.ProviderId = source.ProviderId;
        target.ModelId = source.ModelId;
    }

    // Umbraco.Code.MapAll
    private static void Map(AiModelDescriptor source, ModelDescriptorResponseModel target, MapperContext context)
    {
        target.Model = context.Map<ModelRefModel>(source.Model);
        target.Name = source.Name;
        target.Metadata = source.Metadata;
    }
}
