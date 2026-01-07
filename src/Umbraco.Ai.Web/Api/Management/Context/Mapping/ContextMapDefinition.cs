using Umbraco.Ai.Core.Context;
using Umbraco.Ai.Core.Context.ResourceTypes;
using Umbraco.Ai.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Context.Mapping;

/// <summary>
/// Map definitions for Context models.
/// </summary>
public class ContextMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AiContext, ContextResponseModel>((_, _) => new ContextResponseModel(), MapToResponse);
        mapper.Define<AiContext, ContextItemResponseModel>((_, _) => new ContextItemResponseModel(), MapToItemResponse);
        mapper.Define<AiContextResource, ContextResourceModel>((_, _) => new ContextResourceModel(), MapResourceToModel);

        // Resource type mappings
        mapper.Define<IAiContextResourceType, ResourceTypeItemResponseModel>((_, _) => new ResourceTypeItemResponseModel
        {
            Id = string.Empty,
            Name = string.Empty
        }, MapResourceTypeToResponse);

        // Request mappings (request -> domain)
        mapper.Define<CreateContextRequestModel, AiContext>(CreateContextFactory, MapFromCreateRequest);
        mapper.Define<UpdateContextRequestModel, AiContext>((_, _) => new AiContext
        {
            Alias = string.Empty,
            Name = string.Empty
        }, MapFromUpdateRequest);
        mapper.Define<ContextResourceModel, AiContextResource>(CreateResourceFactory, MapResourceFromModel);
    }

    private static AiContext CreateContextFactory(CreateContextRequestModel source, MapperContext context)
    {
        return new AiContext
        {
            Alias = source.Alias,
            Name = source.Name
        };
    }

    private static AiContextResource CreateResourceFactory(ContextResourceModel source, MapperContext context)
    {
        return new AiContextResource
        {
            ResourceTypeId = source.ResourceTypeId,
            Name = source.Name,
            Data = source.Data
        };
    }

    // Umbraco.Code.MapAll -Id -Alias -DateCreated -DateModified
    private static void MapFromCreateRequest(CreateContextRequestModel source, AiContext target, MapperContext context)
    {
        target.Name = source.Name;
        target.Resources = source.Resources.Select(r => context.Map<AiContextResource>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -Id -DateCreated -DateModified
    private static void MapFromUpdateRequest(UpdateContextRequestModel source, AiContext target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Resources = source.Resources.Select(r => context.Map<AiContextResource>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -ResourceTypeId
    private static void MapResourceFromModel(ContextResourceModel source, AiContextResource target, MapperContext context)
    {
        target.Id = source.Id;
        // ResourceTypeId is set in factory (init-only property)
        target.Name = source.Name;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Data = source.Data;
        target.InjectionMode = Enum.TryParse<AiContextResourceInjectionMode>(source.InjectionMode, true, out var mode)
            ? mode
            : AiContextResourceInjectionMode.Always;
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AiContext source, ContextResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.Resources = source.Resources.Select(r => context.Map<ContextResourceModel>(r)!).ToList();
    }

    // Umbraco.Code.MapAll
    private static void MapToItemResponse(AiContext source, ContextItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ResourceCount = source.Resources.Count;
        target.DateModified = source.DateModified;
    }

    // Umbraco.Code.MapAll
    private static void MapResourceToModel(AiContextResource source, ContextResourceModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.ResourceTypeId = source.ResourceTypeId;
        target.Name = source.Name;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Data = source.Data;
        target.InjectionMode = source.InjectionMode.ToString();
    }

    // Umbraco.Code.MapAll
    private static void MapResourceTypeToResponse(IAiContextResourceType source, ResourceTypeItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
    }
}
