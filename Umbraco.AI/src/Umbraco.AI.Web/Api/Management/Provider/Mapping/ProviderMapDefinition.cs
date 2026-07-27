using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Provider.Mapping;

/// <summary>
/// Map definitions for Provider models.
/// </summary>
public class ProviderMapDefinition : IMapDefinition
{
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderMapDefinition"/> class.
    /// </summary>
    public ProviderMapDefinition(IAIExperimentalFeatures experimentalFeatures)
    {
        _experimentalFeatures = experimentalFeatures;
    }

    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<IAIProvider, ProviderItemResponseModel>((_, _) => new ProviderItemResponseModel(), Map);
        mapper.Define<IAIProvider, ProviderResponseModel>((_, _) => new ProviderResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(IAIProvider source, ProviderItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Capabilities = MapCapabilities(source);
    }

    // Umbraco.Code.MapAll
    private void Map(IAIProvider source, ProviderResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Capabilities = MapCapabilities(source);
        target.SettingsSchema = source.SettingsType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetSettingsSchema())
            : null;
        target.CapabilitySettingsSchemas = MapCapabilitySettingsSchemas(source, context);
    }

    private IEnumerable<string> MapCapabilities(IAIProvider source)
        => source.GetCapabilities()
            .Where(c => _experimentalFeatures.IsCapabilityEnabled(c.Kind))
            .Select(c => c.Kind.ToString());

    private IReadOnlyDictionary<string, EditableModelSchemaModel> MapCapabilitySettingsSchemas(
        IAIProvider source,
        MapperContext context)
    {
        var schemas = new Dictionary<string, EditableModelSchemaModel>();
        foreach (var capability in source.GetCapabilities()
                     .Where(c => _experimentalFeatures.IsCapabilityEnabled(c.Kind)))
        {
            var schema = source.GetCapabilitySettingsSchema(capability.Kind);
            if (schema is null)
            {
                continue;
            }

            var mapped = context.Map<EditableModelSchemaModel>(schema);
            if (mapped is not null)
            {
                schemas[capability.Kind.ToString()] = mapped;
            }
        }

        return schemas;
    }
}
