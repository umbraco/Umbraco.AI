using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Provider.Mapping;

/// <summary>
/// Map definitions for Provider models.
/// </summary>
public class ProviderMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<IAiProvider, ProviderItemResponseModel>((_, _) => new ProviderItemResponseModel(), Map);
        mapper.Define<IAiProvider, ProviderResponseModel>((_, _) => new ProviderResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(IAiProvider source, ProviderItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Capabilities = source.GetCapabilities().Select(c => c.Kind.ToString());
    }

    // Umbraco.Code.MapAll
    private static void Map(IAiProvider source, ProviderResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Capabilities = source.GetCapabilities().Select(c => c.Kind.ToString());
        target.SettingsSchema = source.SettingsType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetSettingsSchema())
            : null;
    }
}
