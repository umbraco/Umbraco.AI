using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Management.Common.Models;
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
        mapper.Define<AiSettingDefinition, SettingDefinitionModel>((_, _) => new SettingDefinitionModel(), Map);
        mapper.Define<AiModelDescriptor, ModelDescriptorResponseModel>((_, _) => new ModelDescriptorResponseModel(), Map);
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
        target.SettingDefinitions = context.MapEnumerable<AiSettingDefinition, SettingDefinitionModel>(source.GetSettingDefinitions());
    }

    // Umbraco.Code.MapAll
    private static void Map(AiSettingDefinition source, SettingDefinitionModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Label = source.Label;
        target.Description = source.Description;
        target.EditorUiAlias = source.EditorUiAlias;
        target.EditorConfig = source.EditorConfig;
        target.DefaultValue = source.DefaultValue;
        target.SortOrder = source.SortOrder;
        target.IsRequired = source.ValidationRules.OfType<RequiredAttribute>().Any();
    }

    // Umbraco.Code.MapAll
    private static void Map(AiModelDescriptor source, ModelDescriptorResponseModel target, MapperContext context)
    {
        target.Model = context.Map<ModelRefModel>(source.Model);
        target.Name = source.Name;
        target.Metadata = source.Metadata;
    }
}
