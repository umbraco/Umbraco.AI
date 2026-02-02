using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Common.Mapping;

/// <summary>
/// Map definitions for shared models.
/// </summary>
public class CommonMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AIModelRef, ModelRefModel>((_, _) => new ModelRefModel(), Map);
        mapper.Define<AIModelDescriptor, ModelDescriptorResponseModel>((_, _) => new ModelDescriptorResponseModel(), Map);

        // Request mappings (request -> domain)
        mapper.Define<ModelRefModel, AIModelRef>((source, _) => new AIModelRef(source.ProviderId, source.ModelId));
        
        // Editable model mappings
        mapper.Define<AIEditableModelSchema, EditableModelSchemaModel>((_, _) => new EditableModelSchemaModel(), Map);
        mapper.Define<AIEditableModelField, EditableModelFieldModel>((_, _) => new EditableModelFieldModel(), Map);

        // Version history mappings
        mapper.Define<AIEntityVersion, EntityVersionResponseModel>((_, _) => new EntityVersionResponseModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static void Map(AIModelRef source, ModelRefModel target, MapperContext context)
    {
        target.ProviderId = source.ProviderId;
        target.ModelId = source.ModelId;
    }

    // Umbraco.Code.MapAll
    private static void Map(AIModelDescriptor source, ModelDescriptorResponseModel target, MapperContext context)
    {
        target.Model = context.Map<ModelRefModel>(source.Model);
        target.Name = source.Name;
        target.Metadata = source.Metadata;
    }
    
    // Umbraco.Code.MapAll
    private static void Map(AIEditableModelSchema source, EditableModelSchemaModel target, MapperContext context)
    {
        target.Type = source.Type;
        target.Fields = source.Fields
            .Select(field => context.Map<EditableModelFieldModel>(field)!)
            .ToList();
    }
    
    // Umbraco.Code.MapAll
    private static void Map(AIEditableModelField source, EditableModelFieldModel target, MapperContext context)
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

    // Umbraco.Code.MapAll -CreatedByUserName
    private static void Map(AIEntityVersion source, EntityVersionResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.EntityId = source.EntityId;
        target.Version = source.Version;
        target.DateCreated = source.DateCreated;
        target.CreatedByUserId = source.CreatedByUserId;
        target.ChangeDescription = source.ChangeDescription;
        // Note: CreatedByUserName is resolved separately in the controller
    }
}
