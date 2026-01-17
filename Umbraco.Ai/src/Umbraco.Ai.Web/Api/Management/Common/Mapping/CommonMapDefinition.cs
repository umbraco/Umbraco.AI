using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
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
        // Response mappings (domain -> response)
        mapper.Define<AiModelRef, ModelRefModel>((_, _) => new ModelRefModel(), Map);
        mapper.Define<AiModelDescriptor, ModelDescriptorResponseModel>((_, _) => new ModelDescriptorResponseModel(), Map);

        // Request mappings (request -> domain)
        mapper.Define<ModelRefModel, AiModelRef>((source, _) => new AiModelRef(source.ProviderId, source.ModelId));
        
        // Editable model mappings
        mapper.Define<AiEditableModelSchema, EditableModelSchemaModel>((_, _) => new EditableModelSchemaModel(), Map);
        mapper.Define<AiEditableModelField, EditableModelFieldModel>((_, _) => new EditableModelFieldModel(), Map);
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
    
    // Umbraco.Code.MapAll
    private static void Map(AiEditableModelSchema source, EditableModelSchemaModel target, MapperContext context)
    {
        target.Type = source.Type;
        target.Fields = source.Fields
            .Select(field => context.Map<EditableModelFieldModel>(field)!)
            .ToList();
    }
    
    // Umbraco.Code.MapAll
    private static void Map(AiEditableModelField source, EditableModelFieldModel target, MapperContext context)
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
}
